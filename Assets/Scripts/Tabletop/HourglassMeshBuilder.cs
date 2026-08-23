using System.Collections.Generic;
using UnityEngine;

namespace Tessera.Tabletop
{
    internal sealed class HourglassSandMesh
    {
        internal readonly Mesh Mesh;
        internal readonly Vector2[] Profile;
        internal readonly Vector3[] Vertices;

        internal HourglassSandMesh(string name, int profileCount, int segments)
        {
            Mesh = new Mesh { name = name };
            Mesh.MarkDynamic();
            Profile = new Vector2[profileCount];
            Vertices = new Vector3[profileCount * (segments + 1)];
        }

        internal HourglassSandMesh(Mesh mesh, int profileCount, int segments)
        {
            Mesh = mesh;
            Mesh.MarkDynamic();
            Profile = new Vector2[profileCount];

            int vertexCount = profileCount * (segments + 1);
            Vector3[] existingVertices = mesh.vertices;
            Vertices = existingVertices.Length == vertexCount
                ? existingVertices
                : new Vector3[vertexCount];
        }
    }

    /// <summary>
    /// 모래시계의 연속 유리 표면과 중력 방향 모래 표면을 만드는 절차적 메쉬 도우미입니다.
    /// </summary>
    internal static class HourglassMeshBuilder
    {
        private const int GlassSegments = 32;
        private const int SandSegments = 24;
        private const int SandRadialSteps = 10;

        private const float ChamberRadius = 0.36f;
        private const float ChamberBottom = 0.92f;
        private const float ChamberNeck = 0.08f;
        private const float SandSlope = 0.675f; // 약 34도의 안식각

        internal static Mesh CreateUnifiedGlassMesh()
        {
            // 아래 캡 소켓부터 위 캡 소켓까지 끊기지 않는 한 장의 회전체 표면입니다.
            Vector2[] profile =
            {
                new(0.34f, -1.03f), new(0.37f, -0.96f), new(0.40f, -0.84f),
                new(0.43f, -0.64f), new(0.42f, -0.48f), new(0.37f, -0.31f),
                new(0.26f, -0.16f), new(0.15f, -0.065f), new(0.105f, 0f),
                new(0.15f, 0.065f), new(0.26f, 0.16f), new(0.37f, 0.31f),
                new(0.42f, 0.48f), new(0.43f, 0.64f), new(0.40f, 0.84f),
                new(0.37f, 0.96f), new(0.34f, 1.03f)
            };

            Mesh mesh = CreateLatheMesh("Hourglass_UnifiedGlassMesh", profile, GlassSegments);
            return mesh;
        }

        internal static HourglassSandMesh CreateSandMesh(string name)
        {
            int profileCount = (SandRadialSteps + 1) * 2;
            HourglassSandMesh sand = new(name, profileCount, SandSegments);
            sand.Mesh.vertices = sand.Vertices;
            sand.Mesh.triangles = BuildLatheTriangles(profileCount, SandSegments);
            return sand;
        }

        internal static HourglassSandMesh BindSandMesh(Mesh mesh)
        {
            int profileCount = (SandRadialSteps + 1) * 2;
            return mesh == null ? null : new HourglassSandMesh(mesh, profileCount, SandSegments);
        }

        internal static void UpdateSourceSand(HourglassSandMesh sand, float chamberSign, float amount)
        {
            float clamped = Mathf.Clamp01(amount);
            float visibleRadius = ChamberRadius * Mathf.Pow(clamped, 0.28f);
            float level = Mathf.Lerp(ChamberNeck + 0.015f, 0.84f, Mathf.Pow(clamped, 0.78f));
            float funnelDepth = Mathf.Lerp(0.018f, 0.075f, Mathf.SmoothStep(0f, 1f, clamped));
            Vector2[] profile = sand.Profile;

            for (int i = 0; i <= SandRadialSteps; i++)
            {
                float t = i / (float)SandRadialSteps;
                float radius = visibleRadius * t;
                float support = SourceSupport(radius);
                float funnel = funnelDepth * (1f - t) * (1f - t);
                float surface = Mathf.Max(support + 0.004f, level - funnel);
                profile[i] = new Vector2(radius, surface * chamberSign);
            }

            for (int i = 0; i <= SandRadialSteps; i++)
            {
                int sourceIndex = SandRadialSteps - i;
                float radius = visibleRadius * sourceIndex / SandRadialSteps;
                profile[SandRadialSteps + 1 + i] = new Vector2(radius, SourceSupport(radius) * chamberSign);
            }

            UpdateLatheMesh(sand, SandSegments);
        }

