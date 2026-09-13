using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    [TestFixture]
    public sealed class FontFallbackTests
    {
        [Test]
        public void MulmaruFontAssetExistsInProject()
        {
            Font mulmaru = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Mulmaru.ttf");
            Assert.That(mulmaru, Is.Not.Null, "Assets/Fonts/Mulmaru.ttf 에셋이 로드되지 않았습니다.");
        }

        [Test]
        public void AlagardFontFallbackContainsMulmaru()
        {
            Font mulmaru = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Mulmaru.ttf");
            Assert.That(mulmaru, Is.Not.Null);

            TrueTypeFontImporter importer = AssetImporter.GetAtPath("Assets/Fonts/alagard.ttf") as TrueTypeFontImporter;
            Assert.That(importer, Is.Not.Null, "alagard.ttf의 TrueTypeFontImporter를 가져올 수 없습니다.");

            // .meta YAML의 키는 fallbackFontReferences지만 직렬화 프로퍼티 이름은 다르다.
            // SerializedObject로 그 키를 찾으면 항상 null이므로 공개 API를 쓴다.
            Font[] fallbacks = importer.fontReferences;
            Assert.That(fallbacks, Is.Not.Null, "alagard.ttf의 fontReferences가 null입니다.");

            Assert.That(fallbacks, Does.Contain(mulmaru), "alagard.ttf의 폴백 폰트에 Mulmaru가 등록되지 않았습니다.");
        }

        [Test]
        public void M6x11FontFallbackContainsMulmaru()
        {
            Font mulmaru = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Mulmaru.ttf");
            Assert.That(mulmaru, Is.Not.Null);

            TrueTypeFontImporter importer = AssetImporter.GetAtPath("Assets/Fonts/m6x11.ttf") as TrueTypeFontImporter;
            Assert.That(importer, Is.Not.Null, "m6x11.ttf의 TrueTypeFontImporter를 가져올 수 없습니다.");

            // .meta YAML의 키는 fallbackFontReferences지만 직렬화 프로퍼티 이름은 다르다.
            // SerializedObject로 그 키를 찾으면 항상 null이므로 공개 API를 쓴다.
            Font[] fallbacks = importer.fontReferences;
            Assert.That(fallbacks, Is.Not.Null, "m6x11.ttf의 fontReferences가 null입니다.");

            Assert.That(fallbacks, Does.Contain(mulmaru), "m6x11.ttf의 폴백 폰트에 Mulmaru가 등록되지 않았습니다.");
        }
    }
}
