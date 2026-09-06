using NUnit.Framework;
using Tessera.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        private const string ScenePath = "Assets/Scenes/Augmented Dice.unity";

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
        public void 업스케일_셰이더는_양자화_프로퍼티를_가진다()
        {
            Shader shader = Shader.Find("DicePoC/PixelUpscale");
            Assert.That(shader, Is.Not.Null, "DicePoC/PixelUpscale 셰이더를 찾지 못했습니다.");

            Material material = new(shader);
            try
            {
                Assert.That(material.HasProperty("_Quantize"), Is.True);
                Assert.That(material.HasProperty("_ColorSteps"), Is.True);
                Assert.That(material.HasProperty("_DitherStrength"), Is.True);
                Assert.That(material.GetFloat("_Quantize"), Is.EqualTo(0f),
                    "기본값은 꺼짐이어야 씬에 구워진 재질과 어긋나지 않습니다.");
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void 씬에_구워진_업스케일_재질이_게임_시작값과_같다()
        {
            // 플레이 전 에디터 프리뷰와 플레이 직후 화면이 달라지지 않게 고정한다.
            // 시작 해상도를 640x360에서 480x270으로 바꿨을 때 실제로 어긋났던 자리다.
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Material upscale = FindUpscaleMaterial(scene);
                Assert.That(upscale, Is.Not.Null, $"{ScenePath} 에서 업스케일 재질을 찾지 못했습니다.");

                Vector2Int expected = PixelFilterSettings.StartResolution;
                Vector4 actual = upscale.GetVector("_VirtualResolution");

                Assert.That(actual.x, Is.EqualTo((float)expected.x),
                    "씬 재질의 격자가 게임 시작 해상도와 다릅니다. Tools/Tessera/Sync Pixel Filter Preview 를 실행하십시오.");
                Assert.That(actual.y, Is.EqualTo((float)expected.y));
                Assert.That(upscale.GetFloat("_Quantize"), Is.EqualTo((float)PixelFilterSettings.StartQuantizeMode));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        private static Material FindUpscaleMaterial(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (RawImage image in root.GetComponentsInChildren<RawImage>(true))
                {
                    Material material = image.material;
                    if (material != null && material.shader != null && material.shader.name == "DicePoC/PixelUpscale")
                    {
                        return material;
                    }
                }
            }
            return null;
        }

        [Test]
        public void 팔레트는_셰이더_배열에_들어가고_색이_겹치지_않는다()
        {
            Color[] palette = TesseraPixelPalette.Build();

            Assert.That(palette.Length, Is.LessThanOrEqualTo(TesseraPixelPalette.MaxColors),
                "팔레트가 셰이더 배열보다 크면 뒤쪽 색이 잘립니다.");

            foreach (Color color in palette)
            {
                Assert.That(color.r, Is.InRange(0f, 1f));
                Assert.That(color.g, Is.InRange(0f, 1f));
                Assert.That(color.b, Is.InRange(0f, 1f));
            }

            var seen = new System.Collections.Generic.HashSet<Color32>();
            foreach (Color color in palette)
            {
                Assert.That(seen.Add(color), Is.True, $"중복 색이 팔레트 칸을 낭비합니다: {color}");
            }

            Vector4[] shaderArray = TesseraPixelPalette.BuildShaderArray(out int count);
            Assert.That(shaderArray.Length, Is.EqualTo(TesseraPixelPalette.MaxColors));
            Assert.That(count, Is.EqualTo(palette.Length));
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
