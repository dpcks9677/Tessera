using System.Collections.Generic;
using System.IO;
using Tessera.Core;
using Tessera.Dice;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 특수 주사위 형상을 구워 정적 자산으로 남긴다(M7-T5).
///
/// 8면 주사위는 원본 augmented-dice의 preset-studio/src/geometryUtils.js:81-240 알고리즘을 옮겨
/// 베벨 8면체를 만들고, 세븐스 주사위는 기존 D6 모델의 1번 면을 7눈 면으로 바꾼다.
/// 눈 메시와 몸체 규약(Pip* 이름, ShadowProxy)은 D6 모델을 그대로 따라 런타임 코드를 건드리지 않는다.
///
/// 자동 호출하지 않는다. 형상 상수를 바꾼 뒤 메뉴로 다시 굽고 결과 자산을 커밋한다.
/// </summary>
public static class DiceShapeBaker
{
    // 원본 geometryUtils.js:82-84. 주사위 원본 규격(1.62) 단위를 그대로 쓴다.
    // 런타임이 1/1.62로 정규화하므로 여기서 Unity 단위로 환산하지 않는다.
    // 원본은 0.90이지만 트레이 정렬 간격(DiceBoardMetrics.ActiveSpacing = 1.5)이 허용하는 한계까지 키웠다.
    // 저해상도 렌더에서 8면체가 D6보다 작게 읽혀 면에 새긴 숫자가 뭉개졌기 때문이다.
    private const float OctRadius = 0.98f;
    // 원본은 0.22지만 그만큼 두면 둥근 모서리가 앰버 키라이트를 받아 흰 테두리로 번지고
    // 저해상도에서 8면체가 흰 덩어리로 읽힌다. 평평한 면을 넓히려고 좁혔다.
    private const float BevelRadius = 0.13f;
    private const int BevelSegments = 6;

    // 삼각형 면의 평면부는 내접원이 약 0.212다. 점 여섯 개를 넣으면 저해상도에서 한 덩어리로 뭉쳐
    // 원본과 같이 숫자를 새긴다(preset-studio/src/diceMaterials.js:76-77).
    // 이 씬의 앰버 키라이트가 강해 숫자를 키우면 획이 포화해 주사위 전체가 흰 덩어리로 읽힌다.
    // 형상과 색으로 종류를 구분하고, 숫자는 뭉개지지 않는 최대 크기까지만 새긴다.
    private const float DigitHeight = 0.22f;
    private const float DigitWidth = 0.13f;
    private const float DigitStroke = 0.030f;
    private const float PipSurfaceLift = 0.01f;

    private const string DiceModelPath = "Assets/Art/Reference/normal_dice.fbx";
    private const string MeshFolder = "Assets/Art/Generated/Dice";
    private const string PrefabFolder = "Assets/Prefabs/Dice";

    /// <summary>원본 geometryUtils.js:110-113의 8면 방향 순서. 배열 위치가 곧 면 인덱스 - 1이다.</summary>
    private static readonly Vector3[] OctFaceDirections =
    {
        new(1f, 1f, 1f), new(1f, -1f, 1f), new(1f, -1f, -1f), new(1f, 1f, -1f),
        new(-1f, 1f, -1f), new(-1f, -1f, -1f), new(-1f, -1f, 1f), new(-1f, 1f, 1f)
    };

    [MenuItem("Tessera/Bake/Dice Shapes")]
    public static void BakeAll()
    {
        GameObject diceModel = AssetDatabase.LoadAssetAtPath<GameObject>(DiceModelPath);
        if (diceModel == null)
        {
            Debug.LogError($"[DiceShapeBaker] 주사위 모델을 찾지 못했습니다: {DiceModelPath}");
            return;
        }

        Directory.CreateDirectory(MeshFolder);
        Directory.CreateDirectory(PrefabFolder);

        Mesh octBody = SaveMesh(BuildOctahedronBody(), "Dice_Octahedron_Body");
        var digits = new Mesh[6];
        for (int value = 1; value <= 6; value++) digits[value - 1] = SaveMesh(BuildDigit(value), $"Dice_Digit_{value}");
        BakeOctahedronPrefab(octBody, digits);
        BakeSevensPrefab(diceModel);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[DiceShapeBaker] 8면·세븐스 주사위 형상을 다시 구웠습니다.");
    }

