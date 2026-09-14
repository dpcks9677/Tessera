using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// <see cref="CoinMeshBaker.Split"/>만 검증한다. 에셋 IO(메시·텍스처·프리팹 저장)는 다루지 않는다(M17-T23-4).
    /// </summary>
    [TestFixture]
    public sealed class CoinMeshBakerTests
    {
        private const string GlbPath = "Assets/Art/Reference/coin.glb";

        private static CoinMeshSplit SplitReferenceCoin()
        {
            CoinGlbPrimitive[] primitives = CoinGlbReader.Read(File.ReadAllBytes(GlbPath));
            return CoinMeshBaker.Split(primitives);
        }

        [Test]
        public void SplitsCoinIntoHeadAndTailByTriangleCount()
        {
            CoinMeshSplit split = SplitReferenceCoin();

            Assert.That(split.Head.Indices.Length / 3, Is.EqualTo(428));
            Assert.That(split.Tail.Indices.Length / 3, Is.EqualTo(414));
            Assert.That(split.Edge.Indices.Length / 3, Is.EqualTo(846));
        }

        [Test]
        public void HeadAndTailVertexCountsSumToOriginalCoinPrimitive()
        {
            CoinMeshSplit split = SplitReferenceCoin();

            Assert.That(split.Head.Positions.Length + split.Tail.Positions.Length, Is.EqualTo(470));
        }

        [Test]
        public void HeadVerticesAreAboveEquatorAndTailVerticesBelow()
        {
            CoinMeshSplit split = SplitReferenceCoin();

            foreach (Vector3 p in split.Head.Positions) Assert.That(p.y, Is.GreaterThan(0f));
            foreach (Vector3 p in split.Tail.Positions) Assert.That(p.y, Is.LessThan(0f));
        }

        [Test]
        public void CapUvsStayWithinUnitRange()
        {
            CoinMeshSplit split = SplitReferenceCoin();

            foreach (Vector2 uv in split.Head.Uvs)
            {
                Assert.That(uv.x, Is.InRange(-1e-4f, 1f + 1e-4f));
                Assert.That(uv.y, Is.InRange(-1e-4f, 1f + 1e-4f));
            }
            foreach (Vector2 uv in split.Tail.Uvs)
            {
                Assert.That(uv.x, Is.InRange(-1e-4f, 1f + 1e-4f));
                Assert.That(uv.y, Is.InRange(-1e-4f, 1f + 1e-4f));
            }
        }

        [Test]
        public void CapUvIsCenteredOnCoinAxis()
        {
            // 데시메이트된 원반이라 캡 안쪽 정점이 없다(최소 반지름 0.817). 가장자리 링 정점의 평균 UV로
            // 투영 중심이 동전 축에 있는지 본다.
            CoinMeshSplit split = SplitReferenceCoin();

            Vector2 headCenterUv = MeanUv(split.Head);
            Vector2 tailCenterUv = MeanUv(split.Tail);

            Assert.That(headCenterUv.x, Is.EqualTo(0.5f).Within(0.05f));
            Assert.That(headCenterUv.y, Is.EqualTo(0.5f).Within(0.05f));
            Assert.That(tailCenterUv.x, Is.EqualTo(0.5f).Within(0.05f));
            Assert.That(tailCenterUv.y, Is.EqualTo(0.5f).Within(0.05f));
        }

        [Test]
        public void HeadAndTailFlipVAxisAtMaxZ()
        {
            CoinMeshSplit split = SplitReferenceCoin();

            float headV = VAtMaxZ(split.Head);
            float tailV = VAtMaxZ(split.Tail);

            Assert.That(headV, Is.GreaterThan(0.95f));
            Assert.That(tailV, Is.LessThan(0.05f));
        }

        [Test]
        public void FieldPlateauIsDeepenedBelowRimLip()
        {
            CoinMeshSplit split = SplitReferenceCoin();

            Assert.That(MinY(split.Head), Is.EqualTo(0.03f).Within(1e-3f));
            Assert.That(MaxY(split.Head), Is.EqualTo(0.0996f).Within(2e-3f));
            Assert.That(MaxY(split.Tail), Is.EqualTo(-0.03f).Within(1e-3f));
            Assert.That(MinY(split.Tail), Is.EqualTo(-0.0996f).Within(2e-3f));
        }

        [Test]
        public void FieldPlateauNormalsStayVertical()
        {
            CoinMeshSplit split = SplitReferenceCoin();

            for (int i = 0; i < split.Head.Positions.Length; i++)
                if (Mathf.Abs(split.Head.Positions[i].y) < 0.031f)
                    Assert.That(split.Head.Normals[i].y, Is.GreaterThan(0.99f));

            for (int i = 0; i < split.Tail.Positions.Length; i++)
                if (Mathf.Abs(split.Tail.Positions[i].y) < 0.031f)
                    Assert.That(split.Tail.Normals[i].y, Is.LessThan(-0.99f));
        }

        [Test]
        public void CapWindingMatchesNormalsAfterDeepening()
        {
            CoinMeshSplit split = SplitReferenceCoin();

            AssertWindingMatchesNormals(split.Head);
            AssertWindingMatchesNormals(split.Tail);
        }

        [Test]
        public void EdgeMeshKeepsOriginalRimVertexAndIndexCounts()
        {
            CoinMeshSplit split = SplitReferenceCoin();

            Assert.That(split.Edge.Positions.Length, Is.EqualTo(586));
            Assert.That(split.Edge.Indices.Length, Is.EqualTo(2538));
        }

        [Test]
        public void ThrowsWhenCoinPrimitiveHasNearVerticalTriangle()
        {
            var primitives = new[]
            {
                new CoinGlbPrimitive
                {
                    MaterialName = "Coin",
                    Positions = new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(1f, 1f, 0f) },
                    Normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward },
                    Uvs = new[] { Vector2.zero, Vector2.zero, Vector2.zero },
                    Indices = new[] { 0, 1, 2 }
                },
                new CoinGlbPrimitive
                {
                    MaterialName = "CoinRim",
                    Positions = new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(1f, 1f, 0f) },
                    Normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward },
                    Uvs = new[] { Vector2.zero, Vector2.zero, Vector2.zero },
                    Indices = new[] { 0, 1, 2 }
                }
            };

            Assert.Throws<InvalidDataException>(() => CoinMeshBaker.Split(primitives));
        }

        private static void AssertWindingMatchesNormals(CoinMeshData cap)
        {
            for (int t = 0; t < cap.Indices.Length; t += 3)
            {
                int i0 = cap.Indices[t];
                int i1 = cap.Indices[t + 1];
                int i2 = cap.Indices[t + 2];
                Vector3 p0 = cap.Positions[i0];
                Vector3 p1 = cap.Positions[i1];
                Vector3 p2 = cap.Positions[i2];
                Vector3 faceNormal = Vector3.Cross(p1 - p0, p2 - p0);
                Vector3 vertexNormalSum = cap.Normals[i0] + cap.Normals[i1] + cap.Normals[i2];

                Assert.That(Vector3.Dot(faceNormal, vertexNormalSum), Is.GreaterThan(0f));
            }
        }

        private static float MinY(CoinMeshData cap)
        {
            float min = float.MaxValue;
            foreach (Vector3 p in cap.Positions) min = Mathf.Min(min, p.y);
            return min;
        }

        private static float MaxY(CoinMeshData cap)
        {
            float max = float.MinValue;
            foreach (Vector3 p in cap.Positions) max = Mathf.Max(max, p.y);
            return max;
        }

        private static Vector2 MeanUv(CoinMeshData cap)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < cap.Uvs.Length; i++) sum += cap.Uvs[i];
            return sum / cap.Uvs.Length;
        }

        private static float VAtMaxZ(CoinMeshData cap)
        {
            int maxIndex = 0;
            float maxZ = float.MinValue;
            for (int i = 0; i < cap.Positions.Length; i++)
            {
                if (cap.Positions[i].z > maxZ)
                {
                    maxZ = cap.Positions[i].z;
                    maxIndex = i;
                }
            }
            return cap.Uvs[maxIndex].y;
        }
    }
}
