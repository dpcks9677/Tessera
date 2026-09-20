using System.IO;
using NUnit.Framework;
using System.Reflection;
using Tessera.Tabletop;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 코스믹 큐브의 굴림 예산 3상태 계약을 고정한다.
///
/// 출하 경로는 구운 프리팹이라 BuildGeometry가 돌지 않고, 화면에 실제로 나오는 것은 구운
/// 머티리얼과 셰이더다. 그래서 에셋 계약 검사가 이 기능을 지키는 핵심이다.
/// </summary>
public sealed class RollCosmicCubeAugmentStateTests
{
    private const string ShaderFolder = "Assets/Rendering/Shaders";
    private const string MaterialFolder = "Assets/Art/Generated/Tabletop/Materials";
    private const string TintProperty = "_AugmentTint";
    private const string DrainProperty = "_AugmentDrain";

    private static readonly string[] ShaderNames =
    {
        "DicePoC/CosmicVolume",
        "DicePoC/CosmicCrystalShell",
        "DicePoC/CosmicCore",
        "DicePoC/CosmicCubeHoverOutline",
        "DicePoC/CosmicTesseract"
    };

    private static readonly string[] ShaderFiles =
    {
        "DiceCosmicVolume", "DiceCosmicCrystalShell", "DiceCosmicCore",
        "DiceCosmicCubeHoverOutline", "DiceCosmicTesseract"
    };

    private static readonly string[] CosmicMaterials =
    {
        "3D_Roll_Cosmic_Cube_Cosmic_Inner_Volume_Mat",
        "3D_Roll_Cosmic_Cube_Cosmic_Crystal_Front_Mat",
        "3D_Roll_Cosmic_Cube_Cosmic_Crystal_Back_Mat",
        "3D_Roll_Cosmic_Cube_Cosmic_Crystal_Inner_B_Mat",
        "3D_Roll_Cosmic_Cube_Cosmic_Energy_Core_Mat",
        "3D_Roll_Cosmic_Cube_Cosmic_Cube_Near_Halo_Mat",
        "3D_Roll_Cosmic_Cube_Cosmic_Cube_Outer_Halo_Mat",
        "3D_Roll_Cosmic_Cube_Cosmic_Tesseract_Mat"
    };

    private GameObject testRoot;

    [TearDown]
    public void TearDown()
    {
        if (testRoot != null) Object.DestroyImmediate(testRoot);
    }

    [Test]
    public void FiveCosmicShadersHaveAugmentStateProperties()
    {
        foreach (string shaderName in ShaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            Assert.That(shader, Is.Not.Null, $"{shaderName} 셰이더를 찾지 못했습니다.");
            Assert.That(shader.isSupported, Is.True, $"{shaderName}이 이 플랫폼에서 지원되지 않습니다.");

            Material material = new(shader);
            try
            {
                Assert.That(material.HasProperty(TintProperty), Is.True, shaderName);
                Assert.That(material.HasProperty(DrainProperty), Is.True, shaderName);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }
    }

    [Test]
    public void AugmentStateUniformsLiveInUnityPerMaterialCbuffer()
    {
        // HasProperty는 CBUFFER 선언이 빠져도 통과한다. 그 누락은 다섯 셰이더를 조용히
        // SRP Batcher 비호환으로 만들고 아무 오류도 내지 않으므로 텍스트로 직접 확인한다.
        foreach (string file in ShaderFiles)
        {
            string path = $"{ShaderFolder}/{file}.shader";
            Assert.That(File.Exists(path), Is.True, path);
            string text = File.ReadAllText(path);

            Assert.That(text, Does.Contain("#include \"CosmicAugmentState.hlsl\""), path);

            int start = text.IndexOf("CBUFFER_START(UnityPerMaterial)", System.StringComparison.Ordinal);
            int end = text.IndexOf("CBUFFER_END", System.StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), $"{path}에 UnityPerMaterial CBUFFER가 없습니다.");
            Assert.That(end, Is.GreaterThan(start), path);

            string cbuffer = text.Substring(start, end - start);
            Assert.That(cbuffer, Does.Contain(TintProperty), $"{path}의 CBUFFER에 {TintProperty}가 없습니다.");
            Assert.That(cbuffer, Does.Contain(DrainProperty), $"{path}의 CBUFFER에 {DrainProperty}가 없습니다.");
        }
    }

