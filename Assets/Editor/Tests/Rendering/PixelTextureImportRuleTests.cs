using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// D-050에 따라 게임용 텍스처는 Point 필터이고 밉맵이 꺼져 있어야 합니다.
    /// 아직 맞추지 않은 텍스처는 맞출 태스크 ID와 함께 <see cref="PendingExceptions"/>에 두고,
    /// 태스크가 끝나면 그 줄을 지웁니다. ThirdParty는 검사 루트에 넣지 않아 제외됩니다.
    /// </summary>
    [TestFixture]
    public sealed class PixelTextureImportRuleTests
    {
        private static readonly string[] Roots =
        {
            "Assets/04.Art/Generated",
            "Assets/04.Art/Source",
            "Assets/Resources",
        };

        /// <summary>게임이 쓰지 않는 원본·레퍼런스입니다.</summary>
        private static readonly HashSet<string> PermanentExclusions = new()
        {
            "Assets/04.Art/Source/Coin/coin_head_source.png",
            "Assets/04.Art/Source/Coin/coin_tail_source.png",
            "Assets/04.Art/Source/Dice/dice_pixel_reference.png",
        };

        /// <summary>경로 → 맞출 태스크 ID. 태스크가 끝나면 줄을 지웁니다.</summary>
        private static readonly Dictionary<string, string> PendingExceptions = new()
        {
            { "Assets/04.Art/Generated/Games/Yacht/ScoreIcons/4oak.png", "PIXEL-T8" },
            { "Assets/04.Art/Generated/Games/Yacht/ScoreIcons/choice.png", "PIXEL-T8" },
            { "Assets/04.Art/Generated/Games/Yacht/ScoreIcons/dice_1.png", "PIXEL-T8" },
            { "Assets/04.Art/Generated/Games/Yacht/ScoreIcons/dice_2.png", "PIXEL-T8" },
            { "Assets/04.Art/Generated/Games/Yacht/ScoreIcons/dice_3.png", "PIXEL-T8" },
            { "Assets/04.Art/Generated/Games/Yacht/ScoreIcons/dice_4.png", "PIXEL-T8" },
            { "Assets/04.Art/Generated/Games/Yacht/ScoreIcons/dice_5.png", "PIXEL-T8" },
            { "Assets/04.Art/Generated/Games/Yacht/ScoreIcons/dice_6.png", "PIXEL-T8" },
            { "Assets/04.Art/Generated/Games/Yacht/ScoreIcons/fullhouse.png", "PIXEL-T8" },
            { "Assets/04.Art/Generated/Games/Yacht/ScoreIcons/l_straight.png", "PIXEL-T8" },
            { "Assets/04.Art/Generated/Games/Yacht/ScoreIcons/s_straight.png", "PIXEL-T8" },
            { "Assets/04.Art/Generated/Games/Yacht/ScoreIcons/yacht.png", "PIXEL-T8" },
        };

        [Test]
        public void GameTexturesArePointWithoutMipmaps()
        {
            List<string> violations = AllTexturePaths()
                .Where(path => !PermanentExclusions.Contains(path) && !PendingExceptions.ContainsKey(path))
                .Where(path => IsViolating(path))
                .ToList();

            Assert.That(violations, Is.Empty,
                "Point 필터 + 밉맵 끔이 아닌 텍스처가 있습니다(D-050):\n" + string.Join("\n", violations));
        }

        [Test]
        public void PendingExceptionsStillNeedWork()
        {
            var stale = new List<string>();
            foreach (KeyValuePair<string, string> entry in PendingExceptions)
            {
                var importer = AssetImporter.GetAtPath(entry.Key) as TextureImporter;
                if (importer == null || !IsViolating(importer))
                {
                    stale.Add($"{entry.Key} ({entry.Value})");
                }
            }

            Assert.That(stale, Is.Empty,
                "이미 규칙을 지키거나 없는 항목입니다. 예외 목록에서 지우십시오:\n" + string.Join("\n", stale));
        }

        [Test]
        public void PermanentExclusionsExist()
        {
            List<string> missing = PermanentExclusions
                .Where(path => AssetImporter.GetAtPath(path) as TextureImporter == null)
                .ToList();

            Assert.That(missing, Is.Empty, "영구 제외 항목을 찾을 수 없습니다(이름이 바뀌었는지 확인):\n" + string.Join("\n", missing));
        }

        private static IEnumerable<string> AllTexturePaths()
        {
            return AssetDatabase.FindAssets("t:Texture2D", Roots).Select(AssetDatabase.GUIDToAssetPath);
        }

        private static bool IsViolating(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            return importer != null && IsViolating(importer);
        }

        private static bool IsViolating(TextureImporter importer)
        {
            return !(importer.filterMode == FilterMode.Point && !importer.mipmapEnabled);
        }
    }
}