    // ---------------------------------------------------------------- 8면체 몸체

    /// <summary>
    /// 베벨 8면체를 만든다. 면 8개는 삼각형 하나씩이고, 모서리 12개와 꼭짓점 6개가 베벨을 채운다.
    /// 서브메시는 하나로 합친다. 런타임이 서브메시 2개 이상이면 몸체·홈 두 재질만 꽂기 때문이다.
    /// </summary>
    private static Mesh BuildOctahedronBody()
    {
        var vertices = new List<Vector3>();

        for (int i = 0; i < OctFaceDirections.Length; i++)
        {
            Vector3 dir = OctFaceDirections[i];
            Vector3 n = dir.normalized;
            AddTriangle(vertices,
                new Vector3(dir.x * OctRadius, 0f, 0f) + n * BevelRadius,
                new Vector3(0f, dir.y * OctRadius, 0f) + n * BevelRadius,
                new Vector3(0f, 0f, dir.z * OctRadius) + n * BevelRadius);
        }

        AddEdgeBevels(vertices);
        AddCornerBevels(vertices);

        var mesh = new Mesh { name = "Dice_Octahedron_Body" };
        mesh.SetVertices(vertices);

        int[] triangles = new int[vertices.Count];
        for (int i = 0; i < triangles.Length; i++) triangles[i] = i;
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddEdgeBevels(List<Vector3> vertices)
    {
        // 원본 geometryUtils.js:149-162. 축 두 개를 잇는 모서리마다 인접한 두 면 법선을 보간한다.
        (Vector3 a, Vector3 b, Vector3 d1, Vector3 d2)[] edges =
        {
            (new(OctRadius, 0f, 0f), new(0f, OctRadius, 0f), new(1f, 1f, 1f), new(1f, 1f, -1f)),
            (new(OctRadius, 0f, 0f), new(0f, -OctRadius, 0f), new(1f, -1f, 1f), new(1f, -1f, -1f)),
            (new(-OctRadius, 0f, 0f), new(0f, OctRadius, 0f), new(-1f, 1f, 1f), new(-1f, 1f, -1f)),
            (new(-OctRadius, 0f, 0f), new(0f, -OctRadius, 0f), new(-1f, -1f, 1f), new(-1f, -1f, -1f)),

            (new(OctRadius, 0f, 0f), new(0f, 0f, OctRadius), new(1f, 1f, 1f), new(1f, -1f, 1f)),
            (new(OctRadius, 0f, 0f), new(0f, 0f, -OctRadius), new(1f, 1f, -1f), new(1f, -1f, -1f)),
            (new(-OctRadius, 0f, 0f), new(0f, 0f, OctRadius), new(-1f, 1f, 1f), new(-1f, -1f, 1f)),
            (new(-OctRadius, 0f, 0f), new(0f, 0f, -OctRadius), new(-1f, 1f, -1f), new(-1f, -1f, -1f)),

            (new(0f, OctRadius, 0f), new(0f, 0f, OctRadius), new(1f, 1f, 1f), new(-1f, 1f, 1f)),
            (new(0f, OctRadius, 0f), new(0f, 0f, -OctRadius), new(1f, 1f, -1f), new(-1f, 1f, -1f)),
            (new(0f, -OctRadius, 0f), new(0f, 0f, OctRadius), new(1f, -1f, 1f), new(-1f, -1f, 1f)),
            (new(0f, -OctRadius, 0f), new(0f, 0f, -OctRadius), new(1f, -1f, -1f), new(-1f, -1f, -1f))
        };

        foreach ((Vector3 a, Vector3 b, Vector3 d1, Vector3 d2) edge in edges)
        {
            Vector3 n1 = edge.d1.normalized;
            Vector3 n2 = edge.d2.normalized;

            for (int s = 0; s < BevelSegments; s++)
            {
                Vector3 nA = Vector3.Lerp(n1, n2, s / (float)BevelSegments).normalized;
                Vector3 nB = Vector3.Lerp(n1, n2, (s + 1) / (float)BevelSegments).normalized;

                AddQuad(vertices,
                    edge.a + nA * BevelRadius,
                    edge.b + nA * BevelRadius,
                    edge.b + nB * BevelRadius,
                    edge.a + nB * BevelRadius);
            }
        }
    }

    private static void AddCornerBevels(List<Vector3> vertices)
    {
        // 원본 geometryUtils.js:196-233. 축 꼭짓점 6개를 구면 사각형으로 덮는다.
        Vector3[] corners =
        {
            new(OctRadius, 0f, 0f), new(-OctRadius, 0f, 0f),
            new(0f, OctRadius, 0f), new(0f, -OctRadius, 0f),
            new(0f, 0f, OctRadius), new(0f, 0f, -OctRadius)
        };

        foreach (Vector3 corner in corners)
        {
            for (int i = 0; i < BevelSegments; i++)
            {
                for (int j = 0; j < BevelSegments; j++)
                {
                    float u1 = -1f + 2f * (i / (float)BevelSegments);
                    float u2 = -1f + 2f * ((i + 1) / (float)BevelSegments);
                    float v1 = -1f + 2f * (j / (float)BevelSegments);
                    float v2 = -1f + 2f * ((j + 1) / (float)BevelSegments);

                    AddQuad(vertices,
                        corner + CornerNormal(corner, u1, v1) * BevelRadius,
                        corner + CornerNormal(corner, u2, v1) * BevelRadius,
                        corner + CornerNormal(corner, u2, v2) * BevelRadius,
                        corner + CornerNormal(corner, u1, v2) * BevelRadius);
                }
            }
        }
    }

    private static Vector3 CornerNormal(Vector3 corner, float u, float v)
    {
        if (!Mathf.Approximately(corner.x, 0f)) return new Vector3(Mathf.Sign(corner.x), u, v).normalized;
        if (!Mathf.Approximately(corner.y, 0f)) return new Vector3(u, Mathf.Sign(corner.y), v).normalized;
        return new Vector3(u, v, Mathf.Sign(corner.z)).normalized;
    }

    /// <summary>바깥을 향하도록 감는다. 안쪽을 향하면 두 정점을 맞바꾼다(원본 addTri와 같은 판정).</summary>
    private static void AddTriangle(List<Vector3> vertices, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 center = (v0 + v1 + v2) / 3f;
        Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);

        if (Vector3.Dot(normal, center) < 0f)
        {
            vertices.Add(v0);
            vertices.Add(v2);
            vertices.Add(v1);
        }
        else
        {
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
        }
    }

