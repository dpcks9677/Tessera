using Tessera.Rendering;
using UnityEngine;

namespace Tessera.Games.Yacht.Presentation
{
    /// <summary>
    /// 증강 스크롤 양피지 텍스처의 크기와 타일링을 픽셀 필터 텍셀 밀도에 맞춘다(PIXEL-T9).
    ///
    /// 스크롤은 트레이 카드 슬롯 위에 눕혀 놓이는 정지 평면이다. 본문 메시의 월드 크기(메시 로컬 크기 × 프리셋 스케일 ×
    /// 트레이 스케일)에서 텍스처 한 장이 덮는 월드 크기를 역산하면 텍셀 하나가 필터 한 칸과 일치한다.
    /// 본문 메시 UV의 u 배율(<see cref="BodyUvScaleU"/>)과 머티리얼 타일링의 옛 값(<see cref="LegacyTilingX"/>,
    /// <see cref="LegacyTilingY"/>)이 텍스처 한 장이 덮는 범위를 정하고 있어, 그 범위를 유지한 채 텍셀 수만 격자에
    /// 맞추고 타일링으로 남는 오차를 흡수한다.
    /// </summary>
    public static class AugmentScrollTexelMapping
    {
        // AugmentScrollModelFactory 본문 메시 UV의 u 배율(uvs.Add(new Vector2(u * 1.15f, v))).
        public const float BodyUvScaleU = 1.15f;
        public const float BodyUvScaleV = 1.0f;

        // 정렬 이전 머티리얼 타일링. 텍스처 한 장이 덮는 범위를 정하는 기준으로만 쓴다.
        public const float LegacyTilingX = 1.10f;
        public const float LegacyTilingY = 0.92f;

        /// <summary>본문 메시 로컬 크기와 월드 스케일에서 본문의 월드 크기(x=u축 폭, y=v축 깊이)를 구한다.</summary>
        public static Vector2 BodyWorldSize(Vector3 meshLocalSize, Vector3 lossyScale)
        {
            return new Vector2(
                meshLocalSize.x * Mathf.Abs(lossyScale.x),
                meshLocalSize.z * Mathf.Abs(lossyScale.z));
        }

        /// <summary>텍스처 한 장(1회 반복)이 덮는 월드 크기.</summary>
        public static Vector2 TextureWorldSize(Vector2 bodyWorldSize)
        {
            return new Vector2(
                bodyWorldSize.x / (BodyUvScaleU * LegacyTilingX),
                bodyWorldSize.y / (BodyUvScaleV * LegacyTilingY));
        }

        /// <summary>본문 월드 크기 기준 텍스처 해상도. 텍셀 수는 바닥 텍셀 밀도로 내림한다.</summary>
        public static Vector2Int TextureSize(Vector2 bodyWorldSize)
        {
            return PixelTexelDensity.TexelCountFor(TextureWorldSize(bodyWorldSize), PixelSurfaceFacing.Floor);
        }

        /// <summary>텍스처 텍셀 하나가 필터 한 칸의 월드 크기가 되도록 하는 정확한 타일링.</summary>
        public static Vector2 Tiling(Vector2 bodyWorldSize, int textureWidth, int textureHeight)
        {
            Vector2 texel = PixelTexelDensity.TexelWorldSize(PixelSurfaceFacing.Floor);
            return new Vector2(
                bodyWorldSize.x / (BodyUvScaleU * textureWidth * texel.x),
                bodyWorldSize.y / (BodyUvScaleV * textureHeight * texel.y));
        }
    }
}
