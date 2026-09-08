#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Tessera.EditorTools
{
    /// <summary>
    /// 영문 픽셀 폰트(Alagard, m6x11)의 fallbackFontReferences에
    /// 한글 픽셀 폰트(Mulmaru)를 등록하여 한글 출력 시 일관된 픽셀 스타일을 유지합니다.
    /// </summary>
    [InitializeOnLoad]
    public static class FontFallbackSetup
    {
        private const string MulmaruPath = "Assets/Fonts/Mulmaru.ttf";
        private const string AlagardPath = "Assets/Fonts/alagard.ttf";
        private const string M6x11Path = "Assets/Fonts/m6x11.ttf";

        static FontFallbackSetup()
        {
            EditorApplication.delayCall += EnsureFontFallbacks;
        }

        [MenuItem("Tools/Tessera/Ensure Font Fallbacks")]
        public static void EnsureFontFallbacks()
        {
            Font mulmaru = AssetDatabase.LoadAssetAtPath<Font>(MulmaruPath);
            if (mulmaru == null)
            {
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(MulmaruPath, ImportAssetOptions.ForceUpdate);
                mulmaru = AssetDatabase.LoadAssetAtPath<Font>(MulmaruPath);
            }

            if (mulmaru == null)
            {
                Debug.LogWarning("[FontFallbackSetup] Mulmaru.ttf를 로드할 수 없습니다. Unity 창을 활성화하여 임포트를 진행해 주세요.");
                return;
            }

            ConfigureFallback(AlagardPath, mulmaru);
            ConfigureFallback(M6x11Path, mulmaru);
            Debug.Log("[FontFallbackSetup] Mulmaru 폰트 및 Fallback 구성이 완료되었습니다.");
        }

        private static void ConfigureFallback(string fontPath, Font fallbackFont)
        {
            TrueTypeFontImporter importer = AssetImporter.GetAtPath(fontPath) as TrueTypeFontImporter;
            if (importer == null) return;

            SerializedObject so = new SerializedObject(importer);
            SerializedProperty fallbackProp = so.FindProperty("fallbackFontReferences");
            if (fallbackProp == null) return;

            bool alreadyContains = false;
            for (int i = 0; i < fallbackProp.arraySize; i++)
            {
                SerializedProperty elem = fallbackProp.GetArrayElementAtIndex(i);
                if (elem.objectReferenceValue == fallbackFont)
                {
                    alreadyContains = true;
                    break;
                }
            }

            if (!alreadyContains)
            {
                int index = fallbackProp.arraySize;
                fallbackProp.InsertArrayElementAtIndex(index);
                fallbackProp.GetArrayElementAtIndex(index).objectReferenceValue = fallbackFont;
                so.ApplyModifiedProperties();
                AssetDatabase.ImportAsset(fontPath, ImportAssetOptions.ForceUpdate);
            }
        }
    }
}
#endif
