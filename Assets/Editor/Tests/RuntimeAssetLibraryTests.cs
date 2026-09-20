using System.IO;
using NUnit.Framework;
using Tessera.Core;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 런타임 에셋 카탈로그가 플레이어 빌드에서도 폰트·양피지 텍스처·족보 아이콘을
    /// 제공하는지 검증한다(LOAD-01).
    /// </summary>
    public sealed class RuntimeAssetLibraryTests
    {
        private const string IconsDir = "Assets/Art/Generated/Parchment/Icons";

        // ParchmentScoreSheet.cs의 upperIcons/lowerIcons 배열에 실제로 넘기는 문자열 그대로.
        private static readonly string[] UsedIconNames =
        {
            "dice_1", "dice_2", "dice_3", "dice_4", "dice_5", "dice_6",
            "choice", "4oak", "fullhouse", "s_straight", "l_straight", "yacht"
        };

        [Test]
        public void CatalogLoadsFromResources()
        {
            RuntimeAssetLibrary library = Resources.Load<RuntimeAssetLibrary>(RuntimeAssetLibrary.ResourcePath);
            Assert.That(library, Is.Not.Null, $"'{RuntimeAssetLibrary.ResourcePath}' 카탈로그를 Resources에서 로드하지 못했습니다.");
        }

        [Test]
        public void AllPixelFontsAreAssigned()
        {
            Assert.That(RuntimeAssetLibrary.KoreanPixelFont, Is.Not.Null, "koreanPixelFont(Mulmaru)가 비어 있습니다.");
            Assert.That(RuntimeAssetLibrary.LatinPixelFont, Is.Not.Null, "latinPixelFont(alagard)가 비어 있습니다.");
            Assert.That(RuntimeAssetLibrary.LatinPixelFontAlt, Is.Not.Null, "latinPixelFontAlt(m6x11)가 비어 있습니다.");
        }

        [Test]
        public void AllParchmentTexturesAreAssigned()
        {
            Assert.That(RuntimeAssetLibrary.ParchmentBase, Is.Not.Null, "parchmentBase가 비어 있습니다.");
            Assert.That(RuntimeAssetLibrary.ParchmentBurntEdge, Is.Not.Null, "parchmentBurntEdge가 비어 있습니다.");
            Assert.That(RuntimeAssetLibrary.ParchmentWarmSand, Is.Not.Null, "parchmentWarmSand가 비어 있습니다.");
        }

        [Test]
        public void ScoreIconCountMatchesIconsDirectory()
        {
            string iconsFullPath = Path.Combine(Application.dataPath, "..", IconsDir);
            int pngCount = Directory.GetFiles(iconsFullPath, "*.png").Length;

            RuntimeAssetLibrary library = Resources.Load<RuntimeAssetLibrary>(RuntimeAssetLibrary.ResourcePath);
            Assert.That(library, Is.Not.Null);

            SerializedObject so = new(library);
            int scoreIconCount = so.FindProperty("scoreIcons").arraySize;

            Assert.That(scoreIconCount, Is.EqualTo(pngCount), $"'{IconsDir}'의 png 개수({pngCount})와 scoreIcons 길이({scoreIconCount})가 다릅니다.");
        }

        [Test]
        public void AllIconsUsedByScoreSheetAreFound()
        {
            foreach (string iconName in UsedIconNames)
            {
                Sprite sprite = RuntimeAssetLibrary.FindScoreIcon(iconName);
                Assert.That(sprite, Is.Not.Null, $"ParchmentScoreSheet가 쓰는 아이콘 '{iconName}'을 FindScoreIcon으로 찾지 못했습니다.");
            }
        }
    }
}
