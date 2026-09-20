using System;
using System.Collections.Generic;
using System.IO;
using Tessera.Core;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 동전 메시를 굽는다(M17-T23-2, M17-T23-4).
///
/// coin.glb의 Coin 프리미티브는 양면(앞·뒷면)이 한 메시에 섞여 있어, 삼각형 기하 법선의 Y 부호로
/// 앞/뒤를 갈라 독립 메시 둘로 쪼갠다. CoinRim은 통째로 테두리 메시가 된다.
///
/// 자동 호출하지 않는다. 소스 자산을 바꾼 뒤 메뉴로 다시 굽고 결과 자산을 커밋한다.
/// </summary>
public sealed class CoinMeshData
{
    public Vector3[] Positions;
    public Vector3[] Normals;
    public Vector2[] Uvs;
    public int[] Indices;
}

public sealed class CoinMeshSplit
{
    public CoinMeshData Head;
    public CoinMeshData Tail;
    public CoinMeshData Edge;
}

public static class CoinMeshBaker
{
    private const string GlbPath = "Assets/Art/Source/Coin/coin.glb";
    private const string HeadSourcePath = "Assets/Art/Source/Coin/coin_head_source.png";
    private const string TailSourcePath = "Assets/Art/Source/Coin/coin_tail_source.png";
    private const string GeneratedFolder = "Assets/Art/Generated/Coin";
    private const string MeshFolder = GeneratedFolder + "/Meshes";
    private const string MaterialFolder = GeneratedFolder + "/Materials";
    private const string TextureFolder = GeneratedFolder + "/Textures";
    private const string PrefabPath = "Assets/Prefabs/Coin/Coin.prefab";

    // 사용자 화면 확인으로 정한 값. 지름 약 1.59(주사위 한 변 1.014의 약 1.57배).
    private const float ModelScale = 0.8f;

    // 캡 필드(테두리 안쪽 평판) 깊이 실측. 평판은 |y|=SourcePlateauHeight(반지름 r≤0.817, 법선 y≈0.999)에서
    // 시작해, 바깥으로 둥근 필렛이 r=0.897에서 |y|=RimLipHeight까지 올라가 테두리 안턱과 만난다.
    // 원본 평판 깊이(0.0696)는 480×270 저해상도에서 반 칸도 안 돼 평평하게 보이므로 FieldPlateauHeight로 더 판다.
    private const float SourcePlateauHeight = 0.0696f;
    private const float RimLipHeight = 0.0996f;
    private const float FieldPlateauHeight = 0.03f;

    // 키라이트 Quaternion.Euler(60f, -35f, 0f), 카메라 yaw 0(YachtSceneAssembler.cs:139,159)이라
    // 빛은 +x·-z에서 들어온다. 앞면 UV는 u=+x·v=+z라 텍셀 방향 (+1,-1). 뒷면은 v=-z로 투영했으므로
    // X축으로 뒤집은 상태에서 v=월드 +z가 되어 마찬가지로 (+1,-1)이 된다.
    private static readonly Vector2Int FaceLightFrom = new(1, -1);

    /// <summary>
    /// Coin 프리미티브를 삼각형 기하 법선의 Y 부호로 Head/Tail로 가르고, CoinRim을 Edge로 삼는다.
    /// </summary>
    public static CoinMeshSplit Split(CoinGlbPrimitive[] primitives)
    {
        CoinGlbPrimitive coin = FindPrimitive(primitives, "Coin");
        CoinGlbPrimitive rim = FindPrimitive(primitives, "CoinRim");

        var headTriangles = new List<int>();
        var tailTriangles = new List<int>();
        for (int t = 0; t < coin.Indices.Length; t += 3)
        {
            int i0 = coin.Indices[t];
            int i1 = coin.Indices[t + 1];
            int i2 = coin.Indices[t + 2];
            Vector3 p0 = coin.Positions[i0];
            Vector3 p1 = coin.Positions[i1];
            Vector3 p2 = coin.Positions[i2];
            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0).normalized;

            if (Mathf.Abs(n.y) < 0.3f)
                throw new InvalidDataException($"coin.glb: Coin 삼각형 {t / 3}이 수직에 가까워(n.y={n.y}) 앞/뒤를 가를 수 없습니다.");

            (n.y > 0f ? headTriangles : tailTriangles).AddRange(new[] { i0, i1, i2 });
        }

        // 캡 UV 재투영에 쓸 반지름: Head/Tail 전 정점 중 최대 반경을 공유한다.
        float capRadius = 0f;
        foreach (Vector3 p in coin.Positions)
            capRadius = Mathf.Max(capRadius, Mathf.Sqrt(p.x * p.x + p.z * p.z));

