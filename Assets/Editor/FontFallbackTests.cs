using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    [TestFixture]
    public sealed class FontFallbackTests
    {
        [Test]
        public void Mulmaru_폰트_에셋이_프로젝트에_존재한다()
        {
            Font mulmaru = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Mulmaru.ttf");
            Assert.That(mulmaru, Is.Not.Null, "Assets/Fonts/Mulmaru.ttf 에셋이 로드되지 않았습니다.");
        }

        [Test]
        public void Alagard_폰트의_폴백에_Mulmaru가_등록되어_있다()
        {
            Font mulmaru = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Mulmaru.ttf");
            Assert.That(mulmaru, Is.Not.Null);

            TrueTypeFontImporter importer = AssetImporter.GetAtPath("Assets/Fonts/alagard.ttf") as TrueTypeFontImporter;
            Assert.That(importer, Is.Not.Null, "alagard.ttf의 TrueTypeFontImporter를 가져올 수 없습니다.");

            SerializedObject so = new SerializedObject(importer);
            SerializedProperty fallbackProp = so.FindProperty("fallbackFontReferences");
            Assert.That(fallbackProp, Is.Not.Null, "fallbackFontReferences 속성을 찾을 수 없습니다.");

            bool containsMulmaru = false;
            for (int i = 0; i < fallbackProp.arraySize; i++)
            {
                if (fallbackProp.GetArrayElementAtIndex(i).objectReferenceValue == mulmaru)
                {
                    containsMulmaru = true;
                    break;
                }
            }

            Assert.That(containsMulmaru, Is.True, "alagard.ttf의 fallbackFontReferences에 Mulmaru가 등록되지 않았습니다.");
        }

        [Test]
        public void M6x11_폰트의_폴백에_Mulmaru가_등록되어_있다()
        {
            Font mulmaru = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Mulmaru.ttf");
            Assert.That(mulmaru, Is.Not.Null);

            TrueTypeFontImporter importer = AssetImporter.GetAtPath("Assets/Fonts/m6x11.ttf") as TrueTypeFontImporter;
            Assert.That(importer, Is.Not.Null, "m6x11.ttf의 TrueTypeFontImporter를 가져올 수 없습니다.");

            SerializedObject so = new SerializedObject(importer);
            SerializedProperty fallbackProp = so.FindProperty("fallbackFontReferences");
            Assert.That(fallbackProp, Is.Not.Null, "fallbackFontReferences 속성을 찾을 수 없습니다.");

            bool containsMulmaru = false;
            for (int i = 0; i < fallbackProp.arraySize; i++)
            {
                if (fallbackProp.GetArrayElementAtIndex(i).objectReferenceValue == mulmaru)
                {
                    containsMulmaru = true;
                    break;
                }
            }

            Assert.That(containsMulmaru, Is.True, "m6x11.ttf의 fallbackFontReferences에 Mulmaru가 등록되지 않았습니다.");
        }
    }
}
