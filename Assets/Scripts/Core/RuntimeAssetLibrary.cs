using UnityEngine;

namespace Tessera.Core
{
    /// <summary>
    /// 런타임 코드가 폰트·양피지 텍스처·족보 아이콘을 얻는 유일한 통로.
    ///
    /// 이 에셋들은 원래 <c>#if UNITY_EDITOR</c> + <c>AssetDatabase.LoadAssetAtPath</c>로 로드됐다.
    /// 플레이어 빌드에서는 그 코드가 통째로 빠져 null이 되고, 폰트는 기본 폰트로 떨어지며
    /// 양피지 텍스처와 족보 아이콘은 폴백 없이 단색 평면·빈 아이콘이 된다.
    ///
    /// 해결책은 이 <c>ScriptableObject</c> 하나를 <c>Assets/Resources/</c>에 두고
    /// <see cref="Resources.Load{T}(string)"/>로 참조를 얻는 것이다. 에셋 파일 자체는
    /// 원래 위치(<c>Assets/Art/...</c>)에 그대로 두고, 이 카탈로그가 참조를 들고 있으므로
    /// 빌드 시 자동으로 포함된다. 값 채우기는 <c>Assets/Editor/Tools/RuntimeAssetLibraryBaker.cs</c>가 한다.
    /// </summary>
    public sealed class RuntimeAssetLibrary : ScriptableObject
    {
        public const string ResourcePath = "RuntimeAssetLibrary";

        [SerializeField] private Font koreanPixelFont;
        [SerializeField] private Font latinPixelFont;
        [SerializeField] private Font latinPixelFontAlt;
        [SerializeField] private Texture2D parchmentBase;
        [SerializeField] private Texture2D parchmentBurntEdge;
        [SerializeField] private Texture2D parchmentWarmSand;
        [SerializeField] private Sprite[] scoreIcons;

        private static RuntimeAssetLibrary instance;
        private static bool missingWarningLogged;

        /// <summary>
        /// 카탈로그 인스턴스. <c>Resources.Load</c>는 한 번만 수행하고 결과를 캐시한다.
        /// 카탈로그 에셋이 아직 구워지지 않았으면 null이며, 매 프레임 반복하지 않고
        /// 경고 로그를 한 번만 남긴다.
        /// </summary>
        public static RuntimeAssetLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<RuntimeAssetLibrary>(ResourcePath);
                    if (instance == null && !missingWarningLogged)
                    {
                        Debug.LogWarning($"[RuntimeAssetLibrary] '{ResourcePath}' 카탈로그를 Resources에서 찾지 못했습니다. Tools/Tessera/Bake Runtime Asset Library로 구워야 합니다.");
                        missingWarningLogged = true;
                    }
                }
                return instance;
            }
        }

        public static Font KoreanPixelFont => Instance != null ? Instance.koreanPixelFont : null;
        public static Font LatinPixelFont => Instance != null ? Instance.latinPixelFont : null;
        public static Font LatinPixelFontAlt => Instance != null ? Instance.latinPixelFontAlt : null;
        public static Texture2D ParchmentBase => Instance != null ? Instance.parchmentBase : null;
        public static Texture2D ParchmentBurntEdge => Instance != null ? Instance.parchmentBurntEdge : null;
        public static Texture2D ParchmentWarmSand => Instance != null ? Instance.parchmentWarmSand : null;

        /// <summary>이름으로 족보 아이콘 스프라이트를 찾는다. 카탈로그가 없거나 못 찾으면 null.</summary>
        public static Sprite FindScoreIcon(string iconName)
        {
            RuntimeAssetLibrary lib = Instance;
            if (lib == null || lib.scoreIcons == null) return null;

            foreach (Sprite sprite in lib.scoreIcons)
            {
                if (sprite != null && sprite.name == iconName) return sprite;
            }
            return null;
        }
    }
}
