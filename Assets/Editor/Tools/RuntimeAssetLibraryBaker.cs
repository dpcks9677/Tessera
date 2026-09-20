#if UNITY_EDITOR
using System.IO;
using System.Linq;
using Tessera.Core;
using UnityEditor;
using UnityEngine;

namespace Tessera.EditorTools
{
    /// <summary>
    /// <see cref="RuntimeAssetLibrary"/> 카탈로그에 폰트·양피지 텍스처·족보 아이콘 참조를 채워 넣는다.
    ///
    /// 런타임 코드가 <c>#if UNITY_EDITOR</c> + <c>AssetDatabase.LoadAssetAtPath</c>로만 에셋을 얻으면
    /// 플레이어 빌드에서 전부 null이 된다(LOAD-01). 이 도구는 그 참조들을 미리 구워
    /// <c>Assets/Resources/RuntimeAssetLibrary.asset</c>에 담는다.
    /// </summary>
    public static class RuntimeAssetLibraryBaker
    {
        private const string OutputAssetPath = "Assets/Resources/RuntimeAssetLibrary.asset";

        private const string MulmaruPath = "Assets/Art/ThirdParty/Fonts/Mulmaru.ttf";
        private const string AlagardPath = "Assets/Art/ThirdParty/Fonts/alagard.ttf";
        private const string M6x11Path = "Assets/Art/ThirdParty/Fonts/m6x11.ttf";

        private const string ParchmentBasePath = "Assets/Art/Generated/Parchment/parchment_base.png";
        private const string ParchmentBurntEdgePath = "Assets/Art/Generated/Parchment/parchment_burnt_edge.png";
        private const string ParchmentWarmSandPath = "Assets/Art/Generated/Parchment/parchment_warm_sand.png";

        private const string IconsDir = "Assets/Art/Generated/Parchment/Icons";

        [MenuItem("Tools/Tessera/Bake Runtime Asset Library")]
        public static void Bake()
        {
            RuntimeAssetLibrary library = AssetDatabase.LoadAssetAtPath<RuntimeAssetLibrary>(OutputAssetPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<RuntimeAssetLibrary>();
                AssetDatabase.CreateAsset(library, OutputAssetPath);
            }

            SerializedObject so = new(library);

            SetFont(so, "koreanPixelFont", MulmaruPath);
            SetFont(so, "latinPixelFont", AlagardPath);
            SetFont(so, "latinPixelFontAlt", M6x11Path);

            SetTexture(so, "parchmentBase", ParchmentBasePath);
            SetTexture(so, "parchmentBurntEdge", ParchmentBurntEdgePath);
            SetTexture(so, "parchmentWarmSand", ParchmentWarmSandPath);

            SetScoreIcons(so);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RuntimeAssetLibraryBaker] '{OutputAssetPath}' 갱신 완료.");
        }

        private static void SetFont(SerializedObject so, string fieldName, string assetPath)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(assetPath);
            if (font == null)
            {
                Debug.LogWarning($"[RuntimeAssetLibraryBaker] '{assetPath}' 폰트를 로드하지 못했습니다. {fieldName} 필드를 비워 둡니다.");
            }
            so.FindProperty(fieldName).objectReferenceValue = font;
        }

        private static void SetTexture(SerializedObject so, string fieldName, string assetPath)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                Debug.LogWarning($"[RuntimeAssetLibraryBaker] '{assetPath}' 텍스처를 로드하지 못했습니다. {fieldName} 필드를 비워 둡니다.");
            }
            so.FindProperty(fieldName).objectReferenceValue = texture;
        }

        private static void SetScoreIcons(SerializedObject so)
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { IconsDir });
            Sprite[] icons = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(sprite => sprite != null)
                .ToArray();

            if (icons.Length == 0)
            {
                Debug.LogWarning($"[RuntimeAssetLibraryBaker] '{IconsDir}'에서 아이콘을 찾지 못했습니다. scoreIcons 필드를 비워 둡니다.");
            }

            SerializedProperty iconsProp = so.FindProperty("scoreIcons");
            iconsProp.arraySize = icons.Length;
            for (int i = 0; i < icons.Length; i++)
            {
                iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
            }
        }
    }
}
#endif
