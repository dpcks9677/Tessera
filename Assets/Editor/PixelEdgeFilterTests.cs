using NUnit.Framework;
using Tessera.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 픽셀 엣지 필터의 계약을 고정한다(M10.5).
    ///
    /// 특히 패스 시점이 <see cref="RenderPassEvent.AfterRenderingSkybox"/>라는 점은 아트 취향이
    /// 아니라 회귀 방지 계약이다. 그 뒤로 옮기면 코스믹 큐브 같은 반투명 오브젝트 위에
    /// 배경 뎁스로 계산한 검은 외곽선이 덧칠된다.
    /// </summary>
    [TestFixture]
    public sealed class PixelEdgeFilterTests
    {
        private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";

        [Test]
        public void 엣지_셰이더는_필요한_프로퍼티를_모두_가진다()
        {
            Shader shader = Shader.Find(PixelEdgeRendererFeature.ShaderPath);
            Assert.That(shader, Is.Not.Null, $"{PixelEdgeRendererFeature.ShaderPath} 셰이더를 찾지 못했습니다.");
            Assert.That(shader.isSupported, Is.True, "엣지 셰이더가 이 플랫폼에서 지원되지 않습니다.");

            Material material = new(shader);
            try
            {
                Assert.That(material.HasProperty("_DepthEdgeStrength"), Is.True);
                Assert.That(material.HasProperty("_NormalEdgeStrength"), Is.True);
                Assert.That(material.HasProperty("_DepthEdgeThreshold"), Is.True);

                // 가상 해상도는 전역 유니폼이다. Properties에 있으면 재질 기본값이 전역 값을
                // 덮어써서 해상도 전환이 반영되지 않으므로, 없는 것이 정상이다.
                Assert.That(material.HasProperty("_PixelEdgeVirtualResolution"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void PC_렌더러에_엣지_피처가_하나만_등록된다()
        {
            ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(PcRendererPath);
            Assert.That(rendererData, Is.Not.Null, $"{PcRendererPath} 을 찾지 못했습니다.");

            int count = 0;
            foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
            {
                if (feature is PixelEdgeRendererFeature) count++;
            }
            Assert.That(count, Is.EqualTo(1), "엣지 피처는 렌더러당 하나여야 합니다.");
        }

        [Test]
        public void 엣지_피처는_불투명_직후에_실행된다()
        {
            ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(PcRendererPath);
            Assert.That(rendererData, Is.Not.Null);

            PixelEdgeRendererFeature edgeFeature = null;
            foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
            {
                if (feature is PixelEdgeRendererFeature found) edgeFeature = found;
            }

            Assert.That(edgeFeature, Is.Not.Null);
            Assert.That(edgeFeature.PassEvent, Is.EqualTo(RenderPassEvent.AfterRenderingSkybox),
                "이 시점보다 뒤로 옮기면 반투명 오브젝트 위에 배경 뎁스 기준 외곽선이 덧칠됩니다.");
        }

        [Test]
        public void 표시가_없거나_꺼진_카메라는_엣지_패스를_받지_않는다()
        {
            GameObject cameraObject = new("Pixel Edge Test Camera", typeof(Camera));
            try
            {
                Camera camera = cameraObject.GetComponent<Camera>();
                Assert.That(PixelEdgeRendererFeature.ShouldRender(camera), Is.False, "표시가 없는 카메라입니다.");

                PixelEdgeCamera marker = cameraObject.AddComponent<PixelEdgeCamera>();
                Assert.That(PixelEdgeRendererFeature.ShouldRender(camera), Is.True, "표시가 붙으면 대상이 됩니다.");

                marker.EdgeFilterEnabled = false;
                Assert.That(PixelEdgeRendererFeature.ShouldRender(camera), Is.False, "꺼 두면 패스가 등록되지 않습니다.");
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
