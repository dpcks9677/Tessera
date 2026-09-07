using UnityEngine;

namespace Tessera.Rendering
{
    /// <summary>
    /// 픽셀 필터의 내부 해상도 프리셋과 게임 시작값을 담는 단일 출처.
    ///
    /// 같은 값이 컨트롤러(프리셋 두 개), 카메라 리그(시작값), 양피지 비주얼(기본값), 그리고 씬에 구워진
    /// 업스케일 재질까지 네 곳에 흩어져 있었다. 그래서 시작값을 480x270으로 바꾼 뒤에도 씬 재질은
    /// 640x360으로 남아, 플레이를 누르기 전 에디터 프리뷰가 실제 게임 시작 화면과 달랐다.
    /// 값을 여기로 모으고 씬 쪽은 <c>PixelFilterPreview</c> 에디터 도구가 맞춘다.
    /// </summary>
    public static class PixelFilterSettings
    {
        /// <summary>
        /// 1920x1080 렌더 타깃 안에서 격자를 스냅하므로 1920을 정수로 나누는 값만 쓴다.
        /// 나누어떨어지지 않으면 어떤 칸은 3화면픽셀, 어떤 칸은 4화면픽셀이 되어 격자가 무너진다.
        /// </summary>
        public static readonly Vector2Int ResolutionA = new(640, 360);  // 3x3 블록

        /// <inheritdoc cref="ResolutionA"/>
        public static readonly Vector2Int ResolutionB = new(480, 270);  // 4x4 블록

        /// <summary>게임이 시작할 때의 내부 해상도. 더 굵은 쪽으로 시작한다.</summary>
        public static Vector2Int StartResolution => ResolutionB;

        /// <summary>게임이 시작할 때의 색 양자화 모드. 0은 끔이다.</summary>
        public const int StartQuantizeMode = 0;

        /// <summary>게임이 시작할 때의 연출 방식. 채택 전까지 기준선이 기본이다.</summary>
        public const RenderStyle StartRenderStyle = RenderStyle.Baseline;

        /// <summary>두 프리셋을 번갈아 준다. 지금 값이 어느 쪽이든 나머지 하나를 돌려준다.</summary>
        public static Vector2Int NextPreset(Vector2Int current)
        {
            return current == ResolutionA ? ResolutionB : ResolutionA;
        }
    }
}