        CoinMeshData head = BuildCapMesh(coin, headTriangles, capRadius, flipV: false);
        CoinMeshData tail = BuildCapMesh(coin, tailTriangles, capRadius, flipV: true);
        CoinMeshData edge = BuildEdgeMesh(rim);

        return new CoinMeshSplit { Head = head, Tail = tail, Edge = edge };
    }

    private static CoinGlbPrimitive FindPrimitive(CoinGlbPrimitive[] primitives, string materialName)
    {
        foreach (CoinGlbPrimitive primitive in primitives)
            if (primitive.MaterialName == materialName)
                return primitive;
        throw new InvalidDataException($"coin.glb: 머티리얼 이름이 '{materialName}'인 프리미티브를 찾지 못했습니다.");
    }

    /// <summary>
    /// 삼각형 인덱스 목록으로 가리키는 정점만 골라 독립 배열로 재색인하고, 캡 UV를 원판 투영으로 새로 만든다.
    ///
    /// Head는 uv=(x/(2R)+0.5, z/(2R)+0.5), Tail은 z축 부호를 반전해 uv=(x/(2R)+0.5, -z/(2R)+0.5)를 쓴다.
    /// 텍스처 전체를 캡 하나에 다 쓰면서, 동전을 X축으로 뒤집어 뒷면을 봤을 때 리라 문양이 정립으로 읽히게 하기 위해서다.
    /// </summary>
    private static CoinMeshData BuildCapMesh(CoinGlbPrimitive source, List<int> triangleIndices, float capRadius, bool flipV)
    {
        var remap = new Dictionary<int, int>();
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var indices = new int[triangleIndices.Count];

        for (int i = 0; i < triangleIndices.Count; i++)
        {
            int oldIndex = triangleIndices[i];
            if (!remap.TryGetValue(oldIndex, out int newIndex))
            {
                newIndex = positions.Count;
                remap[oldIndex] = newIndex;
                (Vector3 p, Vector3 n) = DeepenFieldPlateau(source.Positions[oldIndex], source.Normals[oldIndex]);
                positions.Add(p);
                normals.Add(n);
                float u = p.x / (2f * capRadius) + 0.5f;
                float v = (flipV ? -p.z : p.z) / (2f * capRadius) + 0.5f;
                uvs.Add(new Vector2(u, v));
            }
            indices[i] = newIndex;
        }

        return new CoinMeshData
        {
            Positions = positions.ToArray(),
            Normals = normals.ToArray(),
            Uvs = uvs.ToArray(),
            Indices = indices
        };
    }

    /// <summary>
    /// 캡 평판을 테두리 안턱보다 더 깊게 파, 480×270 저해상도에서도 높이 단차가 보이게 한다.
    /// x·z는 그대로 둬 UV 재투영과 테두리 이음매(r≈0.897, h=RimLipHeight → t=1로 불변)가 유지된다.
    /// 법선은 높이축 확대에 맞춰 해석적으로 보정한다. 평판 법선(≈수직)은 사실상 불변, 필렛은 가팔라져
    /// 조명에서 그림자 링이 생긴다.
    /// </summary>
    private static (Vector3 position, Vector3 normal) DeepenFieldPlateau(Vector3 position, Vector3 normal)
    {
        float h = Mathf.Abs(position.y);
        float t = Mathf.Clamp01((h - SourcePlateauHeight) / (RimLipHeight - SourcePlateauHeight));
        float newH = Mathf.Lerp(FieldPlateauHeight, RimLipHeight, t);
        Vector3 newPosition = new(position.x, Mathf.Sign(position.y) * newH, position.z);

        float k = (RimLipHeight - FieldPlateauHeight) / (RimLipHeight - SourcePlateauHeight);
        Vector3 newNormal = new Vector3(normal.x * k, normal.y, normal.z * k).normalized;

        return (newPosition, newNormal);
    }

    private static CoinMeshData BuildEdgeMesh(CoinGlbPrimitive rim)
    {
        return new CoinMeshData
        {
            Positions = rim.Positions,
            Normals = rim.Normals,
            Uvs = rim.Uvs,
            Indices = rim.Indices
        };
    }

    [MenuItem("Tessera/Bake/Coin Model")]
    public static void Bake()
    {
        EnsureFolder(GeneratedFolder);
        EnsureFolder(MeshFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(TextureFolder);

        CoinGlbPrimitive[] primitives = CoinGlbReader.Read(File.ReadAllBytes(GlbPath));
        CoinMeshSplit split = Split(primitives);

        Mesh headMesh = SaveMesh(ToMesh(split.Head), "Coin_Head");
        Mesh tailMesh = SaveMesh(ToMesh(split.Tail), "Coin_Tail");
        Mesh edgeMesh = SaveMesh(ToMesh(split.Edge), "Coin_Edge");

        Texture2D headTexture = BakeFaceTexture(HeadSourcePath, $"{TextureFolder}/coin_head_albedo.png");
        Texture2D tailTexture = BakeFaceTexture(TailSourcePath, $"{TextureFolder}/coin_tail_albedo.png");

        Material edgeMaterial = CreateOrReplaceLitMaterial($"{MaterialFolder}/CoinEdge.mat",
            new Color32(0xE5, 0xA9, 0x3C, 0xFF), metallic: 0.88f, smoothness: 0.75f, texture: null);
        Material headMaterial = CreateOrReplaceLitMaterial($"{MaterialFolder}/CoinHead.mat",
            Color.white, metallic: 0f, smoothness: 0.12f, texture: headTexture);
        Material tailMaterial = CreateOrReplaceLitMaterial($"{MaterialFolder}/CoinTail.mat",
            Color.white, metallic: 0f, smoothness: 0.12f, texture: tailTexture);

        BuildPrefab(edgeMesh, edgeMaterial, headMesh, headMaterial, tailMesh, tailMaterial);

        AssetDatabase.SaveAssets();
        Debug.Log("동전 메시·텍스처·머티리얼·프리팹을 구웠습니다.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        string name = path.Substring(path.LastIndexOf('/') + 1);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static Mesh ToMesh(CoinMeshData data)
    {
        var mesh = new Mesh();
        mesh.SetVertices(data.Positions);
        mesh.SetNormals(data.Normals);
        mesh.SetUVs(0, data.Uvs);
        mesh.SetTriangles(data.Indices, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh SaveMesh(Mesh mesh, string assetName)
    {
        // CopySerialized는 이름까지 복사한다. 이름 없는 새 메시로 덮으면 주 오브젝트 이름이 비어 경고가 난다.
        mesh.name = assetName;
        string path = $"{MeshFolder}/{assetName}.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    /// <summary>
    /// 소스 PNG를 임포터 설정과 무관하게 원본 픽셀로 읽어 <see cref="CoinFaceTextureConverter"/>로 픽셀아트화하고,
    /// 픽셀아트 텍스처가 요구하는 Point/밉맵 off/비압축 임포트 설정으로 저장한다.
    /// </summary>
    private static Texture2D BakeFaceTexture(string sourcePath, string outputPath)
    {
        var source = new Texture2D(2, 2);
        source.LoadImage(File.ReadAllBytes(sourcePath));
        Color32[] converted = CoinFaceTextureConverter.Convert(
            source.GetPixels32(), source.width, source.height, CoinFaceTextureConverter.PixelPathSize, FaceLightFrom);
        UnityEngine.Object.DestroyImmediate(source);

        var output = new Texture2D(CoinFaceTextureConverter.PixelPathSize, CoinFaceTextureConverter.PixelPathSize,
            TextureFormat.RGBA32, false);
        output.SetPixels32(converted);
        File.WriteAllBytes(outputPath, output.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(output);

        AssetDatabase.ImportAsset(outputPath);
        if (AssetImporter.GetAtPath(outputPath) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
    }

    /// <summary>
    /// 평평한 금속 캡은 키라이트에서 스페큘러 정점에 걸려 흰 덩어리로 번지므로, 금속감은 곡면 테두리(Edge)에만 두고
    /// 캡은 GI 반사를 끈다.
    /// </summary>
    private static Material CreateOrReplaceLitMaterial(string path, Color color, float metallic, float smoothness, Texture2D texture)
    {
        AssetDatabase.DeleteAsset(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path), color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (texture != null)
        {
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        }
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>
    /// 서브메시가 아니라 자식 GameObject 셋으로 나눈다. 레이어는 GameObject 단위라, CrispUI로 승격할 때
    /// 면(Face_Head/Face_Tail)만 올리고 테두리(Body)는 픽셀 필터에 남겨야 하기 때문이다.
    /// </summary>
    private static void BuildPrefab(Mesh edgeMesh, Material edgeMaterial, Mesh headMesh, Material headMaterial, Mesh tailMesh, Material tailMaterial)
    {
        var root = new GameObject("Coin");
        root.layer = TesseraLayers.Decoration;
        root.transform.localScale = Vector3.one * ModelScale;

        CreateChild(root.transform, "Body", edgeMesh, edgeMaterial);
        CreateChild(root.transform, "Face_Head", headMesh, headMaterial);
        CreateChild(root.transform, "Face_Tail", tailMesh, tailMaterial);

        EnsureFolder("Assets/Prefabs/Coin");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateChild(Transform parent, string name, Mesh mesh, Material material)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.layer = TesseraLayers.Decoration;
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
}