    private static void AddQuad(List<Vector3> vertices, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        AddTriangle(vertices, v0, v1, v2);
        AddTriangle(vertices, v0, v2, v3);
    }

    // ---------------------------------------------------------------- 프리팹 조립

    /// <summary>
    /// 7세그먼트 모양의 숫자 글리프. XZ 평면에 눕고 법선은 +Y라 어느 면에든 회전만으로 붙는다.
    /// 삼각형 면에는 점 여섯 개가 들어가지 않아 원본도 8면체만 숫자를 썼다.
    /// </summary>
    private static Mesh BuildDigit(int value)
    {
        float halfHeight = DigitHeight * 0.5f;
        float halfWidth = DigitWidth * 0.5f;
        float inset = DigitStroke * 0.5f;
        float armLength = halfHeight * 0.5f;

        var vertices = new List<Vector3>();

        // 가로 획: 위(a) 가운데(g) 아래(d)
        if (HasSegment(value, 'a')) AddBar(vertices, new Vector2(0f, halfHeight - inset), DigitWidth, DigitStroke);
        if (HasSegment(value, 'g')) AddBar(vertices, Vector2.zero, DigitWidth, DigitStroke);
        if (HasSegment(value, 'd')) AddBar(vertices, new Vector2(0f, -halfHeight + inset), DigitWidth, DigitStroke);

        // 세로 획: 왼쪽 위(f) 오른쪽 위(b) 왼쪽 아래(e) 오른쪽 아래(c)
        if (HasSegment(value, 'f')) AddBar(vertices, new Vector2(-halfWidth + inset, armLength), DigitStroke, halfHeight);
        if (HasSegment(value, 'b')) AddBar(vertices, new Vector2(halfWidth - inset, armLength), DigitStroke, halfHeight);
        if (HasSegment(value, 'e')) AddBar(vertices, new Vector2(-halfWidth + inset, -armLength), DigitStroke, halfHeight);
        if (HasSegment(value, 'c')) AddBar(vertices, new Vector2(halfWidth - inset, -armLength), DigitStroke, halfHeight);

        var mesh = new Mesh { name = $"Dice_Digit_{value}" };
        mesh.SetVertices(vertices);
        int[] triangles = new int[vertices.Count];
        for (int i = 0; i < triangles.Length; i++) triangles[i] = i;
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static bool HasSegment(int value, char segment)
    {
        string segments = value switch
        {
            1 => "bc",
            2 => "abged",
            3 => "abgcd",
            4 => "fbgc",
            5 => "afgcd",
            _ => "afgecd"
        };
        return segments.IndexOf(segment) >= 0;
    }

    /// <summary>XZ 평면에 놓인 직사각형 획 하나. 위(+Y)에서 봤을 때 앞면이 보이도록 감는다.</summary>
    private static void AddBar(List<Vector3> vertices, Vector2 center, float width, float height)
    {
        float hw = width * 0.5f;
        float hh = height * 0.5f;
        Vector3 a = new(center.x - hw, 0f, center.y - hh);
        Vector3 b = new(center.x - hw, 0f, center.y + hh);
        Vector3 c = new(center.x + hw, 0f, center.y + hh);
        Vector3 d = new(center.x + hw, 0f, center.y - hh);

        vertices.Add(a); vertices.Add(b); vertices.Add(c);
        vertices.Add(a); vertices.Add(c); vertices.Add(d);
    }

    private static void BakeOctahedronPrefab(Mesh body, Mesh[] digits)
    {
        var root = new GameObject("Die_Octahedron");
        CreateRenderer(root.transform, "Body", body);

        int[] faceValues = DiceFaceValues.Get(DieType.Octahedron);
        for (int face = 0; face < OctFaceDirections.Length; face++)
        {
            Vector3 dir = OctFaceDirections[face];
            Vector3 n = dir.normalized;
            Vector3 faceCenter = n * (OctRadius / Mathf.Sqrt(3f) + BevelRadius);
            var group = new GameObject($"Pip_{face + 1}");
            group.transform.SetParent(root.transform, false);

            // 숫자가 삼각형의 한 꼭짓점을 향하도록 세운다(원본 geometryUtils.js:125의 upDir 규칙).
            Vector3 localUp = new Vector3(-dir.x * OctRadius / 3f, -dir.y * OctRadius / 3f, 2f * dir.z * OctRadius / 3f).normalized;
            if (localUp.sqrMagnitude < 0.001f) localUp = Vector3.Cross(n, Vector3.right).normalized;

            GameObject digit = CreateRenderer(group.transform, $"Pip_{face + 1}_Digit", digits[faceValues[face] - 1]);
            digit.transform.localPosition = faceCenter + n * PipSurfaceLift;
            digit.transform.localRotation = Quaternion.LookRotation(localUp, n);
            digit.transform.localScale = Vector3.one;
        }

        // 음각 홈이 없어도 D6와 그림자 규약을 맞춰 둔다. 런타임이 프록시를 새로 만들지 않는다.
        GameObject proxy = CreateRenderer(root.transform, "ShadowProxy", body);
        MeshRenderer proxyRenderer = proxy.GetComponent<MeshRenderer>();
        proxyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        proxyRenderer.receiveShadows = false;

        SavePrefab(root, "Die_Octahedron");
    }

    /// <summary>
    /// 세븐스 주사위는 D6 몸체를 그대로 쓰되 눈을 한 칸씩 밀어 새긴다.
    /// 면 값 표가 2·3·4·5·6·7이므로 i번 면에는 i+1눈이 와야 한다(원본 diceMaterials.js:69-70).
    /// </summary>
    private static void BakeSevensPrefab(GameObject diceModel)
    {
        GameObject root = Object.Instantiate(diceModel);
        root.name = "Die_Sevens";

        PipTemplate pip = ResolvePipTemplate(diceModel);
        List<Transform> allPips = CollectPips(root.transform);
        if (allPips.Count == 0)
        {
            Debug.LogError("[DiceShapeBaker] D6 모델에서 눈을 찾지 못했습니다.");
            Object.DestroyImmediate(root);
            return;
        }

        Transform pipsParent = allPips[0].parent;
        float diagonal = MeasureDiagonalOffset(allPips);
        int[] faceValues = DiceFaceValues.Get(DieType.Sevens);

        // 면마다 중심·방향·눈 크기를 먼저 재 둔다. 지우고 나면 잴 수 없다.
        var faceCenters = new Vector3[6];
        var faceRotations = new Quaternion[6];
        var faceScales = new Vector3[6];
        var faceCounts = new int[6];

        foreach (Transform dot in allPips)
        {
            int face = ParseFaceIndex(dot.name);
            if (face < 1 || face > 6) continue;

            faceCenters[face - 1] += dot.localPosition;
            faceRotations[face - 1] = dot.localRotation;
            faceScales[face - 1] = dot.localScale;
            faceCounts[face - 1]++;
        }

        foreach (Transform dot in allPips) Object.DestroyImmediate(dot.gameObject);

        for (int face = 1; face <= 6; face++)
        {
            if (faceCounts[face - 1] == 0) continue;

            Vector3 center = faceCenters[face - 1] / faceCounts[face - 1];
            Vector3 normal = center.normalized;
            Vector3 right = Vector3.Cross(normal, Vector3.up);
            if (right.sqrMagnitude < 0.001f) right = Vector3.Cross(normal, Vector3.forward);
            right = right.normalized;
            Vector3 up = Vector3.Cross(right, normal).normalized;

            foreach (Vector3 offset in PipOffsets(faceValues[face - 1], right, up, diagonal))
            {
                GameObject dot = CreateRenderer(pipsParent, $"Pip_{face}", pip.Mesh);
                dot.transform.localPosition = center + offset;
                dot.transform.localRotation = faceRotations[face - 1];
                dot.transform.localScale = faceScales[face - 1];
            }
        }

        SavePrefab(root, "Die_Sevens");
    }

    /// <summary>정육면체 면의 표준 눈 배치. 원본 diceMaterials.js:93-97과 같은 규칙이다.</summary>
    private static IEnumerable<Vector3> PipOffsets(int value, Vector3 right, Vector3 up, float d)
    {
        if (value == 1 || value == 3 || value == 5 || value == 7) yield return Vector3.zero;

        if (value >= 2)
        {
            yield return (-right - up) * d;
            yield return (right + up) * d;
        }
        if (value >= 4)
        {
            yield return (right - up) * d;
            yield return (-right + up) * d;
        }
        if (value >= 6)
        {
            yield return -right * d;
            yield return right * d;
        }
    }

    private static int ParseFaceIndex(string pipName)
    {
        string digits = pipName.Substring(4).Split('.')[0];
        return int.TryParse(digits, out int face) ? face : -1;
    }

    /// <summary>6번 면 눈들의 간격에서 대각 오프셋을 실측한다. D6 모델이 바뀌어도 따라간다.</summary>
    private static float MeasureDiagonalOffset(List<Transform> allPips)
    {
        var sixFace = new List<Vector3>();
        foreach (Transform child in allPips)
        {
            if (child.name.StartsWith("Pip_6")) sixFace.Add(child.localPosition);
        }

        if (sixFace.Count < 2) return 0.38f;

        Vector3 center = Vector3.zero;
        foreach (Vector3 p in sixFace) center += p;
        center /= sixFace.Count;

        float best = 0f;
        foreach (Vector3 p in sixFace)
        {
            Vector3 planar = Vector3.ProjectOnPlane(p - center, center.normalized);
            best = Mathf.Max(best, planar.magnitude);
        }
        return best > 0.001f ? best / Mathf.Sqrt(2f) : 0.38f;
    }

    // ---------------------------------------------------------------- 보조

    private readonly struct PipTemplate
    {
        public readonly Mesh Mesh;
        /// <summary>메시 자체의 반지름. 스케일 1로 놓았을 때의 크기다.</summary>
        public readonly float LocalRadius;
        /// <summary>D6에서 실제로 보이는 눈 반지름. 목표 크기를 정하는 기준이다.</summary>
        public readonly float WorldRadius;
        public readonly Vector3 Normal;

        public PipTemplate(Mesh mesh, float localRadius, float worldRadius, Vector3 normal)
        {
            Mesh = mesh;
            LocalRadius = localRadius;
            WorldRadius = worldRadius;
            Normal = normal;
        }
    }

    /// <summary>D6 모델의 눈 하나를 본으로 삼는다. 크기와 향하는 방향을 함께 잰다.</summary>
    private static PipTemplate ResolvePipTemplate(GameObject diceModel)
    {
        GameObject probe = Object.Instantiate(diceModel);
        try
        {
            foreach (Transform child in CollectPips(probe.transform))
            {
                if (!child.name.StartsWith("Pip_1")) continue;

                MeshFilter filter = child.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;

                // 메시 자체 크기와 모델 계층 스케일을 곱한 실제 크기를 함께 잰다.
                // 스케일을 빼먹으면 눈이 몸체를 덮을 만큼 커진다.
                Bounds bounds = filter.sharedMesh.bounds;
                Vector3 scale = child.lossyScale;
                float localRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
                float worldRadius = Mathf.Max(bounds.extents.x * Mathf.Abs(scale.x), bounds.extents.z * Mathf.Abs(scale.z));
                return new PipTemplate(filter.sharedMesh, localRadius, worldRadius, child.localPosition.normalized);
            }
            return default;
        }
        finally
        {
            Object.DestroyImmediate(probe);
        }
    }

    private static GameObject CreateRenderer(Transform parent, string name, Mesh mesh)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        return go;
    }

    /// <summary>이름이 Pip_로 시작하는 자손을 모두 모은다. 묶음 노드 이름에 기대지 않는다.</summary>
    private static List<Transform> CollectPips(Transform root)
    {
        var found = new List<Transform>();
        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate != root && candidate.name.StartsWith("Pip_")) found.Add(candidate);
        }
        return found;
    }

    private static string DescribeNames(List<Transform> transforms)
    {
        var names = new List<string>();
        for (int i = 0; i < transforms.Count && i < 8; i++) names.Add(transforms[i].name);
        return string.Join(", ", names);
    }

    private static Mesh SaveMesh(Mesh mesh, string assetName)
    {
        string path = $"{MeshFolder}/{assetName}.mesh";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void SavePrefab(GameObject root, string assetName)
    {
        string path = $"{PrefabFolder}/{assetName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
