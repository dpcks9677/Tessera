using NUnit.Framework;
using Tessera.Dice;
using Tessera.Games.AugmentedYacht;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 8면 주사위의 회전 계산을 본다(M7-T5).
    /// 6면과 달리 면이 축과 나란하지 않아 별도 경로를 타므로 따로 검증한다.
    /// </summary>
    [TestFixture]
    public sealed class OctahedronOrientationTests
    {
        [Test]
        public void 여덟_면_법선은_서로_다르고_정규화되어_있다()
        {
            Vector3[] normals = DiceFaceOrientation.OctaFaceNormals;
            Assert.That(normals.Length, Is.EqualTo(8));

            for (int i = 0; i < normals.Length; i++)
            {
                Assert.That(normals[i].magnitude, Is.EqualTo(1f).Within(0.001f));
                for (int j = i + 1; j < normals.Length; j++)
                {
                    Assert.That(Vector3.Dot(normals[i], normals[j]), Is.LessThan(0.99f),
                        $"{i + 1}번과 {j + 1}번 면이 같은 방향을 봅니다.");
                }
            }
        }

        [Test]
        public void 어떤_착지회전에서도_목표면이_위로_온다()
        {
            UnityEngine.Random.InitState(20260905);

            for (int i = 0; i < 200; i++)
            {
                Quaternion landing = UnityEngine.Random.rotationUniform;
                int face = UnityEngine.Random.Range(1, 9);

                Quaternion remap = DiceFaceOrientation.GetOctaVisualRemapRotation(landing, face);
                Vector3 shown = landing * remap * DiceFaceOrientation.OctaFaceNormals[face - 1];
                Vector3 physicalTop = DiceFaceOrientation.OctaFaceNormals[DiceFaceOrientation.GetOctaTopFace(landing) - 1];

                // 목표 면이 원래 위를 향하던 면 자리로 옮겨 온다.
                Assert.That(Vector3.Dot(shown.normalized, (landing * physicalTop).normalized), Is.GreaterThan(0.99f));
            }
        }

        [Test]
        public void 카메라정렬회전은_목표면을_카메라쪽으로_돌린다()
        {
            // 카메라가 75도로 내려다보므로 면 법선은 위에서 15도 기운 방향을 향해야 한다.
            Vector3 expected = Quaternion.Euler(-15f, 0f, 0f) * Vector3.up;

            for (int face = 1; face <= 8; face++)
            {
                Quaternion rotation = DiceFaceOrientation.GetOctaCameraFacingRotation(face);
                Vector3 shown = rotation * DiceFaceOrientation.OctaFaceNormals[face - 1];
                Assert.That(Vector3.Dot(shown.normalized, expected.normalized), Is.GreaterThan(0.999f),
                    $"{face}번 면이 카메라를 향하지 않습니다.");
            }
        }

        [Test]
        public void 팔면주사위는_프리셋의_뒤쪽_슬롯을_쓴다()
        {
            var types = new[]
            {
                DieType.Octahedron, DieType.Octahedron,
                DieType.Normal, DieType.Normal, DieType.Normal
            };

            int[] order = YachtDiceRoundPresenter.BuildPresetSlotOrder(types);

            Assert.That(order, Is.EqualTo(new[] { 2, 3, 4, 0, 1 }),
                "8면 주사위는 프리셋 뒤쪽 슬롯에서 던져지므로 마지막에 와야 합니다.");
        }

        [Test]
        public void 팔면주사위가_없으면_슬롯_순서를_바꾸지_않는다()
        {
            var types = new[] { DieType.Normal, DieType.Golden, DieType.Sevens, DieType.HeavyRed, DieType.Couple };

            int[] order = YachtDiceRoundPresenter.BuildPresetSlotOrder(types);

            Assert.That(order, Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
        }
    }
}
