using NUnit.Framework;
using Tessera.Dice;
using Tessera.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 셀 셰이딩 전환의 계약을 고정한다(M10.8).
    ///
    /// 가장 중요한 계약은 "Baseline 경로가 그대로 살아 있다"는 것이다. 채택 여부가 정해지지 않은
    /// 동안 토글을 되돌리면 M10.7까지의 화면이 그대로 돌아와야 하고, 그래야 비교가 성립한다.
    /// </summary>
    [TestFixture]
    public sealed class CelSurfaceTests
    {
        private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        [TearDown]
        public void TearDown()
        {
            DicePaletteCatalog.ClearCache();
        }

        [Test]
        public void 셀_셰이더는_필요한_프로퍼티를_모두_가진다()
        {
            Shader shader = Shader.Find(CelMaterialFactory.ShaderName);
            Assert.That(shader, Is.Not.Null, $"{CelMaterialFactory.ShaderName} 셰이더를 찾지 못했습니다.");
            Assert.That(shader.isSupported, Is.True, "셀 셰이더가 이 플랫폼에서 지원되지 않습니다.");

            Material material = new(shader);
            try
            {
                Assert.That(material.HasProperty("_BaseColor"), Is.True);
                Assert.That(material.HasProperty("_Bands"), Is.True);
                Assert.That(material.HasProperty("_RampValues"), Is.True);
                Assert.That(material.HasProperty("_NormalSnap"), Is.True);
                Assert.That(material.HasProperty("_RimColor"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void 셀_램프는_아트_가이드_명도_램프와_같다()
        {
            // 재료 단계 밴드와 포스트 팔레트가 같은 값을 써야 두 경로를 겹쳐도 색이 어긋나지 않는다.
            Vector4 ramp = TesseraPixelPalette.RampVector;

            Assert.That(ramp.x, Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(ramp.y, Is.EqualTo(0.65f).Within(0.0001f));
            Assert.That(ramp.z, Is.EqualTo(1.00f).Within(0.0001f));
            Assert.That(ramp.w, Is.EqualTo(1.45f).Within(0.0001f));
        }

        [Test]
        public void Baseline_몸체_재질은_기존_URP_Lit을_유지한다()
        {
            Material baseline = DicePaletteCatalog.GetBodyMaterial(DieType.Normal, RenderStyle.Baseline);

            Assert.That(baseline, Is.Not.Null);
            Assert.That(baseline.shader.name, Is.EqualTo(LitShaderName),
                "Baseline 경로가 바뀌면 되돌릴 기준선이 사라집니다.");
        }

        [Test]
        public void Cel_몸체_재질은_셀_셰이더와_노멀_스냅을_쓴다()
        {
            Material cel = DicePaletteCatalog.GetBodyMaterial(DieType.Normal, RenderStyle.Cel);

            Assert.That(cel, Is.Not.Null);
            Assert.That(cel.shader.name, Is.EqualTo(CelMaterialFactory.ShaderName));

            // 스냅하지 않으면 휜 노멀 위에 동심원 밴드가 생겨 포스트 양자화와 같은 실패가 재현된다.
            Assert.That(cel.GetFloat("_NormalSnap"), Is.EqualTo(1f),
                "주사위는 면이 오브젝트 축에 정렬돼 있어 노멀 스냅이 켜져 있어야 합니다.");
        }

        [Test]
        public void 금속_주사위는_밴드를_하나_더_받는다()
        {
            Material diffuse = DicePaletteCatalog.GetBodyMaterial(DieType.Normal, RenderStyle.Cel);
            Material metallic = DicePaletteCatalog.GetBodyMaterial(DieType.Metal, RenderStyle.Cel);

            Assert.That(diffuse.GetFloat("_Bands"), Is.EqualTo((float)CelMaterialFactory.DiffuseBands));
            Assert.That(metallic.GetFloat("_Bands"), Is.EqualTo((float)CelMaterialFactory.MetallicBands),
                "스페큘러가 사라진 자리를 밝은 밴드가 대신합니다.");
        }

        [Test]
        public void 눈_재질은_두_모드가_공용이다()
        {
            // 눈은 M10.6에서 이미 Unlit 평면색이라 셀 전환의 대상이 아니다.
            Material pip = DicePaletteCatalog.GetPipMaterial(DieType.Normal);

            Assert.That(pip, Is.Not.Null);
            Assert.That(pip.shader.name, Does.Contain("Unlit"));
        }

        [Test]
        public void PC_렌더러는_Forward로_동작한다()
        {
            // 셀 셰이더는 커스텀 라이팅이라 GBuffer를 채우지 않는다. Deferred면 엣지 피처가 읽는
            // 노멀이 비어 주사위에서 노멀 엣지가 사라진다.
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(PcRendererPath);

            Assert.That(rendererData, Is.Not.Null, $"{PcRendererPath} 을 찾지 못했습니다.");
            Assert.That(rendererData.renderingMode, Is.EqualTo(RenderingMode.Forward));
        }
    }
}
