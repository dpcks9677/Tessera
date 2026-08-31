using System.Collections.Generic;
using UnityEngine;

namespace Tessera.Games.AugmentedYacht
{
    public enum AugmentParchmentPreset
    {
        GentleWave,
        TopTear,
        BottomTear,
        WornCorners
    }

    /// <summary>선택 카드와 트레이 카드가 공유하는 직사각형 양피지 프리셋을 제공합니다.</summary>
    public static class AugmentParchmentVisuals
    {
        public const int PresetCount = 4;
        public const float DecorationGutterNormalized = .22f;
        public static readonly Rect ContentSafeRect = new(.24f, .08f, .71f, .84f);

        private const int TextureWidth = 512;
        private const int TextureHeight = 288;
        private static readonly Dictionary<int, Sprite> FallbackSprites = new();
        private static readonly Dictionary<(int Preset, int Width, int Height), Sprite> PixelFilteredSprites = new();

        /// <summary>월드 픽셀 필터가 사용하는 내부 해상도입니다. 해상도 전환 시 컨트롤러가 갱신합니다.</summary>
        public static Vector2Int PixelFilterResolution { get; set; } = new(640, 360);

        private static readonly float[][] EdgeProfiles =
        {
            new[] { .004f, .008f, .005f, .010f, .004f, .007f, .005f, .009f },
            new[] { .005f, .009f, .006f, .014f, .005f, .008f, .011f, .005f },
            new[] { .006f, .004f, .012f, .006f, .009f, .005f, .014f, .006f },
            new[] { .014f, .006f, .005f, .008f, .006f, .005f, .007f, .014f }
        };

        public static AugmentParchmentPreset Normalize(int value) =>
            (AugmentParchmentPreset)(value == 4 ? 3 : value >= 0 && value < PresetCount ? value : 0);

        public static string GetOutlineSignature(AugmentParchmentPreset preset) =>
            string.Join(",", EdgeProfiles[(int)Normalize((int)preset)]);

        public static float SampleEdgeInset(AugmentParchmentPreset preset, float normalized, bool top)
        {
            int key = (int)Normalize((int)preset);
            float[] profile = EdgeProfiles[key];
            float scaled = Mathf.Clamp01(normalized) * (profile.Length - 1);
            int index = Mathf.Min(Mathf.FloorToInt(scaled), profile.Length - 2);
            float inset = Mathf.Lerp(profile[index], profile[index + 1], scaled - index);
            float tear = key switch
            {
                1 when top => .012f * Mathf.Exp(-Mathf.Pow((normalized - .68f) / .035f, 2f)),
                2 when !top => .012f * Mathf.Exp(-Mathf.Pow((normalized - .43f) / .04f, 2f)),
                3 => .008f * (Mathf.Exp(-Mathf.Pow((normalized - .04f) / .04f, 2f))
                    + Mathf.Exp(-Mathf.Pow((normalized - .96f) / .04f, 2f))),
                _ => 0f
            };
            return Mathf.Min(.018f, inset + tear);
        }

