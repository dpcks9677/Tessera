using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// coin.glb 파서 고정 테스트다(M17-T23-2). 실측값은 Blender에서 내보낸 원본 파일 기준이다.
    /// </summary>
    [TestFixture]
    public sealed class CoinGlbReaderTests
    {
        private const string GlbPath = "Assets/Art/Source/Coin/coin.glb";

        [Test]
        public void ReadsBothPrimitivesWithExpectedCounts()
        {
            CoinGlbPrimitive[] primitives = CoinGlbReader.Read(File.ReadAllBytes(GlbPath));

            Assert.That(primitives.Length, Is.EqualTo(2));
            Assert.That(primitives[0].MaterialName, Is.EqualTo("Coin"));
            Assert.That(primitives[0].Positions.Length, Is.EqualTo(470));
            Assert.That(primitives[0].Indices.Length, Is.EqualTo(2526));
            Assert.That(primitives[1].MaterialName, Is.EqualTo("CoinRim"));
            Assert.That(primitives[1].Positions.Length, Is.EqualTo(586));
            Assert.That(primitives[1].Indices.Length, Is.EqualTo(2538));
        }

        [Test]
        public void CoinPrimitiveBoundsMatchReference()
        {
            CoinGlbPrimitive primitive = CoinGlbReader.Read(File.ReadAllBytes(GlbPath))[0];

            Bounds bounds = ComputeBounds(primitive.Positions);
            Assert.That(bounds.min.x, Is.EqualTo(-0.89589f).Within(1e-3f));
            Assert.That(bounds.max.x, Is.EqualTo(0.89589f).Within(1e-3f));
            Assert.That(bounds.min.y, Is.EqualTo(-0.09962f).Within(1e-3f));
            Assert.That(bounds.max.y, Is.EqualTo(0.09962f).Within(1e-3f));
            Assert.That(bounds.min.z, Is.EqualTo(-0.89668f).Within(1e-3f));
            Assert.That(bounds.max.z, Is.EqualTo(0.89668f).Within(1e-3f));
        }

        [Test]
        public void CoinRimPrimitiveBoundsMatchReference()
        {
            CoinGlbPrimitive primitive = CoinGlbReader.Read(File.ReadAllBytes(GlbPath))[1];

            Bounds bounds = ComputeBounds(primitive.Positions);
            Assert.That(bounds.min.x, Is.EqualTo(-0.99360f).Within(1e-3f));
            Assert.That(bounds.max.x, Is.EqualTo(0.99360f).Within(1e-3f));
            Assert.That(bounds.min.y, Is.EqualTo(-0.10000f).Within(1e-3f));
            Assert.That(bounds.max.y, Is.EqualTo(0.10014f).Within(1e-3f));
            Assert.That(bounds.min.z, Is.EqualTo(-0.99360f).Within(1e-3f));
            Assert.That(bounds.max.z, Is.EqualTo(0.99360f).Within(1e-3f));
        }

        [Test]
        public void AllTrianglesWindCounterClockwiseAgainstTheirNormal()
        {
            foreach (CoinGlbPrimitive primitive in CoinGlbReader.Read(File.ReadAllBytes(GlbPath)))
            {
                for (int t = 0; t < primitive.Indices.Length; t += 3)
                {
                    int i0 = primitive.Indices[t];
                    int i1 = primitive.Indices[t + 1];
                    int i2 = primitive.Indices[t + 2];
                    Vector3 p0 = primitive.Positions[i0];
                    Vector3 p1 = primitive.Positions[i1];
                    Vector3 p2 = primitive.Positions[i2];
                    Vector3 faceNormal = Vector3.Cross(p1 - p0, p2 - p0);
                    Vector3 vertexNormalSum = primitive.Normals[i0] + primitive.Normals[i1] + primitive.Normals[i2];

                    Assert.That(Vector3.Dot(faceNormal, vertexNormalSum), Is.GreaterThan(0f),
                        $"{primitive.MaterialName} 삼각형 {t / 3}의 와인딩이 법선과 어긋납니다.");
                }
            }
        }

        [Test]
        public void RejectsInvalidMagic()
        {
            byte[] glb = File.ReadAllBytes(GlbPath);
            glb[0] = (byte)'X';

            Assert.Throws<InvalidDataException>(() => CoinGlbReader.Read(glb));
        }

        private static Bounds ComputeBounds(Vector3[] positions)
        {
            var bounds = new Bounds(positions[0], Vector3.zero);
            for (int i = 1; i < positions.Length; i++) bounds.Encapsulate(positions[i]);
            return bounds;
        }
    }
}
