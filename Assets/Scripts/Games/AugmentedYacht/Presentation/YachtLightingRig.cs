using System;
using System.Collections.Generic;
using Tessera.Core;
using Tessera.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 테이블 조명을 설정하고 키라이트 프리셋을 전환한다(M10-T3).
    ///
    /// 프리셋 목록이 <c>[SerializeField]</c>라 인스펙터에서 조명 톤을 바꿀 수 있다.
    /// 코드를 고치지 않고 분위기를 조정하는 것이 목적이다.
    /// </summary>
    public sealed class YachtLightingRig : MonoBehaviour
    {
        private const string KeyLightName = "Key Light";
        private const string FillLightName = "Fill Light";

        /// <summary>URP의 SSAO 피처 타입 이름. 어셈블리 참조 없이 찾기 위해 문자열로 비교한다.</summary>
        private const string AmbientOcclusionFeatureTypeName = "ScreenSpaceAmbientOcclusion";

        [Serializable]
        public struct KeyLightPreset
        {
            public string Name;
            public Color Color;
            public float Intensity;
        }

        [Header("Ambient")]
        [SerializeField] private Color ambientLight = new(0.16f, 0.12f, 0.09f);

        [Header("Key Light")]
        [SerializeField] private Vector3 keyLightEulerAngles = new(60f, -35f, 0f);
        [SerializeField] private float shadowStrength = 0.58f;
        [SerializeField] private float shadowBias = 0.005f;
        [SerializeField] private float shadowNormalBias = 0.03f;

        /// <summary>
        /// 필라이트가 가져가는 밝기 비중(0~1). 키라이트가 나머지를 쓴다.
        ///
        /// 키라이트는 yaw -35도라 트레이 안쪽 벽 중 +X를 보는 왼쪽 면만 비춘다. 오른쪽 면은
        /// 주변광만 받아 왼쪽보다 60% 어두웠다. yaw를 뒤집은 필라이트가 그 면을 채운다.
        /// 프리셋 강도를 둘로 나눠 쓰므로 위를 보는 면(림 윗면·펠트 바닥)의 밝기는 그대로다.
        /// </summary>
        [Header("Fill Light")]
        [SerializeField, Range(0f, 0.5f)] private float fillLightShare = 0.25f;

        /// <summary>
        /// 키라이트 프리셋. 배열 순서가 곧 전환 순서다(<see cref="TogglePreset"/>).
        ///
        /// 색이 옅은 Soft Neutral을 앞에 두고 Warm Amber를 그다음에 둔다. 앰버는 주사위 바탕과
        /// 눈 색에 강하게 섞여 팔레트를 확인할 때 색을 혼동하게 만든다. 기본 화면에서 먼저
        /// 색을 있는 그대로 보고, 다음 칸에서 앰버를 비교할 수 있게 한 배치다.
        /// </summary>
        [SerializeField]
        private KeyLightPreset[] presets =
        {
            new() { Name = "Soft Neutral", Color = new Color(1.00f, 0.88f, 0.74f), Intensity = 1.35f },
            new() { Name = "Warm Amber", Color = new Color(1.00f, 0.62f, 0.23f), Intensity = 1.50f },
            new() { Name = "Pure White", Color = new Color(1.00f, 1.00f, 1.00f), Intensity = 1.25f },
            new() { Name = "Cool Moon", Color = new Color(0.55f, 0.70f, 0.95f), Intensity = 1.35f },
            new() { Name = "Cozy Candle", Color = new Color(1.00f, 0.48f, 0.16f), Intensity = 1.55f }
        };

        /// <summary>Soft Neutral을 기본으로 시작한다.</summary>
        [SerializeField] private int currentPresetIndex = 0;
        private Light keyLight;
        private Light fillLight;

        /// <summary>연출 방식(M10.8). Cel에서는 그림자를 하드로 바꾸고 SSAO를 끈다.</summary>
        private RenderStyle renderStyle = RenderStyle.Baseline;

        /// <summary>
        /// SSAO 피처를 끄기 전의 활성 상태. 반드시 되돌려야 한다.
        ///
        /// <see cref="ScriptableRendererFeature"/>의 활성 플래그는 직렬화 필드다. 씬 오브젝트와 달리
        /// 에셋의 런타임 변경은 플레이 모드를 나가도 되돌아오지 않으므로, 끈 채로 플레이를 멈추면
        /// 렌더러 에셋에 그대로 저장된다. Baseline으로 되돌리면 기준선 화면이 그대로 돌아온다는
        /// 이 마일스톤의 계약이 거기서 깨진다.
        /// </summary>
        private readonly Dictionary<ScriptableRendererFeature, bool> ambientOcclusionOriginalStates = new();

        /// <summary>현재 프리셋 이름. 버튼 라벨에 쓴다.</summary>
        public string CurrentPresetName =>
            presets.Length == 0 ? string.Empty : presets[currentPresetIndex].Name;

        /// <summary>프리셋이 바뀌면 새 이름을 알린다.</summary>
        public event Action<string> PresetChanged;

        /// <summary>주변광과 키라이트의 그림자 설정을 적용한다.</summary>
        public void Configure()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientLight;

            if (!TryResolveKeyLight()) return;

            keyLight.enabled = true;
            keyLight.transform.rotation = Quaternion.Euler(keyLightEulerAngles);
            keyLight.cullingMask |= TesseraLayers.Mask(TesseraLayers.Dice);
            keyLight.shadows = ShadowsForCurrentStyle();
            keyLight.shadowStrength = shadowStrength;
            keyLight.shadowBias = shadowBias;
            keyLight.shadowNormalBias = shadowNormalBias;

            ConfigureFillLight();
            ApplyCurrentPreset();
        }

        /// <summary>
        /// 필라이트를 키라이트의 yaw만 뒤집은 방향으로 세운다. 그림자는 끈다.
        ///
        /// 디렉셔널 둘이 그림자를 뿌리면 프롭마다 반대 방향 그림자가 겹쳐 렌더 오류처럼 보인다.
        /// URP Forward는 메인 디렉셔널 하나만 그림자를 지원하므로 켜도 의도대로 나오지 않는다.
        /// </summary>
        private void ConfigureFillLight()
        {
            if (!TryResolveFillLight()) return;

            fillLight.enabled = true;
            fillLight.type = LightType.Directional;
            fillLight.transform.rotation = Quaternion.Euler(
                keyLightEulerAngles.x, -keyLightEulerAngles.y, keyLightEulerAngles.z);
            fillLight.cullingMask |= TesseraLayers.Mask(TesseraLayers.Dice);
            fillLight.shadows = LightShadows.None;
        }

        /// <summary>
        /// 연출 방식을 바꾼다(M10.8-T5).
        ///
        /// 소프트 반그림자와 SSAO는 둘 다 연속 그라데이션이라 픽셀 격자에서 되살릴 방법이 없다.
        /// 에셋 기본값은 Baseline 값으로 두고 여기서만 바꿔, 되돌리면 원래 화면이 그대로 돌아온다.
        /// </summary>
        public void SetRenderStyle(RenderStyle style)
        {
            if (renderStyle == style) return;
            renderStyle = style;

            if (TryResolveKeyLight()) keyLight.shadows = ShadowsForCurrentStyle();

            if (style == RenderStyle.Cel) DisableAmbientOcclusion();
            else RestoreAmbientOcclusion();
        }

        private LightShadows ShadowsForCurrentStyle()
        {
            return renderStyle == RenderStyle.Cel ? LightShadows.Hard : LightShadows.Soft;
        }

        /// <summary>
        /// SSAO 렌더 피처를 끈다. 피처는 렌더러 에셋이 소유해 참조 경로가 없으므로 이미 로드된 것
        /// 중에서 찾는다. 끄기 전 상태를 기억해 두고 <see cref="RestoreAmbientOcclusion"/>에서 되돌린다.
        /// </summary>
        private void DisableAmbientOcclusion()
        {
            foreach (ScriptableRendererFeature feature in Resources.FindObjectsOfTypeAll<ScriptableRendererFeature>())
            {
                if (feature == null) continue;
                if (feature.GetType().Name != AmbientOcclusionFeatureTypeName) continue;
                if (ambientOcclusionOriginalStates.ContainsKey(feature)) continue;

                ambientOcclusionOriginalStates[feature] = feature.isActive;
                feature.SetActive(false);
            }
        }

        /// <summary>꺼 둔 SSAO 피처를 원래 상태로 되돌린다. 되돌리지 않으면 에셋에 그대로 남는다.</summary>
        private void RestoreAmbientOcclusion()
        {
            foreach (KeyValuePair<ScriptableRendererFeature, bool> entry in ambientOcclusionOriginalStates)
            {
                if (entry.Key == null) continue;
                entry.Key.SetActive(entry.Value);
            }
            ambientOcclusionOriginalStates.Clear();
        }

        /// <summary>플레이 모드를 나가거나 오브젝트가 꺼질 때도 반드시 되돌린다.</summary>
        private void OnDisable()
        {
            RestoreAmbientOcclusion();
        }

        public void TogglePreset()
        {
            if (presets.Length == 0) return;

            currentPresetIndex = (currentPresetIndex + 1) % presets.Length;
            ApplyCurrentPreset();
        }

        public void ApplyCurrentPreset()
        {
            if (presets.Length == 0) return;

            KeyLightPreset preset = presets[currentPresetIndex];
            float fillIntensity = preset.Intensity * fillLightShare;

            if (TryResolveKeyLight())
            {
                keyLight.color = preset.Color;
                keyLight.intensity = preset.Intensity - fillIntensity;
            }

            if (TryResolveFillLight())
            {
                fillLight.color = preset.Color;
                fillLight.intensity = fillIntensity;
            }

            PresetChanged?.Invoke(preset.Name);
        }

        private bool TryResolveKeyLight()
        {
            if (keyLight != null) return true;

            GameObject found = GameObject.Find(KeyLightName);
            keyLight = found != null ? found.GetComponent<Light>() : null;
            return keyLight != null;
        }

        private bool TryResolveFillLight()
        {
            if (fillLight != null) return true;

            GameObject found = GameObject.Find(FillLightName);
            fillLight = found != null ? found.GetComponent<Light>() : null;
            return fillLight != null;
        }
    }
}