        public static Sprite GetSprite(AugmentParchmentPreset preset, bool overlayContentOnly)
        {
            int key = (int)Normalize((int)preset);
            Sprite baked = Resources.Load<Sprite>($"AugmentScrolls/Previews/AugmentScrollPreview_{key}");
            if (baked != null) return baked;
            if (FallbackSprites.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Sprite fallback = CreateFallbackSprite((AugmentParchmentPreset)key);
            FallbackSprites[key] = fallback;
            return fallback;
        }

        /// <summary>카드 본체를 월드와 같은 픽셀 격자로 다운샘플한 스프라이트를 돌려줍니다.</summary>
        public static Sprite GetPixelFilteredSprite(AugmentParchmentPreset preset, Vector2 displaySize)
        {
            Sprite source = GetSprite(preset, false);
            if (source == null || source.texture == null) return source;
            if (displaySize.x < 1f || displaySize.y < 1f || Screen.width < 1 || Screen.height < 1) return source;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return source;

            int key = (int)Normalize((int)preset);
            int width = Mathf.Clamp(
                Mathf.RoundToInt(displaySize.x * PixelFilterResolution.x / Screen.width), 16, source.texture.width);
            int height = Mathf.Clamp(
                Mathf.RoundToInt(displaySize.y * PixelFilterResolution.y / Screen.height), 16, source.texture.height);

            if (PixelFilteredSprites.TryGetValue((key, width, height), out Sprite cached) && cached != null) return cached;
            Sprite reduced = CreateDownsampledSprite(source.texture, key, width, height);
            if (reduced == null) return source;
            PixelFilteredSprites[(key, width, height)] = reduced;
            return reduced;
        }

        private static Sprite CreateDownsampledSprite(Texture source, int key, int width, int height)
        {
            RenderTexture target = RenderTexture.GetTemporary(
                width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            target.filterMode = FilterMode.Point;
            FilterMode sourceFilter = source.filterMode;
            RenderTexture previous = RenderTexture.active;
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                name = $"Augment_Parchment_Pixel_{key}_{width}x{height}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                source.filterMode = FilterMode.Point;
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
            }
            finally
            {
                RenderTexture.active = previous;
                source.filterMode = sourceFilter;
                RenderTexture.ReleaseTemporary(target);
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 100f);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite CreateFallbackSprite(AugmentParchmentPreset preset)
        {
            int key = (int)preset;
            Color32[] pixels = new Color32[TextureWidth * TextureHeight];
            Color paperLight = new(.98f, .97f, .94f, 1f);
            Color paperDark = new(.86f, .82f, .76f, 1f);
            Color cyan = new(.37f, .86f, 1f, .78f);
            Color leather = new(.20f, .10f, .055f, 1f);
            Color wax = new(.54f, .10f, .09f, 1f);

            for (int y = 0; y < TextureHeight; y++)
            for (int x = 0; x < TextureWidth; x++)
            {
                float u = (float)x / (TextureWidth - 1);
                float v = (float)y / (TextureHeight - 1);
                float bottom = SampleEdgeInset(preset, u, false);
                float top = SampleEdgeInset(preset, u, true);
                float side = .006f + (key == 3 ? .009f * Mathf.Abs(v - .5f) * 2f : 0f);
                if (u < side || u > 1f - side || v < bottom || v > 1f - top) continue;

                float broadStain = .94f + .05f * Mathf.Sin(u * 5.2f + v * 3.1f + key * .8f);
                Color color = Color.Lerp(paperDark, paperLight, broadStain);

                bool innerBorder = (u > .235f && u < .95f && (Mathf.Abs(v - .075f) < .004f || Mathf.Abs(v - .925f) < .004f))
                    || (v > .075f && v < .925f && (Mathf.Abs(u - .235f) < .003f || Mathf.Abs(u - .95f) < .003f));
                if (innerBorder) color = Color.Lerp(color, cyan, .9f);

                if (u < .22f)
                {
                    float rollU = Mathf.Clamp01(u / .22f);
                    float layerWave = .5f + .5f * Mathf.Cos(rollU * Mathf.PI * 5f);
                    float pinch = .80f + .20f * Mathf.Abs(v - .5f) * 2f;
                    color = Color.Lerp(paperDark, paperLight, (.22f + layerWave * .70f) * pinch);
                    if (Mathf.Abs(v - .5f) < .055f) color = leather;
                    Vector2 sealDelta = new((u - .15f) / .052f, (v - .5f) / .092f);
                    if (sealDelta.sqrMagnitude < 1f) color = wax;
                    if (sealDelta.sqrMagnitude < .22f && Mathf.Abs(sealDelta.x) + Mathf.Abs(sealDelta.y) < .58f)
                        color = Color.Lerp(wax, new Color(.30f, .035f, .035f, 1f), .72f);
                }
                pixels[y * TextureWidth + x] = color;
            }

            Texture2D texture = new(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                name = $"Augment_Parchment_Fallback_{key}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, TextureWidth, TextureHeight), new Vector2(.5f, .5f), 100f);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
