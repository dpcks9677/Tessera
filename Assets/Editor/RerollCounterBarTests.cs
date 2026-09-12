using NUnit.Framework;
using System.Reflection;
using Tessera.Tabletop;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 리롤 카운터 바 보석의 점등/소등 표현을 지킨다.
///
/// 출하 경로는 구운 프리팹이라 <c>BuildGeometry</c>가 돌지 않는다. 화면에 실제로 나오는 것은
/// 구운 머티리얼 두 개이므로, 에셋 계약 검사가 이 수정을 지키는 핵심이다.
/// </summary>
public sealed class RerollCounterBarTests
{
    private const string PrefabPath = "Assets/Prefabs/Tabletop/3D Reroll Counter Bar.prefab";
    private const string GemMatPath =
        "Assets/Art/Generated/Tabletop/Materials/3D_Reroll_Counter_Bar_Counter_HexGemBaseMat.mat";
    private const string RidgeMatPath =
        "Assets/Art/Generated/Tabletop/Materials/3D_Reroll_Counter_Bar_Counter_GemRidgeBaseMat.mat";
    private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

    private GameObject testRoot;

    [TearDown]
    public void TearDown()
    {
        if (testRoot != null) Object.DestroyImmediate(testRoot);
    }

    [Test]
    public void BakedGemMaterialIsUrpUnlit()
    {
        Material gem = AssetDatabase.LoadAssetAtPath<Material>(GemMatPath);
        Material ridge = AssetDatabase.LoadAssetAtPath<Material>(RidgeMatPath);

        Assert.That(gem, Is.Not.Null, GemMatPath);
        Assert.That(ridge, Is.Not.Null, RidgeMatPath);
        Assert.That(gem.shader.name, Is.EqualTo(UnlitShaderName),
            "보석이 라이팅을 받으면 점등/소등이 스페큘러에 씻깁니다.");
        Assert.That(ridge.shader.name, Is.EqualTo(UnlitShaderName));
    }

    [Test]
    public void BakedPrefabGemsReferenceOnlyTwoBakedMaterials()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Material gemMat = AssetDatabase.LoadAssetAtPath<Material>(GemMatPath);
        Material ridgeMat = AssetDatabase.LoadAssetAtPath<Material>(RidgeMatPath);
        Assert.That(prefab, Is.Not.Null, PrefabPath);

        Transform platform = prefab.transform.Find("Sector_100_Stone_Platform");
        Assert.That(platform, Is.Not.Null);

        int gemCount = 0;
        int ridgeCount = 0;
        for (int i = 0; i < 3; i++)
        {
            Transform gemRoot = platform.Find($"Faceted_Sapphire_Gem_{i}");
            Assert.That(gemRoot, Is.Not.Null, $"Faceted_Sapphire_Gem_{i}");

            var body = gemRoot.Find("Faceted_Gem_Mesh")?.GetComponent<MeshRenderer>();
            Assert.That(body, Is.Not.Null, "Faceted_Gem_Mesh");
            Assert.That(body.sharedMaterial, Is.SameAs(gemMat),
                "재베이크로 _01 사본이 생기면 구운 머티리얼 수정이 화면에 반영되지 않습니다.");
            gemCount++;

            Transform ridges = gemRoot.Find("Facet_Ridge_Lines");
            Assert.That(ridges, Is.Not.Null, "Facet_Ridge_Lines");
            for (int r = 0; r < ridges.childCount; r++)
            {
                Assert.That(ridges.GetChild(r).GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(ridgeMat));
                ridgeCount++;
            }
        }

        Assert.That(gemCount, Is.EqualTo(3));
        Assert.That(ridgeCount, Is.EqualTo(18));
    }

    [Test]
    public void BuildGeometry_CreatesNoLightOnGemsAndRidges()
    {
        RerollCounterBar bar = BuildFreshBar();

        Assert.That(bar.GetComponentsInChildren<Light>(true), Is.Empty,
            "점등 표현은 색으로만 합니다. 라이트는 장식 레이어를 비추지도 못했습니다.");
    }

    [Test]
    public void BuildGeometry_KeepsGemMaterialNames()
    {
        RerollCounterBar bar = BuildFreshBar();
        Transform platform = bar.transform.Find("Sector_100_Stone_Platform");
        Transform gemRoot = platform.Find("Faceted_Sapphire_Gem_0");

        // 베이커가 이 이름으로 에셋 파일명을 만든다(TabletopPrefabBaker.ExtractMaterials).
        Assert.That(gemRoot.Find("Faceted_Gem_Mesh").GetComponent<MeshRenderer>().sharedMaterial.name,
            Is.EqualTo("Counter_HexGemBaseMat"));
        Assert.That(gemRoot.Find("Facet_Ridge_Lines").GetChild(0).GetComponent<MeshRenderer>().sharedMaterial.name,
            Is.EqualTo("Counter_GemRidgeBaseMat"));
    }

    [Test]
    public void RebuildingGeometryStartsFadeFromCurrentRerollCount()
    {
        RerollCounterBar bar = BuildFreshBar();
        bar.SetRollsRemaining(1);

        // 프리팹 경로는 BindExistingGeometry를 탄다. 여기서 시드하지 않으면 3개가 모두 켜진
        // 상태에서 시작해 잘못된 페이드 아웃이 한 번 재생된다.
        bar.EnsureGeometry();

        Assert.That(ReadFadeProgress(bar), Is.EqualTo(new[] { 1f, 0f, 0f }));
    }

    private RerollCounterBar BuildFreshBar()
    {
        testRoot = new GameObject("Reroll Counter Test Root");
        RerollCounterBar bar = RerollCounterBar.Create(testRoot.transform, Vector3.zero);
        Assert.That(bar.transform.Find("Sector_100_Stone_Platform"), Is.Not.Null);
        return bar;
    }

    private static float[] ReadFadeProgress(RerollCounterBar bar)
    {
        FieldInfo field = typeof(RerollCounterBar).GetField(
            "gemFadeProgress", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "gemFadeProgress 필드를 찾지 못했습니다.");
        return (float[])field.GetValue(bar);
    }
}
