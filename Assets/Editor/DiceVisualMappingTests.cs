using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Dice;
using Tessera.Games.AugmentedYacht;
using Tessera.Games.Yacht;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 규칙 쪽 주사위 종류가 화면 종류로 빠짐없이 이어지는지 본다(M7-T5).
    /// 이 대응이 끊기면 증강을 받아도 주사위 외형이 바뀌지 않는다.
    /// </summary>
    [TestFixture]
    public sealed class DiceVisualMappingTests
    {
        [Test]
        public void Resolve_모든_규칙주사위종류를_고유한_화면종류로_옮긴다()
        {
            var seen = new Dictionary<DieType, YachtDieType>();

            foreach (YachtDieType logical in Enum.GetValues(typeof(YachtDieType)))
            {
                DieType visual = YachtDieVisuals.Resolve(logical);

                if (logical != YachtDieType.Normal)
                {
                    Assert.That(visual, Is.Not.EqualTo(DieType.Normal),
                        $"{logical}이 기본 주사위로 뭉개졌습니다. 화면에서 구분할 수 없습니다.");
                }

                Assert.That(seen.ContainsKey(visual), Is.False,
                    $"{logical}과 {(seen.TryGetValue(visual, out YachtDieType other) ? other.ToString() : string.Empty)}이 같은 화면 종류 {visual}을 씁니다.");
                seen[visual] = logical;
            }

            Assert.That(seen.Count, Is.EqualTo(Enum.GetValues(typeof(YachtDieType)).Length));
        }

        [Test]
        public void Resolve가_반환한_화면종류는_모두_전용_팔레트를_가진다()
        {
            DiePaletteDefinition normal = DicePaletteCatalog.GetDefinition(DieType.Normal);

            foreach (YachtDieType logical in Enum.GetValues(typeof(YachtDieType)))
            {
                if (logical == YachtDieType.Normal) continue;

                DieType visual = YachtDieVisuals.Resolve(logical);
                DiePaletteDefinition palette = DicePaletteCatalog.GetDefinition(visual);

                // GetDefinition은 미등록 종류에 기본 팔레트를 돌려준다. 그 폴백에 걸리면 항목이 없다는 뜻이다.
                Assert.That(palette.DisplayName, Is.Not.EqualTo(normal.DisplayName),
                    $"{visual}에 팔레트 정의가 없어 기본 흰 주사위로 표시됩니다.");
            }
        }

        [Test]
        public void 팔각주사위는_원본의_미드나잇_네이비를_쓴다()
        {
            DiePaletteDefinition octa = DicePaletteCatalog.GetDefinition(DieType.Octahedron);

            // 원본 preset-studio/src/diceMaterials.js:16 의 #002F5E 색조를 유지한다.
            // 앰버 조명에서 색이 읽히도록 명도만 올렸으므로 파랑이 가장 강하고 빨강이 가장 약해야 한다.
            Assert.That(octa.BodyColor.b, Is.GreaterThan(octa.BodyColor.g));
            Assert.That(octa.BodyColor.g, Is.GreaterThan(octa.BodyColor.r));
            Assert.That(octa.BodyColor.r, Is.LessThan(0.2f));
            Assert.That(octa.BodyColor.b, Is.InRange(0.3f, 0.6f));
            // 눈과 몸체는 밝기가 충분히 갈려야 저해상도에서 숫자가 읽힌다.
            Assert.That(Mathf.Abs(octa.PipColor.grayscale - octa.BodyColor.grayscale), Is.GreaterThan(0.2f));
            // 금속기와 광택을 올리면 앰버 키라이트의 반사가 확산광을 덮어 남색이 흰 덩어리로 날아간다.
            // 8면체는 면이 여덟이라 그중 몇 개가 항상 조명을 정면으로 받으므로 D6보다 무광이어야 한다.
            Assert.That(octa.Metallic, Is.EqualTo(0f));
            Assert.That(octa.Smoothness, Is.LessThan(DicePaletteCatalog.GetDefinition(DieType.Normal).Smoothness));
        }
    }
}
