using System;
using Tessera.Core;
using UnityEngine;
using UnityEngine.Rendering;

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

        [SerializeField]
        private KeyLightPreset[] presets =
        {
            new() { Name = "Pure White", Color = new Color(1.00f, 1.00f, 1.00f), Intensity = 1.25f },
            new() { Name = "Warm Amber", Color = new Color(1.00f, 0.62f, 0.23f), Intensity = 1.50f },
            new() { Name = "Soft Neutral", Color = new Color(1.00f, 0.88f, 0.74f), Intensity = 1.35f },
            new() { Name = "Cool Moon", Color = new Color(0.55f, 0.70f, 0.95f), Intensity = 1.35f },
            new() { Name = "Cozy Candle", Color = new Color(1.00f, 0.48f, 0.16f), Intensity = 1.55f }
        };

        /// <summary>Warm Amber를 기본으로 시작한다.</summary>
        [SerializeField] private int currentPresetIndex = 1;
        private Light keyLight;

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
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = shadowStrength;
            keyLight.shadowBias = shadowBias;
            keyLight.shadowNormalBias = shadowNormalBias;
            ApplyCurrentPreset();
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
            if (TryResolveKeyLight())
            {
                keyLight.color = preset.Color;
                keyLight.intensity = preset.Intensity;
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
    }
}
