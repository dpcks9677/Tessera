using UnityEngine;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 흰색 + 알파 마스크로 둥근 사각형 모양만 만드는 절차 스프라이트다. 색은 쓰는 쪽이
    /// Image.color로 입히므로, 버튼 테두리·fill·후광처럼 서로 다른 색을 쓰는 여러 Image가
    /// 스프라이트 하나를 공유할 수 있다.
    ///
    /// AugmentStickerTexture와 같은 구조다 — 픽셀 배열을 만드는 순수 정적 메서드를 분리해
    /// 텍스처 없이도 모양을 검증할 수 있게 했다. 빌트인 UI 스킨 스프라이트(UI/Skin/UISprite.psd)는
    /// AssetDatabase.GetBuiltinExtraResource 전용이라 에디터 밖에서는 열리지 않아 이 방식으로 대체했다.
    /// </summary>
    public static class RoundedRectSprite
    {
        public const int DefaultSize = 32;
        public const float DefaultRadius = 10f;

        /// <summary>
        /// 9-slice 테두리(텍셀 수). 반경(10)보다 1px 크게 잡아, 버튼이 늘어날 때 가운데 스트레치
        /// 구역에 곡선이 섞이지 않게 한다.
        /// </summary>
        public static readonly Vector4 DefaultBorder = new(11f, 11f, 11f, 11f);

        /// <summary>
        /// Canvas.referencePixelsPerUnit 기본값과 같게 둔다. Sliced Image는 테두리를
        /// sprite.pixelsPerUnit / referencePixelsPerUnit 비율로 환산하는데, 1로 두면 테두리가 100배로
        /// 부풀어 버튼 전체를 덮고 32px 텍스처가 통째로 늘어나 모서리가 타원처럼 번진다.
        /// </summary>
        public const float PixelsPerUnit = 100f;

        private static Sprite cachedSprite;

        /// <summary>기본 크기·반경·테두리로 구운 스프라이트를 한 번만 만들어 공유한다.</summary>
        public static Sprite Shared => cachedSprite != null
            ? cachedSprite
            : cachedSprite = CreateSprite(DefaultSize, DefaultSize, DefaultRadius, DefaultBorder);

        public static Texture2D CreateTexture(int width, int height, float radius)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Rounded Rect Sprite",
                // 픽셀 아트인 AugmentStickerTexture와 달리 이쪽은 곡선 경계라 Point가 아니라 Bilinear를 쓴다.
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(CreatePixels(width, height, radius));
            texture.Apply(false);
            return texture;
        }

        public static Sprite CreateSprite(int width, int height, float radius, Vector4 border)
        {
            Texture2D texture = CreateTexture(width, height, radius);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: PixelsPerUnit,
                extrude: 0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>
        /// 픽셀 배열을 만든다. 텍스처 없이도 검증할 수 있도록 분리했다. 같은 인자면 항상 같은
        /// 결과가 나온다. Inigo Quilez의 둥근 사각형 SDF로 경계 픽셀에 부분 알파를 줘 안티에일리어싱한다.
        /// </summary>
        public static Color32[] CreatePixels(int width, int height, float radius)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(width, height) * 0.5f);

            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float py = (y + 0.5f) - halfHeight;
                for (int x = 0; x < width; x++)
                {
                    float px = (x + 0.5f) - halfWidth;
                    float distance = RoundedBoxDistance(px, py, halfWidth, halfHeight, radius);
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - distance) * 255f);
                    pixels[(y * width) + x] = new Color32(255, 255, 255, alpha);
                }
            }
            return pixels;
        }

        /// <summary>
        /// Inigo Quilez의 둥근 사각형 SDF다. 중심 기준 좌표에서 경계까지 거리를 반환한다(경계는 0,
        /// 안쪽은 음수). 사각형 안쪽 직선 구간은 각지게 남고, 네 모서리만 반경만큼 둥글게 깎인다.
        /// </summary>
        private static float RoundedBoxDistance(float px, float py, float halfWidth, float halfHeight, float radius)
        {
            float qx = Mathf.Abs(px) - halfWidth + radius;
            float qy = Mathf.Abs(py) - halfHeight + radius;
            float outsideX = Mathf.Max(qx, 0f);
            float outsideY = Mathf.Max(qy, 0f);
            float outsideDistance = Mathf.Sqrt((outsideX * outsideX) + (outsideY * outsideY));
            float insideDistance = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outsideDistance + insideDistance - radius;
        }
    }
}
