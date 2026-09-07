using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Tessera.Games.AugmentedYacht;

public static class BuildDiceGraphicsPoC
{
    private const string ScenePath = "Assets/Scenes/Augmented Dice.unity";
    private const string TrayStlPath = "Assets/Art/Reference/yacht-tray.stl";
    private const string TrayMeshPath = "Assets/Art/Reference/yacht-tray.asset";
    [MenuItem("Tools/Tessera/Rebuild Augmented Dice Scene")]
    public static void RebuildScene()
    {
        BuildScene(true);
    }

    [MenuItem("Tools/Tessera/Build Windows Validation")]
    public static void BuildWindowsValidation()
    {
        EnsureSceneExists();
        string buildDirectory = Path.GetFullPath("Build");
        Directory.CreateDirectory(buildDirectory);
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.Combine(buildDirectory, "DiceGraphicsPoC.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.InvalidOperationException($"Dice Graphics PoC build failed: {report.summary.result}");
        Debug.Log($"Dice Graphics PoC standalone built: {report.summary.outputPath}");
    }

    private static void EnsureSceneExists()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            BuildScene(false);
    }

    private static void BuildScene(bool openAfterBuild)
    {
        GameObject dice = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Reference/normal_dice.fbx");
        Mesh yachtTray = EnsureYachtTrayMesh();
        Texture2D playmat = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Reference/playmat.png");
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Rendering/Shaders/DicePixelUpscale.shader");
        if (dice == null || yachtTray == null || playmat == null || shader == null)
        {
            Debug.LogError("Tessera build stopped: FBX dice model, playmat, or upscale shader missing.");
            return;
        }

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Reference/playmat.png");
        if (importer != null && (importer.filterMode != FilterMode.Point || importer.mipmapEnabled))
        {
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            playmat = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Reference/playmat.png");
        }

        Scene previous = SceneManager.GetActiveScene();
        bool disposableUntitledScene = previous.IsValid() && string.IsNullOrEmpty(previous.path) && !previous.isDirty;
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            disposableUntitledScene ? NewSceneMode.Single : NewSceneMode.Additive);
        scene.name = "Augmented Dice";
        SceneManager.SetActiveScene(scene);

        GameObject root = new("Tessera Augmented Dice Game");
        AugmentedYachtController controller = root.AddComponent<AugmentedYachtController>();
        SerializedObject serialized = new(controller);
        serialized.FindProperty("diceModel").objectReferenceValue = dice;
        serialized.FindProperty("yachtTrayMesh").objectReferenceValue = yachtTray;
        serialized.FindProperty("playmatTexture").objectReferenceValue = playmat;
        serialized.FindProperty("upscaleShader").objectReferenceValue = shader;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // 테이블 프롭은 코드로 만들지 않는다(M9). Assets/Prefabs/Tabletop 의 프리팹을 씬에 배치한다.
        Debug.LogWarning("빈 씬을 만들었습니다. Assets/Prefabs/Tabletop 의 프리팹을 씬에 배치하십시오.");

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        if (previous.IsValid() && previous.isLoaded && previous != scene)
            EditorSceneManager.CloseScene(previous, true);

        if (openAfterBuild) EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("Dice Graphics PoC scene built: Assets/Scenes/GraphicsPoC.unity");
    }

    private static Mesh EnsureYachtTrayMesh()
    {
        if (!File.Exists(TrayStlPath)) return null;
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(TrayMeshPath);
        if (existing != null && existing.uv != null && existing.uv.Length > 0 && File.GetLastWriteTimeUtc(TrayMeshPath) >= File.GetLastWriteTimeUtc(TrayStlPath))
            return existing;

        Mesh imported = ImportBinaryYachtTray(TrayStlPath);
        if (imported == null) return null;
        imported.name = "Yacht Tray";
        if (existing == null)
            AssetDatabase.CreateAsset(imported, TrayMeshPath);
        else
        {
            EditorUtility.CopySerialized(imported, existing);
            Object.DestroyImmediate(imported);
            EditorUtility.SetDirty(existing);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(TrayMeshPath, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Mesh>(TrayMeshPath);
    }

    private static Mesh ImportBinaryYachtTray(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using BinaryReader reader = new(stream);
        if (stream.Length < 84) return null;
        reader.ReadBytes(80);
        uint triangleCount = reader.ReadUInt32();
        if (stream.Length < 84L + triangleCount * 50L) return null;

        List<Vector3> vertices = new((int)triangleCount * 3);
        List<Vector3> normals = new((int)triangleCount * 3);
        List<Vector2> uvs = new((int)triangleCount * 3);
        List<int> rimIndices = new((int)triangleCount * 3);
        List<int> floorIndices = new((int)triangleCount * 3);
        for (uint triangle = 0; triangle < triangleCount; triangle++)
        {
            Vector3 sourceNormal = ReadVector(reader);
            Vector3 normal = new Vector3(sourceNormal.x, sourceNormal.z, sourceNormal.y).normalized;
            int firstIndex = vertices.Count;
            Vector3 average = Vector3.zero;
            for (int vertex = 0; vertex < 3; vertex++)
            {
                Vector3 source = ReadVector(reader);
                Vector3 transformed = new Vector3(source.x - 125f, source.z - 14f, source.y - 110f);
                vertices.Add(transformed);
                normals.Add(normal);
                average += transformed;

                if (Mathf.Abs(normal.y) >= 0.7f)
                {
                    uvs.Add(new Vector2(transformed.x * (1f / 50f), transformed.z * (1f / 50f)));
                }
                else
                {
                    float uCoord = (Mathf.Abs(normal.x) > Mathf.Abs(normal.z) ? transformed.z : transformed.x) * (1f / 50f);
                    float vCoord = transformed.y * (1f / 50f);
                    uvs.Add(new Vector2(uCoord, vCoord));
                }
            }
            reader.ReadUInt16();
            average /= 3f;
            bool floorRegion = Mathf.Abs(average.x) <= 54.01f
                && average.z >= -68.01f && average.z <= 54.01f && average.y <= 13.01f;
            List<int> indices = floorRegion && Mathf.Abs(normal.y) >= 0.7f ? floorIndices : rimIndices;
            indices.Add(firstIndex);
            indices.Add(firstIndex + 2);
            indices.Add(firstIndex + 1);
        }

        Mesh mesh = new() { indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(rimIndices, 0, false);
        mesh.SetTriangles(floorIndices, 1, false);
        mesh.RecalculateBounds();
        mesh.UploadMeshData(false);
        return mesh;
    }

    private static Vector3 ReadVector(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}
