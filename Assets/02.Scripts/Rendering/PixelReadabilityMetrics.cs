using System.Collections.Generic;
using UnityEngine;

namespace Tessera.Rendering
{
    /// <summary>
    /// 픽셀아트로 읽히는지를 재는 지표(M10.8-T8).
    ///
    /// M10.6은 "고유 색 수"로 판단했다가 616색에서 30색까지 줄이고도 화면이 3D로 읽히는 것을 놓쳤다.
    /// 색 수는 결과일 뿐이고, 실제로 읽히는 것을 결정하는 것은 평면 영역의 크기와 경계의 두께다.
    /// 그래서 여기서는 세 가지를 잰다.
    ///
    /// - 밝기 밴드 수: 계조가 몇 단계로 끊겼는가
    /// - 최대 동일색 연결 영역 비율: 평면이 실제로 넓은가
    /// - 프레임 간 변화 셀 비율: 움직일 때 화면이 얼마나 들끓는가(픽셀 크롤)
    ///
    /// 순수 함수로 두어 합성 입력으로 검증할 수 있게 한다. 캡처와 판정은 에디터 도구가 맡는다.
    /// </summary>
    public static class PixelReadabilityMetrics
    {
        /// <summary>밝기를 이 폭으로 묶어 밴드로 센다. 8비트 한 칸 차이를 다른 밴드로 세지 않기 위한 것이다.</summary>
        public const int DefaultBandTolerance = 6;

        public static float Luminance(Color32 color)
        {
            return (0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b) / 255f;
        }

        /// <summary>
        /// 밝기 밴드 수. 인접한 밝기를 <paramref name="tolerance"/> 폭으로 묶는다.
        /// 셀 셰이딩이 제대로 걸리면 주사위 몸체 크롭에서 3(금속 4) 이하가 나와야 한다.
        /// </summary>
        public static int CountLuminanceBands(IReadOnlyList<Color32> pixels, int tolerance = DefaultBandTolerance)
        {
            if (pixels == null || pixels.Count == 0) return 0;

            int width = Mathf.Max(1, tolerance);
            var buckets = new HashSet<int>();
            for (int index = 0; index < pixels.Count; index++)
            {
                int level = Mathf.RoundToInt(Luminance(pixels[index]) * 255f);
                buckets.Add(level / width);
            }
            return buckets.Count;
        }

        /// <summary>
        /// 가장 큰 동일색 연결 영역이 전체에서 차지하는 비율. 평면이 넓을수록 픽셀아트로 읽힌다.
        /// 4방향 연결로 센다. 대각 연결까지 세면 디더 무늬가 하나의 큰 영역으로 잡힌다.
        /// </summary>
        public static float LargestUniformRegionRatio(Color32[] pixels, int width, int height)
        {
            if (pixels == null || width <= 0 || height <= 0 || pixels.Length < width * height) return 0f;

            bool[] visited = new bool[width * height];
            var frontier = new Stack<int>();
            int largest = 0;

            for (int start = 0; start < width * height; start++)
            {
                if (visited[start]) continue;

                Color32 target = pixels[start];
                int size = 0;

                frontier.Push(start);
                visited[start] = true;

                while (frontier.Count > 0)
                {
                    int current = frontier.Pop();
                    size++;

                    int x = current % width;
                    int y = current / width;

                    if (x > 0) PushIfSame(pixels, visited, frontier, current - 1, target);
                    if (x < width - 1) PushIfSame(pixels, visited, frontier, current + 1, target);
                    if (y > 0) PushIfSame(pixels, visited, frontier, current - width, target);
                    if (y < height - 1) PushIfSame(pixels, visited, frontier, current + width, target);
                }

                if (size > largest) largest = size;
            }

            return largest / (float)(width * height);
        }

        /// <summary>
        /// 두 프레임 사이에 색이 바뀐 셀 비율. 굴림 중 이 값이 크면 실루엣과 디테일이 들끓는다.
        /// 같은 크기의 두 프레임을 넣어야 한다.
        /// </summary>
        public static float ChangedCellRatio(Color32[] previous, Color32[] current)
        {
            if (previous == null || current == null) return 0f;
            if (previous.Length == 0 || previous.Length != current.Length) return 0f;

            int changed = 0;
            for (int index = 0; index < previous.Length; index++)
            {
                Color32 a = previous[index];
                Color32 b = current[index];
                if (a.r != b.r || a.g != b.g || a.b != b.b) changed++;
            }
            return changed / (float)previous.Length;
        }

        private static void PushIfSame(Color32[] pixels, bool[] visited, Stack<int> frontier, int index, Color32 target)
        {
            if (visited[index]) return;

            Color32 candidate = pixels[index];
            if (candidate.r != target.r || candidate.g != target.g || candidate.b != target.b) return;

            visited[index] = true;
            frontier.Push(index);
        }
    }
}
