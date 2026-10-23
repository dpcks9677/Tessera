using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tessera.Core;
using Tessera.Rendering;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 정렬 주사위 크기 계약을 고정한다(PIXEL-T3).
    /// 정렬 크기가 정수 칸이어야 같은 눈이 같은 도트로 찍힌다.
    /// </summary>
    [TestFixture]
    public sealed class ArrangedDieSizeTests
    {
        [Test]
        public void ArrangedSizeIsWholeCellsAndSameWhenKept()
        {
            Assert.AreEqual(
                DiceBoardMetrics.ArrangedTopCells * PixelFilterSettings.CellWorldSize, DiceBoardMetrics.ActiveDieSize, 1e-5f);
            Assert.AreEqual(DiceBoardMetrics.ActiveDieSize, DiceBoardMetrics.KeptDieSize, 1e-5f);
        }

        [Test]
        public void ArrangedSizesAreNotSmallerThanBefore()
        {
            // 사용자 결정: 정렬 크기는 이전보다 줄이지 않는다.
            Assert.Greater(DiceBoardMetrics.ActiveDieSize, 1.014f);
            Assert.Greater(DiceBoardMetrics.KeptDieSize, 0.960f);
        }

        [Test]
        public void KeptDiceDoNotOverlapNeighbors()
        {
            // 소켓(17 소스 단위)보다 커서 넘치는 것은 사용자가 플레이 모드에서 판단하기로 했다.
            Assert.Less(DiceBoardMetrics.KeptDieSize, DiceBoardMetrics.KeepSpacingX);
        }

        private const string DiceFbxPath = "Assets/04.Art/Source/Dice/normal_dice.fbx";
        private const string BodyName = "Plain_D6";
        private const string PipPrefix = "Pip";

        [Test]
        public void ArrangedCellsSampleEveryPipAsSameThreeByThree()
        {
            List<string> masks = PipMasks(DiceBoardMetrics.ArrangedTopCells, 0f, 0f);

            Assert.AreEqual(21, masks.Count, "눈 21개를 찾아야 합니다.");
            Assert.AreEqual(1, masks.Distinct().Count(), "모든 눈의 마스크가 같아야 합니다:\n" + string.Join("\n", masks.Distinct()));
            string expected = string.Join(";", Enumerable.Range(-1, 3).SelectMany(x => Enumerable.Range(-1, 3).Select(y => $"{x},{y}")));
            Assert.AreEqual(expected, masks[0]);
        }

        [Test]
        public void ArrangedCellsKeepSameMaskUnderTenthCellJitter()
        {
            string reference = PipMasks(DiceBoardMetrics.ArrangedTopCells, 0f, 0f)[0];
            float[] offsets = { -0.1f, 0f, 0.1f };

            foreach (float ju in offsets)
            {
                foreach (float jv in offsets)
                {
                    List<string> masks = PipMasks(DiceBoardMetrics.ArrangedTopCells, ju, jv);
                    Assert.IsTrue(masks.All(m => m == reference), $"지터 ({ju}, {jv})에서 마스크가 달라졌습니다.");
                }
            }
        }

        [Test]
        public void EighteenCellsGiveDifferentMasks()
        {
            // 이전 크기(18칸)에서는 모서리 눈이 어긋나 마스크가 갈렸다. 이 테스트가 버그를 잡는 증거다.
            List<string> masks = PipMasks(18, 0f, 0f);

            Assert.Greater(masks.Distinct().Count(), 1);
        }

        [Test]
        public void FbxPipSpacingAndDiameterRatiosMatchDocumentedValues()
        {
            LoadPips(out float length, out List<PipData> pips);

            float spacing = float.MaxValue;
            foreach (IGrouping<(int axis, int sign), PipData> face in pips.GroupBy(p => (p.Axis, p.Sign)))
            {
                PipData[] list = face.ToArray();
                for (int i = 0; i < list.Length; i++)
                    for (int j = i + 1; j < list.Length; j++)
                        spacing = Mathf.Min(spacing, (list[i].Tangent - list[j].Tangent).magnitude);
            }

            float diameter = pips.Average(p => p.Diameter);
            Assert.AreEqual(0.2345f, spacing / length, 0.002f, $"눈 간격 비율 (L={length}, 간격={spacing})");
            Assert.AreEqual(0.1975f, diameter / length, 0.002f, $"눈 지름 비율 (L={length}, d={diameter})");
        }

        private struct PipData
        {
            public int Axis;
            public int Sign;
            public Vector2 Tangent;
            public float Diameter;
        }

        /// <summary>FBX 루트 기준으로 몸체 한 변 L과 눈(중심 접선 좌표, 지름, 면)을 구한다. 좌표는 몸체 중심 기준이다.</summary>
        private static void LoadPips(out float length, out List<PipData> pips)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(DiceFbxPath);
            Assert.IsNotNull(root, DiceFbxPath);

            Bounds? body = null;
            var pipBounds = new List<Bounds>();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                Bounds bounds = RootSpaceBounds(root.transform, filter);
                // 몸체는 루트 GameObject(normal_dice)에 붙어 있어 메시 이름으로 찾는다.
                if (filter.sharedMesh.name == BodyName) body = bounds;
                else if (filter.name.StartsWith(PipPrefix, StringComparison.Ordinal)) pipBounds.Add(bounds);
            }

            Assert.IsTrue(body.HasValue, BodyName + " 메시를 찾지 못했습니다.");
            Vector3 size = body.Value.size;
            length = Mathf.Max(size.x, size.y, size.z);

            pips = new List<PipData>();
            foreach (Bounds bounds in pipBounds)
            {
                Vector3 c = bounds.center - body.Value.center;
                int axis = Mathf.Abs(c.x) >= Mathf.Abs(c.y) && Mathf.Abs(c.x) >= Mathf.Abs(c.z) ? 0
                    : Mathf.Abs(c.y) >= Mathf.Abs(c.z) ? 1 : 2;
                int a = (axis + 1) % 3;
                int b = (axis + 2) % 3;
                pips.Add(new PipData
                {
                    Axis = axis,
                    Sign = c[axis] >= 0f ? 1 : -1,
                    Tangent = new Vector2(c[a], c[b]),
                    Diameter = Mathf.Max(bounds.size[a], bounds.size[b]),
                });
            }
        }

        private static Bounds RootSpaceBounds(Transform root, MeshFilter filter)
        {
            Matrix4x4 matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            Bounds local = filter.sharedMesh.bounds;
            var result = new Bounds(matrix.MultiplyPoint3x4(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                result.Encapsulate(matrix.MultiplyPoint3x4(corner));
            }
            return result;
        }

        /// <summary>
        /// 한 변이 cells칸인 주사위를 칸 중심(홀수)/칸 꼭짓점(짝수) 샘플로 찍었을 때 눈마다 덮는 샘플 집합을
        /// 눈 중심에 가장 가까운 샘플 기준 정수 오프셋 문자열로 돌려준다. 지터는 눈 중심을 u, v 방향으로 옮기는 칸 단위 값이다.
        /// </summary>
        private static List<string> PipMasks(int cells, float jitterU, float jitterV)
        {
            LoadPips(out float length, out List<PipData> pips);
            float scale = cells / length;
            bool odd = cells % 2 == 1;
            var masks = new List<string>();

            foreach (PipData pip in pips)
            {
                float u = (pip.Tangent.x * scale) + jitterU;
                float v = (pip.Tangent.y * scale) + jitterV;
                float radius = pip.Diameter * 0.5f * scale;

                // 샘플점: 홀수면 정수, 짝수면 반정수.
                float su = odd ? Mathf.Round(u) : Mathf.Floor(u) + 0.5f;
                float sv = odd ? Mathf.Round(v) : Mathf.Floor(v) + 0.5f;

                int range = Mathf.CeilToInt(radius) + 1;
                var offsets = new List<string>();
                for (int dx = -range; dx <= range; dx++)
                {
                    for (int dy = -range; dy <= range; dy++)
                    {
                        float distX = su + dx - u;
                        float distY = sv + dy - v;
                        if ((distX * distX) + (distY * distY) <= radius * radius) offsets.Add($"{dx},{dy}");
                    }
                }
                masks.Add(string.Join(";", offsets));
            }
            return masks;
        }
    }
}
