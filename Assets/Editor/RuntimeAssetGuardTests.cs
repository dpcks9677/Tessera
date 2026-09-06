using NUnit.Framework;
using Tessera.Core;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디트 모드 사본이 씬 저장 때 null로 기록되지 않는지 검증한다.
///
/// 사본을 끼운 채로 저장하면 프리팹 인스턴스에 null 오버라이드가 남아 프롭이 통째로
/// 기본 머티리얼로 보인다. 코스믹 큐브에서 실제로 발생했던 문제다.
/// </summary>
public class RuntimeAssetGuardTests
{
    private const string MaterialPath =
        "Assets/Art/Generated/Tabletop/Materials/3D_Roll_Cosmic_Cube_Cosmic_Energy_Core_Mat.mat";

    private GameObject target;

    [SetUp]
    public void SetUp()
    {
        target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.hideFlags = HideFlags.HideAndDontSave;
    }

    [TearDown]
    public void TearDown()
    {
        if (target != null) Object.DestroyImmediate(target);
    }

    [Test]
    public void 에셋_머티리얼은_저장되지_않는_사본으로_바뀐다()
    {
        Material asset = LoadMaterialAsset();
        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = asset;

        Material writable = RuntimeAssetGuard.GetWritableMaterial(renderer);

        Assert.AreNotSame(asset, writable);
        Assert.IsFalse(EditorUtility.IsPersistent(writable));
        Assert.AreEqual(HideFlags.DontSave, writable.hideFlags);
        Assert.AreSame(writable, renderer.sharedMaterial);
    }

    [Test]
    public void 저장_직전에는_구운_에셋_참조로_돌아온다()
    {
        Material asset = LoadMaterialAsset();
        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = asset;
        Material clone = RuntimeAssetGuard.GetWritableMaterial(renderer);

        RuntimeAssetGuard.RestoreBakedAssets();
        Assert.AreSame(asset, renderer.sharedMaterial, "저장되는 씬에는 에셋 참조가 남아야 한다");

        RuntimeAssetGuard.ReapplyEditorClones();
        Assert.AreSame(clone, renderer.sharedMaterial, "저장 뒤에는 원래 사본이 그대로 돌아와야 한다");
    }

    /// <summary>
    /// 프리팹에서 온 메시는 인스턴스끼리 공유된다. 정점을 다시 쓰는 프롭을 둘 이상 놓으면
    /// 두 인스턴스가 같은 버퍼를 덮어쓰므로, 필터마다 사본을 갈라 줘야 한다.
    /// </summary>
    [Test]
    public void 공유된_메시는_필터마다_별도_사본으로_갈린다()
    {
        GameObject other = GameObject.CreatePrimitive(PrimitiveType.Cube);
        other.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            MeshFilter first = target.GetComponent<MeshFilter>();
            MeshFilter second = other.GetComponent<MeshFilter>();
            Assert.AreSame(first.sharedMesh, second.sharedMesh, "전제: 두 프리미티브가 같은 내장 메시를 쓴다");

            Mesh firstWritable = RuntimeAssetGuard.GetWritableMesh(first);
            Mesh secondWritable = RuntimeAssetGuard.GetWritableMesh(second);

            Assert.AreNotSame(firstWritable, secondWritable, "필터마다 다른 메시를 받아야 한다");
            Assert.AreSame(firstWritable, first.sharedMesh);
            Assert.AreSame(secondWritable, second.sharedMesh);
            Assert.AreEqual(HideFlags.DontSave, firstWritable.hideFlags);
        }
        finally
        {
            Object.DestroyImmediate(other);
        }
    }

    [Test]
    public void 이미_갈라_준_메시는_다시_복제하지_않는다()
    {
        MeshFilter filter = target.GetComponent<MeshFilter>();

        Mesh first = RuntimeAssetGuard.GetWritableMesh(filter);
        Mesh second = RuntimeAssetGuard.GetWritableMesh(filter);

        Assert.AreSame(first, second, "같은 필터가 다시 물어보면 같은 사본이 돌아와야 한다");
    }

    private static Material LoadMaterialAsset()
    {
        Material asset = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Assert.IsNotNull(asset, $"테스트용 머티리얼 에셋이 없습니다: {MaterialPath}");
        return asset;
    }
}
