using System.Collections.Generic;
using Tessera.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// `dice-alchemy`(56) 발동 시 킵 안 된 주사위 위로 연기를 터뜨려 눈이 바뀌는 순간을 가린다(M17-T9-1-4).
    ///
    /// 파티클 시스템 하나로 위치만 바꿔 쏘는 구조다. 게임당 1회 연출이라 풀링하지 않는다.
    /// </summary>
    public sealed class DiceSmokePuffVfx : MonoBehaviour
    {
        private const int ParticlesPerBurst = 14;
        private static readonly Color[] Palette =
        {
            new(0.898f, 0.663f, 0.235f), // #e5a93c 러너 골드
            new(0.533f, 0.176f, 0.133f), // #882d22 크림슨
            new(0.212f, 0.294f, 0.431f), // #364b6e 쿨 인디고
            new(1.000f, 0.620f, 0.235f)  // #ff9e3b 웜 앰버
        };

        private ParticleSystem smoke;

        private void Awake()
        {
            gameObject.layer = TesseraLayers.Dice;

            smoke = gameObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = smoke.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 80;
            main.startLifetime = 0.6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.startSize = DiceBoardMetrics.DieSize * 3.2f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(BuildPaletteGradient())
            {
                mode = ParticleSystemGradientMode.RandomColor
            };

            ParticleSystem.EmissionModule emission = smoke.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = DiceBoardMetrics.DieSize * 0.55f;

            ParticleSystem.SizeOverLifetimeModule size = smoke.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve sizeCurve = new(
                new Keyframe(0f, 1.0f),
                new Keyframe(0.3f, 1.5f),
                new Keyframe(1f, 1.5f)
            );
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ParticleSystem.ColorOverLifetimeModule color = smoke.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f) },
                // 눈 교체 시점(0.25초, 수명 대비 약 0.42)까지 알파 1.0을 유지해야 가림이 성립한다.
                // 0.55까지로 여유를 두고 그 뒤에 계단으로 걷어낸다.
                new[]
                {
                    new GradientAlphaKey(1.00f, 0.00f),
                    new GradientAlphaKey(1.00f, 0.55f),
                    new GradientAlphaKey(0.75f, 0.57f),
                    new GradientAlphaKey(0.75f, 0.70f),
                    new GradientAlphaKey(0.50f, 0.72f),
                    new GradientAlphaKey(0.50f, 0.85f),
                    new GradientAlphaKey(0.25f, 0.87f),
                    new GradientAlphaKey(0.00f, 1.00f)
                }
            );
            color.color = gradient;

            ParticleSystemRenderer psRenderer = smoke.GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;

            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Standard");

            Material material = new(particleShader) { name = "DiceSmokePuff_Mat" };
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1); // 1 = Transparent
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0);     // 0 = Alpha
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");

            Texture2D texture = Resources.Load<Texture2D>("Vfx/DiceSmokePuff");
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            }
            psRenderer.material = material;
        }

        /// <summary>지정한 월드 위치마다 연기를 한 뭉치씩 터뜨린다. 위치는 주사위 중심이며, 방출 시 카메라 쪽으로 끌어올려 쏜다.</summary>
        public void Burst(IReadOnlyList<Vector3> worldPositions)
        {
            if (worldPositions == null || worldPositions.Count == 0) return;

            foreach (Vector3 position in worldPositions)
            {
                // 카메라가 위(약 75° 피치)에서 내려다보므로 월드 +Y가 카메라 쪽이다.
                // 주사위 중심에서 그대로 쏘면 깊이 테스트에서 주사위 메시 뒤 픽셀이 버려져 눈을 못 가린다.
                // 주사위 앞쪽으로 끌어올려 깊이 테스트를 통과시킨다.
                Vector3 emitPosition = position + Vector3.up * (DiceBoardMetrics.DieSize * 0.9f);
                ParticleSystem.EmitParams emitParams = new()
                {
                    position = emitPosition,
                    applyShapeToPosition = true
                };
                smoke.Emit(emitParams, ParticlesPerBurst);
            }
        }

        private static Gradient BuildPaletteGradient()
        {
            Gradient gradient = new();
            // RandomColor 모드는 그라디언트를 보간한다. 키를 그대로 4개만 두면 중간 갈색이
            // 섞여 나와 팔레트 4색이 아니라 주황 띠 하나로 뭉갠다. 색마다 구간을 잡고 경계에
            // 키를 두 개씩 붙여 계단으로 세워 보간을 막는다.
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Palette[0], 0.0000f),
                    new GradientColorKey(Palette[0], 0.2499f),
                    new GradientColorKey(Palette[1], 0.2500f),
                    new GradientColorKey(Palette[1], 0.4999f),
                    new GradientColorKey(Palette[2], 0.5000f),
                    new GradientColorKey(Palette[2], 0.7499f),
                    new GradientColorKey(Palette[3], 0.7500f),
                    new GradientColorKey(Palette[3], 1.0000f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            return gradient;
        }
    }
}
