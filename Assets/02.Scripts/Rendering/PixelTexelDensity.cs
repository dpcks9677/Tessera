using UnityEngine;

namespace Tessera.Rendering
{
    /// <summary>텍스처가 붙는 면이 카메라를 향한 방향입니다.</summary>
    public enum PixelSurfaceFacing
    {
        /// <summary>시선에 수직인 면입니다.</summary>
        FacingCamera,

        /// <summary>수평으로 누운 면입니다. 세로축은 월드 Z입니다.</summary>
        Floor,

        /// <summary>카메라 쪽을 향해 선 면입니다. 세로축은 월드 Y입니다.</summary>
        Wall,
    }

    /// <summary>
    /// 텍셀 하나를 픽셀 필터 격자 한 칸에 맞추는 공용 규칙입니다(D-050).
    /// 면 방향에 따라 격자 한 칸이 면 위에서 덮는 월드 길이가 다르므로, 텍셀 수와 크기를 여기서 한 번에 계산합니다.
    /// 가로축은 모든 면에서 월드 X입니다.
    /// </summary>
    public static class PixelTexelDensity
    {
        /// <summary>
        /// 월드 스페이스 캔버스는 1캔버스 단위를 0.01월드로 쓰는 프로젝트 관례입니다.
        /// 캔버스 그래픽의 칸 크기를 캔버스 단위로 계산할 때 씁니다.
        /// </summary>
        public const float CanvasUnitsPerWorldUnit = 100f;

        /// <summary>면 위 월드 길이 1이 화면에서 차지하는 비율입니다.</summary>
        private static Vector2 ScreenScale(PixelSurfaceFacing facing)
        {
            float pitch = PixelFilterSettings.WorldCameraPitch * Mathf.Deg2Rad;
            return facing switch
            {
                PixelSurfaceFacing.Floor => new Vector2(1f, Mathf.Sin(pitch)),
                PixelSurfaceFacing.Wall => new Vector2(1f, Mathf.Cos(pitch)),
                _ => Vector2.one,
            };
        }

        /// <summary>텍셀 하나(= 필터 한 칸)가 그 면 위에서 차지하는 월드 크기입니다.</summary>
        public static Vector2 TexelWorldSize(PixelSurfaceFacing facing)
        {
            Vector2 scale = ScreenScale(facing);
            return new Vector2(PixelFilterSettings.CellWorldSize / scale.x, PixelFilterSettings.CellWorldSize / scale.y);
        }

        /// <summary>
        /// 월드 크기의 면에 굽을 텍셀 수를 구합니다. 텍셀이 한 칸 이상이 되도록 내림합니다.
        /// 텍셀이 칸보다 작으면 칸 중심 샘플이 텍셀을 건너뛸 수 있습니다. 반대로 크면 가끔 두 칸으로
        /// 찍힐 뿐 사라지지 않습니다.
        /// </summary>
        public static Vector2Int TexelCountFor(Vector2 worldSize, PixelSurfaceFacing facing)
        {
            Vector2 scale = ScreenScale(facing);
            return new Vector2Int(
                Mathf.Max(1, Mathf.FloorToInt(worldSize.x * scale.x / PixelFilterSettings.CellWorldSize)),
                Mathf.Max(1, Mathf.FloorToInt(worldSize.y * scale.y / PixelFilterSettings.CellWorldSize)));
        }
    }
}
