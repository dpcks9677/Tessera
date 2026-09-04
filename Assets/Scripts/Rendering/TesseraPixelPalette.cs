using UnityEngine;

namespace Tessera.Rendering
{
    /// <summary>
    /// 픽셀 필터의 색 양자화가 쓸 팔레트를 만든다(M10.6-T2).
    ///
    /// 색은 <c>docs/art_style_guide.md</c> §4의 컬러 토큰을 단일 출처로 삼는다.
    /// 토큰마다 명암 램프를 만들어 팔레트를 채우므로, 아트 방향이 바뀌어 토큰 값을 고치면
    /// 화면 팔레트도 함께 따라간다.
    ///
    /// 값은 sRGB다. 셰이더도 sRGB 공간에서 비교하므로 변환 없이 그대로 넘긴다.
    /// </summary>
    public static class TesseraPixelPalette
    {
        /// <summary>셰이더의 <c>_PaletteColors</c> 배열 크기와 같아야 한다.</summary>
        public const int MaxColors = 32;

        /// <summary>아트 가이드 §4 컬러 토큰. 주석의 이름이 문서의 토큰 명칭이다.</summary>
        private static readonly Color[] Tokens =
        {
            new Color32(0xff, 0x9e, 0x3b, 0xff), // color-light-warm-key
            new Color32(0x36, 0x4b, 0x6e, 0xff), // color-light-cool-rim
            new Color32(0x6e, 0x43, 0x2a, 0xff), // color-table-wood-main
            new Color32(0x82, 0x50, 0x33, 0xff), // color-table-wood-tint
            new Color32(0x88, 0x2d, 0x22, 0xff), // color-runner-crimson
            new Color32(0xe5, 0xa9, 0x3c, 0xff), // color-runner-gold
            new Color32(0x14, 0x0f, 0x0c, 0xff), // color-shadow-underlay
            new Color32(0x0f, 0x0c, 0x10, 0xff)  // color-scene-background
        };

        /// <summary>
        /// 명도 배율. 어둡게만 내리면 이미 어두운 토큰의 램프가 검정으로 뭉치므로
        /// 밝은 쪽으로도 한 칸 올린다. 배율 1을 넘은 명도는 1로 잘린다.
        /// </summary>
        private static readonly float[] ValueScales = { 0.35f, 0.65f, 1.0f, 1.45f };

        /// <summary>토큰마다 명암 램프를 펼친 팔레트를 만든다. 길이는 항상 토큰 수 × 램프 단계 수다.</summary>
        public static Color[] Build()
        {
            Color[] palette = new Color[Tokens.Length * ValueScales.Length];
            int index = 0;
            foreach (Color token in Tokens)
            {
                Color.RGBToHSV(token, out float hue, out float saturation, out float value);
                foreach (float scale in ValueScales)
                {
                    // 명도가 1을 넘으면 그만큼 채도를 낮춰 하이라이트로 뺀다. 그냥 자르면
                    // 이미 명도가 1인 토큰의 위쪽 두 칸이 같은 색이 되어 팔레트 칸을 낭비한다.
                    float scaled = value * scale;
                    float rampValue = Mathf.Min(1f, scaled);
                    float rampSaturation = scaled > 1f ? saturation / scaled : saturation;
                    palette[index++] = Color.HSVToRGB(hue, rampSaturation, rampValue);
                }
            }
            return palette;
        }

        /// <summary>셰이더의 고정 크기 배열에 그대로 넣을 수 있도록 <see cref="MaxColors"/>만큼 채운다.</summary>
        public static Vector4[] BuildShaderArray(out int count)
        {
            Color[] palette = Build();
            count = Mathf.Min(palette.Length, MaxColors);

            Vector4[] values = new Vector4[MaxColors];
            for (int i = 0; i < MaxColors; i++)
            {
                Color color = palette[Mathf.Min(i, count - 1)];
                values[i] = new Vector4(color.r, color.g, color.b, 1f);
            }
            return values;
        }
    }
}
