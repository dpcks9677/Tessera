using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Dice;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 구워 둔 특수 주사위 형상이 규격을 지키는지 본다(M7-T5).
    /// 베이커 상수를 고쳐 다시 구웠을 때 형상이 무너지면 여기서 잡힌다.
    /// </summary>
    [TestFixture]
    public sealed class DiceShapeAssetTests
    {
        // DiceShapeBaker의 형상 상수와 같은 값을 쓴다. 베이커에서 바꾸면 여기도 함께 고친다.
        private const float OctRadius = 0.98f;
        private const float BevelRadius = 0.13f;
        private const float FaceDistance = OctRadius / 1.7320508f + BevelRadius; // ≈ 0.7396

        private static readonly Vector3[] FaceDirections =
        {
            new(1f, 1f, 1f), new(1f, -1f, 1f), new(1f, -1f, -1f), new(1f, 1f, -1f),
            new(-1f, 1f, -1f), new(-1f, -1f, -1f), new(-1f, -1f, 1f), new(-1f, 1f, 1f)
        };

        [Test]
        public void 팔면몸체는_반지름과_베벨_규격_안에_있다()
        {
            Mesh body = LoadOctahedronBody();

            Assert.That(body.subMeshCount, Is.EqualTo(1),
                "서브메시가 여러 개면 런타임이 몸체·홈 두 재질만 꽂아 일부 면이 사라진다.");

            float maxDistance = 0f;
            foreach (Vector3 v in body.vertices) maxDistance = Mathf.Max(maxDistance, v.magnitude);
            Assert.That(maxDistance, Is.LessThanOrEqualTo(OctRadius + BevelRadius + 0.001f));

            Bounds bounds = body.bounds;
            Assert.That(bounds.center.magnitude, Is.LessThan(0.01f), "형상이 원점 대칭이 아닙니다.");
            Assert.That(bounds.extents.x, Is.EqualTo(bounds.extents.y).Within(0.01f));
            Assert.That(bounds.extents.y, Is.EqualTo(bounds.extents.z).Within(0.01f));
        }

        [Test]
        public void 팔면몸체는_여덟_방향에_평평한_면을_가진다()
        {
            Mesh body = LoadOctahedronBody();
            Vector3[] vertices = body.vertices;

            foreach (Vector3 dir in FaceDirections)
            {
                Vector3 n = dir.normalized;
                int onFace = 0;
                foreach (Vector3 v in vertices)
                {
                    if (Mathf.Abs(Vector3.Dot(v, n) - FaceDistance) < 0.001f) onFace++;
                }

                Assert.That(onFace, Is.GreaterThanOrEqualTo(3),
                    $"{dir} 방향에 평면부 정점이 {onFace}개뿐입니다. 면이 만들어지지 않았습니다.");
            }
        }

        [Test]
        public void 팔면몸체의_면은_바깥을_향한다()
        {
            Mesh body = LoadOctahedronBody();
            Vector3[] vertices = body.vertices;
            Vector3[] normals = body.normals;
            int[] triangles = body.triangles;

            int outward = 0;
            int inward = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 center = (vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3f;
                Vector3 normal = (normals[triangles[i]] + normals[triangles[i + 1]] + normals[triangles[i + 2]]) / 3f;
                if (center.sqrMagnitude < 0.0001f) continue;

                if (Vector3.Dot(normal.normalized, center.normalized) > 0f) outward++;
                else inward++;
            }

            Assert.That(inward, Is.Zero,
                $"안쪽을 향한 삼각형이 {inward}개입니다(바깥 {outward}개). 뒷면 컬링으로 몸체가 사라집니다.");
        }

        [Test]
        public void 팔면프리팹은_면마다_값에_맞는_숫자를_하나씩_새긴다()
        {
            GameObject prefab = LoadPrefab("Die_Octahedron");
            int[] faceValues = DiceFaceValues.Get(DieType.Octahedron);

            for (int face = 1; face <= 8; face++)
            {
                Transform group = prefab.transform.Find($"Pip_{face}");
                Assert.That(group, Is.Not.Null, $"{face}번 면의 눈 묶음이 없습니다.");
                Assert.That(group.childCount, Is.EqualTo(1), $"{face}번 면에는 숫자 하나만 새긴다.");

                MeshFilter filter = group.GetChild(0).GetComponent<MeshFilter>();
                Assert.That(filter, Is.Not.Null);
                Assert.That(filter.sharedMesh.name, Is.EqualTo($"Dice_Digit_{faceValues[face - 1]}"),
                    $"{face}번 면에는 {faceValues[face - 1]}이 새겨져야 합니다.");
            }
        }

        [Test]
        public void 팔면프리팹의_눈은_해당_면_위에_놓인다()
        {
            GameObject prefab = LoadPrefab("Die_Octahedron");

            for (int face = 1; face <= 8; face++)
            {
                Vector3 n = FaceDirections[face - 1].normalized;
                foreach (Transform dot in prefab.transform.Find($"Pip_{face}"))
                {
                    float height = Vector3.Dot(dot.localPosition, n);
                    Assert.That(height, Is.EqualTo(FaceDistance).Within(0.05f),
                        $"{face}번 면의 눈이 면 평면에서 벗어났습니다.");

                    Vector3 planar = dot.localPosition - n * height;
                    Assert.That(planar.magnitude, Is.LessThan(0.30f),
                        $"{face}번 면의 눈이 삼각형 밖으로 밀려났습니다.");
                }
            }
        }

        [Test]
        public void 세븐스프리팹의_면값은_2부터7까지다()
        {
            GameObject prefab = LoadPrefab("Die_Sevens");
            int[] expected = DiceFaceValues.Get(DieType.Sevens);
            Dictionary<int, int> counts = CountPipsPerFace(prefab);

            for (int face = 1; face <= 6; face++)
            {
                Assert.That(counts.TryGetValue(face, out int engraved) ? engraved : 0, Is.EqualTo(expected[face - 1]),
                    $"{face}번 면에 {expected[face - 1]}눈이 새겨져야 합니다.");
            }
        }

        [Test]
        public void 세븐스프리팹의_눈은_각_면_평면_위에_고르게_놓인다()
        {
            GameObject prefab = LoadPrefab("Die_Sevens");
            var faceSums = new Dictionary<int, Vector3>();
            var faceCounts = new Dictionary<int, int>();

            foreach (Transform dot in prefab.GetComponentsInChildren<Transform>(true))
            {
                int face = ParseFace(dot.name);
                if (face < 1) continue;
                faceSums.TryGetValue(face, out Vector3 sum);
                faceSums[face] = sum + dot.localPosition;
                faceCounts.TryGetValue(face, out int count);
                faceCounts[face] = count + 1;
            }

            foreach (KeyValuePair<int, Vector3> entry in faceSums)
            {
                // 눈이 면 중심을 기준으로 대칭 배치되므로 평균은 면 중심에 놓인다.
                Vector3 average = entry.Value / faceCounts[entry.Key];
                Assert.That(average.magnitude, Is.GreaterThan(0.5f),
                    $"{entry.Key}번 면의 눈 평균이 주사위 중심에 너무 가깝습니다. 면 위에 놓이지 않았습니다.");
            }
        }

        private static Dictionary<int, int> CountPipsPerFace(GameObject prefab)
        {
            var counts = new Dictionary<int, int>();
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                int face = ParseFace(t.name);
                if (face < 1) continue;
                counts.TryGetValue(face, out int current);
                counts[face] = current + 1;
            }
            return counts;
        }

        private static int ParseFace(string name)
        {
            if (!name.StartsWith("Pip_")) return -1;
            string digits = name.Substring(4).Split('.')[0];
            return int.TryParse(digits, out int face) ? face : -1;
        }

        private static Mesh LoadOctahedronBody()
        {
            Mesh body = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Generated/Dice/Dice_Octahedron_Body.mesh");
            Assert.That(body, Is.Not.Null, "8면 몸체 메시가 없습니다. Tessera/Bake/Dice Shapes 로 먼저 구우십시오.");
            return body;
        }

        private static GameObject LoadPrefab(string assetName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Dice/{assetName}.prefab");
            Assert.That(prefab, Is.Not.Null, $"{assetName} 프리팹이 없습니다. Tessera/Bake/Dice Shapes 로 먼저 구우십시오.");
            return prefab;
        }
    }
}
