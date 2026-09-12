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
        public void EightFaceNormalsAreDistinctAndNormalized()
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
        public void TargetFaceEndsUpAtAnyLandingRotation()
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
        public void CameraAlignRotationTurnsTargetFaceTowardCamera()
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
        public void AligningMakesDigitUprightOnAnyFace()
        {
            UnityEngine.Random.InitState(20260908);

            Vector3 screenUp = Quaternion.Euler(-15f, 0f, 0f) * Vector3.forward;
            Vector3 cameraDirection = Quaternion.Euler(-15f, 0f, 0f) * Vector3.up;

            for (int i = 0; i < 200; i++)
            {
                Quaternion landing = UnityEngine.Random.rotationUniform;
                int face = UnityEngine.Random.Range(1, 9);

                // 착지 회전에서 눈을 바꿔 새긴 뒤(Visual) 카메라 정렬(루트)을 얹는 실제 순서.
                int physicalTop = DiceFaceOrientation.GetOctaTopFace(landing);
                Quaternion remap = DiceFaceOrientation.GetOctaVisualRemapRotation(landing, face);
                Quaternion root = DiceFaceOrientation.GetOctaCameraFacingRotation(physicalTop);
                Quaternion shown = root * remap;

                Assert.That(Vector3.Dot(shown * DiceFaceOrientation.OctaFaceNormals[face - 1], cameraDirection),
                    Is.GreaterThan(0.999f), $"{face}번 면이 카메라를 향하지 않습니다.");
                Assert.That(Vector3.Dot(shown * DiceFaceOrientation.GetOctaFaceUpAxis(face), screenUp),
                    Is.GreaterThan(0.999f), $"{face}번 면의 숫자가 누워 있습니다.");
            }
        }

        [Test]
        public void PipReengraveRotationKeepsSilhouetteByOctahedralSymmetry()
        {
            // 대칭 회전이면 축 꼭짓점이 다시 축 꼭짓점으로 간다. 아니면 몸체가 다른 모양으로 보인다.
            Vector3[] corners = { Vector3.right, Vector3.up, Vector3.forward };

            for (int physicalTop = 1; physicalTop <= 8; physicalTop++)
            {
                Quaternion landing = DiceFaceOrientation.GetOctaCameraFacingRotation(physicalTop, 90f);
                for (int face = 1; face <= 8; face++)
                {
                    Quaternion remap = DiceFaceOrientation.GetOctaVisualRemapRotation(landing, face);
                    foreach (Vector3 corner in corners)
                    {
                        Vector3 moved = remap * corner;
                        float best = Mathf.Max(Mathf.Abs(moved.x), Mathf.Abs(moved.y), Mathf.Abs(moved.z));
                        Assert.That(best, Is.GreaterThan(0.999f),
                            $"{physicalTop}번 면 착지에서 {face}번 면으로 바꿀 때 실루엣이 틀어집니다.");
                    }
                }
            }
        }

        [Test]
        public void OctahedronDiceUseRearPresetSlots()
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
        public void WithoutOctahedronDiceSlotOrderIsUnchanged()
        {
            var types = new[] { DieType.Normal, DieType.Golden, DieType.Sevens, DieType.HeavyRed, DieType.Couple };

            int[] order = YachtDiceRoundPresenter.BuildPresetSlotOrder(types);

            Assert.That(order, Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
        }
    }
}
