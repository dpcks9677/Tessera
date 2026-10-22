using Tessera.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Games.Yacht.Presentation
{
    /// <summary>요트 뱅크 저축 중 킵 칸 슬롯 0 위치에 금색 테두리와 반짝이를 띄우는 절차 생성 연출입니다.</summary>
    public sealed class YachtBankKeepZoneVfx : MonoBehaviour
    {
        private static readonly Color GoldColor = new(0.898f, 0.663f, 0.235f, 1f);
        private static readonly float[] PulseSteps = { 0.55f, 0.7f, 0.85f, 1.0f, 0.85f, 0.7f };

        private const float PulseStepSeconds = 0.12f;
        private const float SurfaceEpsilon = 0.01f;

        // 트레이 메시 실측: 소켓 앞쪽(카메라 쪽) 턱 윗면이 소켓 바닥보다 3 소스 단위 높다.
        private const float FrontLipHeight = 3f * DiceBoardMetrics.TrayScale;
        private const float BorderY = DiceBoardMetrics.KeepSurfaceY - DiceBoardMetrics.KeptDieHalfSize + FrontLipHeight + SurfaceEpsilon;
        private const float SparkleWorldY = DiceBoardMetrics.KeepSurfaceY + DiceBoardMetrics.KeptDieHalfSize;

        private bool visible;
        private GameObject root;
        private Material borderMaterial;
        private ParticleSystem sparkle;
        private float pulseTimer;
        private int pulseIndex;

        /// <param name="centerX">주사위 루트의 월드 X 오프셋. 킵 좌표는 이 루트 기준이라 더해 준다.</param>
        public void SetVisible(bool visible, float centerX = 0f)
        {
            if (this.visible == visible) return;
            if (visible && root == null) CreateRoot();

            this.visible = visible;
            if (root == null) return;

            if (visible)
            {
                // Z 는 시차 보정(KeepParallaxZ) 없이 소켓 지오메트리 중심(KeepCenterZ)을 쓴다.
                Vector3 keep = DiceBoardMetrics.GetKeepPosition(0);
                root.transform.position = new Vector3(centerX + keep.x, BorderY, DiceBoardMetrics.KeepCenterZ);
                pulseTimer = 0f;
                pulseIndex = 0;
                ApplyPulse();
                root.SetActive(true);
                sparkle.Play(true);
            }
            else
            {
                sparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                root.SetActive(false);
            }
        }

        private void Update()
        {
            if (!visible || borderMaterial == null) return;

            pulseTimer += Time.unscaledDeltaTime;
            if (pulseTimer < PulseStepSeconds) return;

            pulseTimer -= PulseStepSeconds;
            pulseIndex = (pulseIndex + 1) % PulseSteps.Length;
            ApplyPulse();
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root);
            if (borderMaterial != null) Destroy(borderMaterial);
        }

        private void ApplyPulse()
        {
            float k = PulseSteps[pulseIndex];
            Color color = new(GoldColor.r * k, GoldColor.g * k, GoldColor.b * k, 1f);
            if (borderMaterial.HasProperty("_BaseColor")) borderMaterial.SetColor("_BaseColor", color);
            if (borderMaterial.HasProperty("_Color")) borderMaterial.SetColor("_Color", color);
        }

        private void CreateRoot()
        {
            float size = 17f * DiceBoardMetrics.TrayScale;
            root = new GameObject("YachtBank_KeepZoneHighlight") { layer = TesseraLayers.Dice };

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) unlit = Shader.Find("Unlit/Color");
            borderMaterial = new Material(unlit) { name = "YachtBank_Border_Mat" };
            ApplyPulse();

            // 안쪽 경계 = 소켓 17 소스 단위, 바깥 경계 = 21 소스 단위. 소켓 가장자리에서 바깥쪽 턱 위에 얹힌다.
            // 뒤쪽·측면 뒤 부분은 벽에 묻혀 안 보여도 되고, 앞쪽 U 자가 보이면 된다.
            float inner = size;
            float thickness = 2f * DiceBoardMetrics.TrayScale;
            float outer = inner + thickness * 2f;
            float mid = inner * 0.5f + thickness * 0.5f;
            // 위·아래·왼·오른쪽 순. 가로 변은 바깥 전체 폭, 세로 변은 가로 변 사이만 채운다.
            CreateBorderQuad("Top", new Vector3(0f, 0f, mid), new Vector2(outer, thickness));
            CreateBorderQuad("Bottom", new Vector3(0f, 0f, -mid), new Vector2(outer, thickness));
            CreateBorderQuad("Left", new Vector3(-mid, 0f, 0f), new Vector2(thickness, inner));
            CreateBorderQuad("Right", new Vector3(mid, 0f, 0f), new Vector2(thickness, inner));

            CreateSparkle(size);
            root.SetActive(false);
        }

        private void CreateBorderQuad(string quadName, Vector3 localPos, Vector2 quadSize)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = quadName;
            // 주사위 물리에 간섭하지 않도록 기본 콜라이더를 제거한다.
            Destroy(quad.GetComponent<MeshCollider>());
            quad.layer = TesseraLayers.Dice;
            quad.transform.SetParent(root.transform, false);
            quad.transform.localPosition = localPos;
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(quadSize.x, quadSize.y, 1f);

            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = borderMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void CreateSparkle(float size)
        {
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
            Material particleMaterial = particleShader != null ? new Material(particleShader) : borderMaterial;
            if (particleShader != null)
            {
                particleMaterial.name = "YachtBank_Sparkle_Mat";
                if (particleMaterial.HasProperty("_BaseColor")) particleMaterial.SetColor("_BaseColor", Color.white);
                if (particleMaterial.HasProperty("_Color")) particleMaterial.SetColor("_Color", Color.white);
            }

            GameObject particleObject = new("YachtBank_Sparkle") { layer = TesseraLayers.Dice };
            particleObject.transform.SetParent(root.transform, false);
            // 파티클은 킵 주사위 윗면 높이에서 시작한다. 루트는 테두리 높이(BorderY)에 있다.
            particleObject.transform.localPosition = new Vector3(0f, SparkleWorldY - BorderY, 0f);
            particleObject.transform.localRotation = Quaternion.identity;

            sparkle = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = sparkle.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = 16;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.06f, size * 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(GoldColor, Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            ParticleSystem.EmissionModule emission = sparkle.emission;
            emission.rateOverTime = 5f;

            ParticleSystem.ShapeModule shape = sparkle.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size, size * 0.1f, size);

            ParticleSystem.VelocityOverLifetimeModule vel = sparkle.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(size * 0.4f, size * 0.7f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystemRenderer psRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
            psRenderer.sharedMaterial = particleMaterial;
            psRenderer.shadowCastingMode = ShadowCastingMode.Off;
            psRenderer.receiveShadows = false;
        }
    }
}
