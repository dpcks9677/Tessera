using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Tessera.Rendering
{
    /// <summary>
    /// 픽셀 격자 위에서 뎁스 외곽선과 노멀 모서리 하이라이트를 합성한다(M10.5-T2).
    ///
    /// <see cref="PixelEdgeCamera"/>가 붙은 카메라에서만 돈다. 패스 시점을
    /// <see cref="RenderPassEvent.AfterRenderingSkybox"/>로 두는 것이 이 피처의 계약이다.
    /// URP의 뎁스 텍스처는 불투명 패스까지만 담으므로 그 뒤에서 합성하면 반투명 오브젝트
    /// 자리에 배경 뎁스로 계산한 외곽선이 덧칠된다. 같은 이유로 코스믹 큐브처럼 스스로
    /// 빛나는 반투명 오브젝트는 이 패스가 끝난 뒤에 그려져 검은 테를 두르지 않는다.
    /// </summary>
    public sealed class PixelEdgeRendererFeature : ScriptableRendererFeature
    {
        public const string ShaderPath = "DicePoC/PixelEdge";

        private static readonly int depthEdgeStrengthId = Shader.PropertyToID("_DepthEdgeStrength");
        private static readonly int normalEdgeStrengthId = Shader.PropertyToID("_NormalEdgeStrength");
        private static readonly int depthEdgeThresholdId = Shader.PropertyToID("_DepthEdgeThreshold");

        [SerializeField] private Shader edgeShader;
        [SerializeField, Range(0f, 1f)] private float depthEdgeStrength = 0.85f;
        [SerializeField, Range(0f, 1f)] private float normalEdgeStrength = 0.55f;
        [SerializeField] private Vector2 depthEdgeThreshold = new(0.18f, 0.4f);
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingSkybox;

        private PixelEdgePass pass;
        private Material material;

        public RenderPassEvent PassEvent => passEvent;

        /// <summary>이 카메라가 엣지 패스를 받을 대상인지. 테스트가 이 조건을 직접 확인한다.</summary>
        public static bool ShouldRender(Camera camera)
        {
            if (camera == null) return false;
            PixelEdgeCamera marker = camera.GetComponent<PixelEdgeCamera>();
            return marker != null && marker.EdgeFilterEnabled;
        }

        public override void Create()
        {
            if (edgeShader == null) edgeShader = Shader.Find(ShaderPath);
            if (edgeShader == null)
            {
                pass = null;
                return;
            }

            if (material == null || material.shader != edgeShader)
            {
                CoreUtils.Destroy(material);
                material = CoreUtils.CreateEngineMaterial(edgeShader);
            }
            pass = new PixelEdgePass(material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pass == null || material == null) return;
            if (!ShouldRender(renderingData.cameraData.camera)) return;

            material.SetFloat(depthEdgeStrengthId, depthEdgeStrength);
            material.SetFloat(normalEdgeStrengthId, normalEdgeStrength);
            material.SetVector(depthEdgeThresholdId, new Vector4(depthEdgeThreshold.x, depthEdgeThreshold.y, 0f, 0f));

            pass.Setup(passEvent);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            pass?.Dispose();
            pass = null;
            CoreUtils.Destroy(material);
            material = null;
        }

        private sealed class PixelEdgePass : ScriptableRenderPass
        {
            private const string ProfilerTag = "Pixel Edge";

            private readonly Material material;
            private RTHandle temporaryColor;

            public PixelEdgePass(Material material)
            {
                this.material = material;
            }

            public void Setup(RenderPassEvent passEvent)
            {
                renderPassEvent = passEvent;
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            }

            /// <summary>
            /// Render Graph 경로. 이 프로젝트의 기본 설정이다
            /// (<c>RenderGraphSettings.m_EnableRenderCompatibilityMode = 0</c>).
            ///
            /// 카메라 컬러를 임시 타깃에 엣지 합성으로 옮긴 뒤 그 타깃을 새 카메라 컬러로 삼는다.
            /// 되돌려 쓰는 블릿이 한 번 줄고, 뒤따르는 반투명 패스가 자연스럽게 그 위에 그려진다.
            /// </summary>
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null) return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer) return;

                TextureHandle source = resourceData.activeColorTexture;
                if (!source.IsValid()) return;

                TextureDesc descriptor = renderGraph.GetTextureDesc(source);
                descriptor.name = "_PixelEdgeTemp";
                descriptor.clearBuffer = false;
                descriptor.filterMode = FilterMode.Point;
                TextureHandle destination = renderGraph.CreateTexture(descriptor);

                using (IRasterRenderGraphBuilder builder =
                    renderGraph.AddRasterRenderPass(ProfilerTag, out PassData passData))
                {
                    passData.source = source;
                    passData.material = material;

                    builder.UseTexture(source);
                    // 셰이더는 전역으로 묶인 뎁스·노멀을 읽는다. 여기서 의존성을 밝혀 두지 않으면
                    // Render Graph가 두 텍스처를 이 패스보다 먼저 만들어 준다고 보장하지 않는다.
                    if (resourceData.cameraDepthTexture.IsValid()) builder.UseTexture(resourceData.cameraDepthTexture);
                    if (resourceData.cameraNormalsTexture.IsValid()) builder.UseTexture(resourceData.cameraNormalsTexture);
                    builder.SetRenderAttachment(destination, 0);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), data.material, 0));
                }

                resourceData.cameraColor = destination;
            }

            private class PassData
            {
                public TextureHandle source;
                public Material material;
            }

            // 아래 두 메서드는 호환 모드(Render Graph 비활성)에서만 쓰인다.
            // 설정을 되돌렸을 때 기능이 조용히 사라지지 않도록 남겨 둔다.
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                RenderingUtils.ReAllocateHandleIfNeeded(ref temporaryColor, descriptor, FilterMode.Point,
                    TextureWrapMode.Clamp, name: "_PixelEdgeTemp");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null || temporaryColor == null) return;

                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                if (source == null) return;

                CommandBuffer cmd = CommandBufferPool.Get(ProfilerTag);
                Blitter.BlitCameraTexture(cmd, source, temporaryColor, material, 0);
                Blitter.BlitCameraTexture(cmd, temporaryColor, source);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                temporaryColor?.Release();
                temporaryColor = null;
            }
        }
    }
}
