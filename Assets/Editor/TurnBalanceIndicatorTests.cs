using NUnit.Framework;
using System.Reflection;
using Tessera.Tabletop;
using UnityEngine;

public sealed class TurnBalanceIndicatorTests
{
    private GameObject testRoot;
    private TurnBalanceIndicator indicator;

    [SetUp]
    public void SetUp()
    {
        testRoot = new GameObject("Turn Balance Test Root");
        indicator = TurnBalanceIndicator.Create(testRoot.transform);
    }

    [TearDown]
    public void TearDown()
    {
        if (testRoot != null) Object.DestroyImmediate(testRoot);
    }

    [Test]
    public void BuildGeometry_필수파트를_중복없이_재생성한다()
    {
        indicator.BuildGeometry();
        indicator.BuildGeometry();

        Assert.That(indicator.transform.Find("Balance_Beam_Pivot"), Is.Not.Null);
        Assert.That(indicator.transform.Find("Balance_Beam_Pivot/Balance_Left_Pan"), Is.Not.Null);
        Assert.That(indicator.transform.Find("Balance_Beam_Pivot/Balance_Right_Pan"), Is.Not.Null);
        Assert.That(indicator.transform.Find("Turn_Wax_Seal"), Is.Not.Null);
        Assert.That(CountDirectChildren("Balance_Beam_Pivot"), Is.EqualTo(1));
        Assert.That(CountDirectChildren("Turn_Wax_Seal"), Is.EqualTo(1));
        Assert.That(indicator.GetComponentInChildren<Rigidbody>(true), Is.Null);
    }

    [Test]
    public void SetActiveSide_왼쪽과_오른쪽의_기울기와_인장위치를_대칭으로_표현한다()
    {
        indicator.SetActiveSide(TurnSide.Left, false);
        float leftAngle = indicator.CurrentBeamAngle;
        float leftSealX = indicator.Seal.localPosition.x;

        indicator.SetActiveSide(TurnSide.Right, false);
        float rightAngle = indicator.CurrentBeamAngle;
        float rightSealX = indicator.Seal.localPosition.x;

        Assert.That(indicator.CurrentSide, Is.EqualTo(TurnSide.Right));
        Assert.That(leftAngle, Is.EqualTo(9f).Within(0.01f));
        Assert.That(rightAngle, Is.EqualTo(-9f).Within(0.01f));
        Assert.That(leftSealX, Is.LessThan(0f));
        Assert.That(rightSealX, Is.GreaterThan(0f));
        Assert.That(Mathf.Abs(leftSealX), Is.EqualTo(Mathf.Abs(rightSealX)).Within(0.01f));
    }

    [Test]
    public void SetActiveSide_None은_천칭과_인장을_중앙으로_복원한다()
    {
        indicator.SetActiveSide(TurnSide.Left, false);
        indicator.SetActiveSide(TurnSide.None, false);

        Assert.That(indicator.CurrentSide, Is.EqualTo(TurnSide.None));
        Assert.That(indicator.CurrentBeamAngle, Is.EqualTo(0f).Within(0.01f));
        Assert.That(indicator.Seal.localPosition.x, Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void Create_장식레이어와_앤틱실버_왁스재질을_사용한다()
    {
        Assert.That(indicator.gameObject.layer, Is.EqualTo(11));
        Assert.That(indicator.transform.localEulerAngles.y, Is.EqualTo(50f).Within(0.01f));

        Renderer[] renderers = indicator.GetComponentsInChildren<Renderer>(true);
        Assert.That(System.Array.Exists(renderers,
            renderer => renderer.sharedMaterial != null && renderer.sharedMaterial.name.Contains("AntiqueSilver")), Is.True);
        Assert.That(System.Array.Exists(renderers,
            renderer => renderer.sharedMaterial != null && renderer.sharedMaterial.name.Contains("CrimsonWax")), Is.True);
    }

    [Test]
    public void BuildGeometry_판타지형_기단과_곡선빔_링크체인_오목접시를_생성한다()
    {
        Assert.That(indicator.transform.Find("Balance_Ornate_Base_Lower"), Is.Not.Null);
        Assert.That(indicator.transform.Find("Balance_Turned_Column"), Is.Not.Null);
        Assert.That(indicator.transform.Find("Balance_Beam_Pivot/Balance_Center_Shield"), Is.Not.Null);

        Transform chain = indicator.transform.Find("Balance_Beam_Pivot/Balance_Left_Chain_Inner");
        Assert.That(chain, Is.Not.Null);
        Assert.That(chain.childCount, Is.EqualTo(5));

        MeshFilter bowl = indicator.transform
            .Find("Balance_Beam_Pivot/Balance_Left_Pan/Balance_Left_Pan_Bowl")
            .GetComponent<MeshFilter>();
        Assert.That(bowl.sharedMesh.name, Does.Contain("Bowl"));
        Assert.That(bowl.sharedMesh.vertexCount, Is.GreaterThan(100));
    }

    [Test]
    public void 인장_이동궤적은_시작과_도착을_정확히_지나며_하나의_부드러운_호를_그린다()
    {
        MethodInfo evaluateArc = typeof(TurnBalanceIndicator).GetMethod(
            "EvaluateTransferArc", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(evaluateArc, Is.Not.Null);

        Vector3 start = new(-1f, 0.5f, -0.4f);
        Vector3 end = new(1f, 0.5f, -0.4f);
        Vector3 previous = start;
        for (int i = 0; i <= 20; i++)
        {
            float t = i / 20f;
            Vector3 point = (Vector3)evaluateArc.Invoke(null, new object[] { start, end, t });
            if (i > 0) Assert.That(point.x, Is.GreaterThan(previous.x));
            Assert.That(point.z, Is.EqualTo(start.z).Within(0.0001f));
            previous = point;
        }

        Vector3 first = (Vector3)evaluateArc.Invoke(null, new object[] { start, end, 0f });
        Vector3 apex = (Vector3)evaluateArc.Invoke(null, new object[] { start, end, 0.5f });
        Vector3 last = (Vector3)evaluateArc.Invoke(null, new object[] { start, end, 1f });
        Assert.That(first, Is.EqualTo(start));
        Assert.That(last, Is.EqualTo(end));
        Assert.That(apex.y, Is.GreaterThan(start.y));
    }

    private int CountDirectChildren(string name)
    {
        int count = 0;
        for (int i = 0; i < indicator.transform.childCount; i++)
        {
            if (indicator.transform.GetChild(i).name == name) count++;
        }
        return count;
    }
}
