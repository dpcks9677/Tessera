using UnityEngine;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 변형 증강이 점수표 칸을 덮는 러너 스티커의 바탕을 절차로 만듭니다.
    /// 증강마다 다른 것은 베이스 색과 테두리 색뿐이라 18종을 따로 그리지 않고 색만 바꿔 찍어 냅니다.
    /// 아이콘과 이름은 이 바탕 위에 원래 칸과 같은 배치로 따로 올립니다.
    /// 시각 사양은 <c>docs/augmented_yacht_m17_vfx_spec.md</c> §3.1.1을 따릅니다.
    ///
    /// 한 텍셀이 픽셀 필터 화면의 한 픽셀이라는 전제로 그립니다. 그래서 금테와 그림자를 텍셀 단위로
    /// 고정해도 화면에서 그 두께 그대로 나옵니다. 굽는 크기는 <c>ParchmentScoreSheet</c>가 정합니다.
    /// </summary>
    public static class AugmentStickerTexture
    {
        public const int DefaultSize = 24;

        /// <summary>기본 베이스입니다. 아트 가이드의 러너 버건디(#882d22)와 같습니다.</summary>
        public static readonly Color32 DefaultBase = new(0x88, 0x2d, 0x22, 0xff);

        /// <summary>기본 내부 테두리입니다. 아트 가이드의 앤틱 골드(#e5a93c)와 같습니다.</summary>
        public static readonly Color32 DefaultBorder = new(0xe5, 0xa9, 0x3c, 0xff);

        /// <summary>
        /// 양피지 위에 지는 그림자입니다. 증강 색과 무관하게 늘 같습니다. 그림자는 천이 아니라
        /// 종이 쪽에서 나오는 것이고, 재질이 알파 컷아웃이라 반투명을 쓸 수 없어 불투명 한 겹으로 찍습니다.
        /// </summary>
        public static readonly Color32 DefaultShadow = new(0x5c, 0x47, 0x33, 0xff);

        private static readonly Color32 Transparent = new(0, 0, 0, 0);

        /// <summary>
        /// 그림자가 몸통에서 밀려나는 텍셀 수입니다. 화면에서 오른쪽·아래로 보입니다.
        ///
        /// 텍스처에서는 양쪽 다 좌표가 줄어드는 방향으로 밉니다. 천은 큐브의 윗면이고 그 면은
        /// 텍스처 u가 종이 −X(화면 왼쪽) 쪽으로 늘어나므로, 화면 오른쪽에 그림자를 보이려면
        /// 텍스처에서는 왼쪽으로 밀어야 합니다. v는 화면 아래와 같은 방향이라 그대로 줄입니다.
        /// </summary>
        public const int ShadowOffset = 1;

        /// <summary>몸통 가장자리에서 금테까지의 텍셀 수입니다.</summary>
        public const int BorderInset = 1;

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
        ///
        /// 세 겹입니다. 오른쪽·아래로 한 칸 밀린 그림자, 그 위를 덮는 몸통, 몸통 가장자리에서
        /// <see cref="BorderInset"/>만큼 안쪽에 놓이는 금테 한 줄입니다.
        /// </summary>
        public static Color32[] CreatePixels(Color32 baseColor, Color32 borderColor, int width, int height)
        {
            width = Mathf.Max(8, width);
            height = Mathf.Max(8, height);

            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Transparent;

            // 몸통은 텍스처 왼쪽 한 칸과 아래 한 칸을 그림자 자리로 내준다(화면에서는 오른쪽·아래다).
            int bodyX0 = ShadowOffset;
            int bodyX1 = width - 1;
            int bodyY0 = ShadowOffset;
            int bodyY1 = height - 1;

            // 그림자를 먼저 깔고 몸통으로 덮는다. 겹치는 자리는 몸통이 이긴다.
            Fill(pixels, width, bodyX0 - ShadowOffset, bodyY0 - ShadowOffset, bodyX1 - ShadowOffset, bodyY1 - ShadowOffset, DefaultShadow);
            Fill(pixels, width, bodyX0, bodyY0, bodyX1, bodyY1, baseColor);

            // 금테 한 겹. 몸통 가장자리에서 텍셀 수로 재므로 어느 칸에서나 같은 자리에 온다.
            int lineX0 = bodyX0 + BorderInset;
            int lineX1 = bodyX1 - BorderInset;
            int lineY0 = bodyY0 + BorderInset;
            int lineY1 = bodyY1 - BorderInset;

            for (int x = lineX0; x <= lineX1; x++)
            {
                pixels[(lineY0 * width) + x] = borderColor;
                pixels[(lineY1 * width) + x] = borderColor;
            }
            for (int y = lineY0; y <= lineY1; y++)
            {
                pixels[(y * width) + lineX0] = borderColor;
                pixels[(y * width) + lineX1] = borderColor;
            }

            return pixels;
        }

        private static void Fill(Color32[] pixels, int width, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++) pixels[(y * width) + x] = color;
            }
        }
    }
}
