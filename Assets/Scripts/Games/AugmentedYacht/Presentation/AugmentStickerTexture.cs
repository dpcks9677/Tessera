using UnityEngine;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 변형 증강이 점수표 Categories 칸을 통째로 덮는 우표 스티커의 바탕을 절차로 만듭니다.
    /// 증강마다 다른 것은 베이스 색과 테두리 색뿐이라 18종을 따로 그리지 않고 색만 바꿔 찍어 냅니다.
    /// 아이콘과 이름은 이 바탕 위에 원래 칸과 같은 배치로 따로 올립니다.
    /// 시각 사양은 <c>docs/augmented_yacht_m17_vfx_spec.md</c> §3.1.1을 따릅니다.
    /// </summary>
    public static class AugmentStickerTexture
    {
        public const int DefaultSize = 24;

        /// <summary>기본 베이스입니다. 아트 가이드의 러너 버건디(#882d22)와 같습니다.</summary>
        public static readonly Color32 DefaultBase = new(0x88, 0x2d, 0x22, 0xff);

        /// <summary>기본 내부 테두리입니다. 아트 가이드의 앤틱 골드(#e5a93c)와 같습니다.</summary>
        public static readonly Color32 DefaultBorder = new(0xe5, 0xa9, 0x3c, 0xff);

        private static readonly Color32 Transparent = new(0, 0, 0, 0);

        /// <summary>
        /// 톱니가 반복되는 간격입니다. 짧은 변을 기준으로 잡아 칸이 가로로 길어도 톱니 크기는 같습니다.
        /// </summary>
        private static int NotchPeriod(int width, int height) =>
            Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(width, height) / 6f), 3, 10);

        /// <summary>
        /// 우표 톱니 하나의 반지름입니다. 간격의 31%로 잡아 톱니와 몸통이 엇비슷해집니다.
        /// 이보다 크면 몸통이 남지 않아 우표가 아니라 톱니바퀴로 보입니다.
        /// </summary>
        private static float NotchRadius(int width, int height) => NotchPeriod(width, height) * 0.31f;

        /// <summary>가장자리에서 내부 테두리선까지의 거리입니다. 톱니가 파고드는 깊이보다 한 픽셀 안쪽입니다.</summary>
        private static int BorderInset(int width, int height) => Mathf.CeilToInt(NotchRadius(width, height)) + 1;

        public static Texture2D Create(int size = DefaultSize) => Create(DefaultBase, DefaultBorder, size, size);

        public static Texture2D Create(Color32 baseColor, Color32 borderColor, int width, int height)
        {
            width = Mathf.Max(8, width);
            height = Mathf.Max(8, height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Augment Sticker",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(CreatePixels(baseColor, borderColor, width, height));
            texture.Apply(false);
            return texture;
        }

        public static Sprite CreateSprite(Color32 baseColor, Color32 borderColor, int width, int height)
        {
            Texture2D texture = Create(baseColor, borderColor, width, height);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f);
            sprite.name = texture.name;
            return sprite;
        }

        /// <summary>
        /// 픽셀 배열을 만듭니다. 텍스처 없이도 검증할 수 있도록 분리했습니다.
        /// 같은 인자면 항상 같은 결과가 나옵니다.
        /// </summary>
        public static Color32[] CreatePixels(Color32 baseColor, Color32 borderColor, int width, int height)
        {
            width = Mathf.Max(8, width);
            height = Mathf.Max(8, height);
            float radius = NotchRadius(width, height);
            int inset = BorderInset(width, height);
            Color32 rimColor = Darken(baseColor, 0.62f);
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[(y * width) + x] = IsPerforated(x, y, width, height, radius) ? Transparent : baseColor;
                }
            }

            // 톱니로 뚫린 자리와 맞닿은 몸통 픽셀을 어둡게 해 양피지 위에서 실루엣이 읽히게 한다.
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width) + x;
                    if (pixels[index].a == 0) continue;
                    if (IsBodyEdge(pixels, x, y, width, height)) pixels[index] = rimColor;
                }
            }

            // 내부 금색 테두리선 한 겹.
            int farX = width - 1 - inset;
            int farY = height - 1 - inset;
            for (int y = inset; y <= farY; y++)
            {
                for (int x = inset; x <= farX; x++)
                {
                    bool onLine = x == inset || x == farX || y == inset || y == farY;
                    if (onLine) pixels[(y * width) + x] = borderColor;
                }
            }

            return pixels;
        }

        /// <summary>우표 톱니에 해당해 뚫려야 하는 자리인지 판정합니다.</summary>
        private static bool IsPerforated(int x, int y, int width, int height, float radius)
        {
            int period = NotchPeriod(width, height);
            float px = x + 0.5f;
            float py = y + 0.5f;

            int horizontal = Mathf.Max(1, width / period);
            for (int i = 0; i < horizontal; i++)
            {
                float center = (i + 0.5f) * period;
                if (Inside(px, py, center, 0f, radius)) return true;        // 아래
                if (Inside(px, py, center, height, radius)) return true;    // 위
            }

            int vertical = Mathf.Max(1, height / period);
            for (int i = 0; i < vertical; i++)
            {
                float center = (i + 0.5f) * period;
                if (Inside(px, py, 0f, center, radius)) return true;        // 왼쪽
                if (Inside(px, py, width, center, radius)) return true;     // 오른쪽
            }

            return false;
        }

        private static bool Inside(float px, float py, float cx, float cy, float radius)
        {
            float dx = px - cx;
            float dy = py - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        /// <summary>투명한 이웃이 있거나 텍스처 경계에 닿은 몸통 픽셀인지 봅니다.</summary>
        private static bool IsBodyEdge(Color32[] pixels, int x, int y, int width, int height)
        {
            if (x == 0 || y == 0 || x == width - 1 || y == height - 1) return true;
            return pixels[(y * width) + (x - 1)].a == 0
                || pixels[(y * width) + (x + 1)].a == 0
                || pixels[((y - 1) * width) + x].a == 0
                || pixels[((y + 1) * width) + x].a == 0;
        }

        private static Color32 Darken(Color32 color, float factor) => new(
            (byte)Mathf.RoundToInt(color.r * factor),
            (byte)Mathf.RoundToInt(color.g * factor),
            (byte)Mathf.RoundToInt(color.b * factor),
            color.a);
    }
}