    [Test]
    public void BakedMaterialsUseAugmentStateDefaults()
    {
        // 셰이더에 프로퍼티를 더해도 .mat을 고칠 필요가 없다는 근거다.
        // 소진 값이 실수로 구워지는 사고도 여기서 걸린다.
        foreach (string name in CosmicMaterials)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
            Assert.That(material, Is.Not.Null, name);
            Assert.That(material.HasProperty(DrainProperty), Is.True, name);
            Assert.That(material.GetFloat(DrainProperty), Is.EqualTo(0f).Within(0.0001f), name);
            Assert.That(material.GetColor(TintProperty).a, Is.EqualTo(0f).Within(0.0001f), name);
        }
    }

    [Test]
    public void RollBudgetStateChangesOnlyLerpTarget()
    {
        RollCosmicCube cube = BuildCube();

        cube.SetRollBudgetState(RollBudgetState.Drained, animate: false);
        Assert.That(ReadLerp(cube, "drainLerp"), Is.EqualTo(1f));
        Assert.That(ReadLerp(cube, "augmentTintLerp"), Is.EqualTo(0f));

        cube.SetRollBudgetState(RollBudgetState.AugmentReady, animate: false);
        Assert.That(ReadLerp(cube, "drainLerp"), Is.EqualTo(0f));
        Assert.That(ReadLerp(cube, "augmentTintLerp"), Is.EqualTo(1f));

        cube.SetRollBudgetState(RollBudgetState.Normal, animate: false);
        Assert.That(ReadLerp(cube, "drainLerp"), Is.EqualTo(0f));
        Assert.That(ReadLerp(cube, "augmentTintLerp"), Is.EqualTo(0f));
    }

    [Test]
    public void InteractionToggleDoesNotTouchRollBudgetState()
    {
        // isInteractable은 굴림 애니메이션과 턴 전환마다 껐다 켜진다. 두 신호가 섞이면
        // 매 굴림마다 큐브가 회색으로 깜빡인다.
        RollCosmicCube cube = BuildCube();
        cube.SetRollBudgetState(RollBudgetState.Drained, animate: false);

        cube.SetInteractable(false);
        cube.SetInteractable(true);

        Assert.That(ReadLerp(cube, "drainLerp"), Is.EqualTo(1f));
    }

    [Test]
    public void BaseAndPillarDoNotUseAugmentStateShader()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tabletop/3D Roll Cosmic Cube.prefab");
        Assert.That(prefab, Is.Not.Null);

        Transform body = prefab.transform.Find("Cosmic_Cube_Floating_Root/Cosmic_Cube_Body");
        Assert.That(body, Is.Not.Null, "Cosmic_Cube_Body");
        AssertDrainSupport(body, expected: true);

        Transform basePlatform = prefab.transform.Find("Base_Platform");
        Transform pillar = prefab.transform.Find("Pillar_Pedestal");
        Assert.That(basePlatform, Is.Not.Null, "Base_Platform");
        Assert.That(pillar, Is.Not.Null, "Pillar_Pedestal");
        AssertDrainSupport(basePlatform, expected: false);
        AssertDrainSupport(pillar, expected: false);
    }

    private static void AssertDrainSupport(Transform root, bool expected)
    {
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material material = renderer.sharedMaterial;
            if (material == null) continue;
            Assert.That(material.HasProperty(DrainProperty), Is.EqualTo(expected),
                $"{renderer.name}의 {material.name}");
        }
    }

    private RollCosmicCube BuildCube()
    {
        testRoot = new GameObject("Cosmic Cube Test Root");
        RollCosmicCube cube = RollCosmicCube.Create(testRoot.transform, Vector3.zero);
        Assert.That(cube, Is.Not.Null);
        return cube;
    }

    private static float ReadLerp(RollCosmicCube cube, string fieldName)
    {
        FieldInfo field = typeof(RollCosmicCube).GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"{fieldName} 필드를 찾지 못했습니다.");
        return (float)field.GetValue(cube);
    }
}