        internal static float UpdateAccumulatedSand(HourglassSandMesh sand, float chamberSign, float amount)
        {
            float clamped = Mathf.Clamp01(amount);
            float apex = GetPileApex(clamped);
            float visibleRadius = ChamberRadius * Mathf.Pow(clamped, 0.30f);
            Vector2[] profile = sand.Profile;

            // 자유 표면은 안식각을 유지하고, 그 아래는 둥근 유리 바닥을 따라 채웁니다.
            for (int i = 0; i <= SandRadialSteps; i++)
            {
                float t = i / (float)SandRadialSteps;
                float radius = visibleRadius * t;
                float floor = PileFloor(radius);
                float surface = Mathf.Min(floor - 0.004f, apex + SandSlope * radius);
                profile[i] = new Vector2(radius, -surface * chamberSign);
            }

            for (int i = 0; i <= SandRadialSteps; i++)
            {
                int sourceIndex = SandRadialSteps - i;
                float radius = visibleRadius * sourceIndex / SandRadialSteps;
                profile[SandRadialSteps + 1 + i] = new Vector2(radius, -PileFloor(radius) * chamberSign);
            }

            UpdateLatheMesh(sand, SandSegments);
            return -apex * chamberSign;
        }

        private static float GetPileApex(float amount)
        {
            // 낮은 구간에서는 밑면이 먼저 퍼지고, 이후 유리벽을 따라 높이가 올라갑니다.
            float growth = Mathf.Pow(Mathf.Clamp01(amount), 0.62f);
            return Mathf.Lerp(ChamberBottom - 0.012f, ChamberNeck, growth);
        }

        private static float SourceSupport(float radius)
        {
            float t = Mathf.Clamp01(radius / ChamberRadius);
            return ChamberNeck + 0.54f * Mathf.Pow(t, 1.65f);
        }

        private static float PileFloor(float radius)
        {
            float t = Mathf.Clamp01(radius / ChamberRadius);
            return ChamberBottom - 0.34f * t * t;
        }

        private static Mesh CreateLatheMesh(string name, IReadOnlyList<Vector2> profile, int segments)
        {
            Mesh mesh = new() { name = name };
            mesh.vertices = BuildLatheVertices(profile, segments);
            mesh.triangles = BuildLatheTriangles(profile.Count, segments);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void UpdateLatheMesh(HourglassSandMesh sand, int segments)
        {
            BuildLatheVertices(sand.Profile, segments, sand.Vertices);
            sand.Mesh.vertices = sand.Vertices;
            sand.Mesh.RecalculateNormals();
            sand.Mesh.RecalculateBounds();
        }

        private static Vector3[] BuildLatheVertices(IReadOnlyList<Vector2> profile, int segments)
        {
            Vector3[] vertices = new Vector3[profile.Count * (segments + 1)];
            BuildLatheVertices(profile, segments, vertices);
            return vertices;
        }

        private static void BuildLatheVertices(IReadOnlyList<Vector2> profile, int segments, Vector3[] vertices)
        {
            for (int segment = 0; segment <= segments; segment++)
            {
                float angle = segment / (float)segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                for (int point = 0; point < profile.Count; point++)
                {
                    Vector2 value = profile[point];
                    vertices[segment * profile.Count + point] = new Vector3(value.x * cos, value.y, value.x * sin);
                }
            }
        }

        private static int[] BuildLatheTriangles(int profileCount, int segments)
        {
            int[] triangles = new int[segments * (profileCount - 1) * 6];
            int index = 0;
            for (int segment = 0; segment < segments; segment++)
            {
                int current = segment * profileCount;
                int next = (segment + 1) * profileCount;
                for (int point = 0; point < profileCount - 1; point++)
                {
                    triangles[index++] = current + point;
                    triangles[index++] = next + point + 1;
                    triangles[index++] = next + point;
                    triangles[index++] = current + point;
                    triangles[index++] = current + point + 1;
                    triangles[index++] = next + point + 1;
                }
            }
            return triangles;
        }
    }
}
