using UnityEngine;

namespace Tessera.Rendering
{
    /// <summary>
    /// 월드 좌표를 픽셀 필터 격자의 칸 중심으로 옮기는 공용 스냅.
    /// </summary>
    public static class PixelGridSnap
    {
        public static Vector3 ToCellCenter(Camera camera, Vector2Int resolution, Vector3 world)
        {
            if (camera == null || resolution.x < 1 || resolution.y < 1) return world;

            Vector3 viewport = camera.WorldToViewportPoint(world);
            // 카메라 뒤로 넘어간 지점은 뷰포트 좌표가 뒤집혀 스냅이 엉뚱한 자리를 만든다.
            if (viewport.z <= 0f) return world;

            viewport.x = (Mathf.Floor(viewport.x * resolution.x) + 0.5f) / resolution.x;
            viewport.y = (Mathf.Floor(viewport.y * resolution.y) + 0.5f) / resolution.y;
            return camera.ViewportToWorldPoint(viewport);
        }
    }
}
