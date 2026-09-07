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
    /// 규칙이 만들어 내는 눈과 주사위 면에 새겨진 눈이 어긋나지 않는지 본다(M7-T5).
    /// 이 대조가 깨지면 화면이 실제 굴림 결과와 다른 값을 보여 준다.
    /// </summary>
    [TestFixture]
    public sealed class DiceFaceValueTests
    {
        [Test]
        public void 세븐스주사위는_2부터7까지를_여섯면에_하나씩_새긴다()
        {
            var used = new HashSet<int>();

            for (int value = 2; value <= 7; value++)
            {
                int face = DiceFaceValues.FaceIndexOf(DieType.Sevens, value);
                Assert.That(face, Is.InRange(1, 6));
                Assert.That(used.Add(face), Is.True, $"값 {value}가 이미 쓰인 면 {face}를 다시 가리킵니다.");
            }

            Assert.That(used.Count, Is.EqualTo(6));
        }

        [Test]
        public void 팔면주사위는_여덟면을_1부터6까지에_대응시킨다()
        {
            int[] faces = DiceFaceValues.Get(DieType.Octahedron);
            Assert.That(faces, Is.Not.Null);
            Assert.That(faces.Length, Is.EqualTo(8));
            Assert.That(DiceFaceValues.FaceCount(DieType.Octahedron), Is.EqualTo(8));

            for (int value = 1; value <= 6; value++)
            {
                int face = DiceFaceValues.FaceIndexOf(DieType.Octahedron, value);
                Assert.That(face, Is.InRange(1, 8));
                Assert.That(faces[face - 1], Is.EqualTo(value));
            }
        }

        [Test]
        public void 일반주사위는_값과_면번호가_같다()
        {
            Assert.That(DiceFaceValues.Get(DieType.Normal), Is.Null);
            for (int value = 1; value <= 6; value++)
            {
                Assert.That(DiceFaceValues.FaceIndexOf(DieType.Normal, value), Is.EqualTo(value));
            }
        }

        [Test]
        public void 규칙이_만드는_눈은_모두_새겨진_면을_가진다()
        {
            foreach (YachtDieType logical in Enum.GetValues(typeof(YachtDieType)))
            {
                DieType visual = YachtDieVisuals.Resolve(logical);
                int[] faces = DiceFaceValues.Get(visual);

                foreach (int value in RuleValuesOf(logical))
                {
                    int face = DiceFaceValues.FaceIndexOf(visual, value);
                    int engraved = faces != null ? faces[face - 1] : face;
                    Assert.That(engraved, Is.EqualTo(value),
                        $"{logical}의 눈 {value}를 보여 줄 면이 없어 {engraved}이(가) 대신 표시됩니다.");
                }
            }
        }

        [Test]
        public void 세븐스의_눈7은_회전계산을_터뜨리지_않는다()
        {
            // 값 7을 그대로 넘기면 FaceNormals[6]을 읽어 예외가 났다. 면 인덱스로 옮기면 6면 안에 들어온다.
            int face = DiceFaceValues.FaceIndexOf(DieType.Sevens, 7);
            Assert.That(face, Is.InRange(1, 6));

            Quaternion landing = Quaternion.Euler(37f, 128f, 264f);
            Quaternion remap = DiceFaceOrientation.GetVisualRemapRotation(landing, face, Quaternion.identity);
            Assert.That(DiceFaceOrientation.GetTopValue(landing * remap), Is.EqualTo(face));
        }

        [Test]
        public void 어떤_착지회전에서도_목표면이_위로_온다()
        {
            UnityEngine.Random.InitState(20260904);

            for (int i = 0; i < 200; i++)
            {
                Quaternion landing = UnityEngine.Random.rotationUniform;
                int face = UnityEngine.Random.Range(1, 7);
                Quaternion remap = DiceFaceOrientation.GetVisualRemapRotation(landing, face, Quaternion.identity);
                Assert.That(DiceFaceOrientation.GetTopValue(landing * remap), Is.EqualTo(face));
            }
        }

        /// <summary>규칙 계층이 이 종류에서 만들 수 있는 눈. YachtAugmentRuntime.RollValue와 같은 표다.</summary>
        private static int[] RuleValuesOf(YachtDieType type)
        {
            return type switch
            {
                YachtDieType.Heavy => new[] { 4, 4, 5, 5, 6, 6 },
                YachtDieType.Octahedron => new[] { 1, 2, 3, 4, 4, 5, 5, 6 },
                YachtDieType.Sevens => new[] { 2, 3, 4, 5, 6, 7 },
                YachtDieType.Promotion => new[] { 1, 2, 3, 4, 5, 6 },
                _ => new[] { 1, 2, 3, 4, 5, 6 }
            };
        }
    }
}
