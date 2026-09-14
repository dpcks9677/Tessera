using UnityEngine;

/// <summary>
/// 동전 면 선화(투명 배경 + 검은 선)를 픽셀 필터용 면 앨비도로 변환한다(M17-T23-3).
///
/// 원본 선화는 선 굵기가 도안 폭의 0.5~1.3%로 매우 가늘어, 출력 해상도(기본 64×64)로 그냥
/// 축소하면 선이 통째로 사라진다. 그래서 가는 선이 사라지는 대신 굵어져 붙는 쪽을 택해,
/// 원본 해상도에서 잉크를 먼저 팽창시킨 뒤 축소한다. 480×270 픽셀 필터 화면에서 동전 면은
/// 약 16칸이라 이 굵기 보정이 없으면 문양이 화면에서 완전히 지워진다.
///
/// 원본 해상도(최대 수백만 픽셀)에서 매 중간 픽셀마다 사각 영역의 잉크 유무를 판정해야 하므로,
/// 원본을 실제로 팽창(dilate)하지 않고 누적합 테이블(summed-area table)로 O(1) 영역 판정만
/// 수행한다. 팽창 자체는 훨씬 작은 중간 격자(outputSize × 4)에서 수행한다.
///
/// 마지막으로 문양 획을 파인 홈처럼 보이게 음각 음영을 입힌다. 홈에서 빛이 들어오는 쪽 벽은
/// 빛을 등지므로 그림자가 진다는 발상이다. 배경/잉크/그림자 세 색은 전부 같은 명도 램프
/// (0.35/0.65/1.0)에 맞춰져 있어 색 양자화에서 튀지 않는다.
/// </summary>
public static class CoinFaceTextureConverter
{
    public const int PixelPathSize = 64;
    public const int CrispPathSize = 256;
    public const int DefaultMinStrokeTexels = 1;
    public const float DefaultCoverageThreshold = 0.35f;
    public const float DesignRadiusRatio = 0.9f;
    public static readonly Color32 InkShadowColor = new(0x50, 0x3B, 0x15, 255); // runner-gold × 0.35
    public static readonly Color32 InkColor = new(0x95, 0x6E, 0x27, 255);       // runner-gold × 0.65
    public static readonly Color32 BackgroundColor = new(0xE5, 0xA9, 0x3C, 255); // runner-gold × 1.0

    // 중간 격자 배율. 값이 클수록 팽창·다운스케일 경계가 매끄럽지만 느려진다.
    private const int GridMultiplier = 4;

    /// <summary>
    /// 선화 픽셀을 면 앨비도 픽셀로 변환한다. source/출력 모두 Texture2D.GetPixels32 순서
    /// (행 아래→위)를 쓰며, 출력은 outputSize×outputSize 크기에 BackgroundColor/InkColor/
    /// InkShadowColor 세 값만 담는다. lightFrom은 격자 한 칸 단위 빛 방향(-1/0/1 성분)이며,
    /// (0,0)이면 그림자 없이 전부 InkColor로 칠한다.
    /// </summary>
    public static Color32[] Convert(Color32[] source, int sourceWidth, int sourceHeight, int outputSize,
        Vector2Int lightFrom,
        int minStrokeTexels = DefaultMinStrokeTexels, float coverageThreshold = DefaultCoverageThreshold)
    {
        Color32[] output = new Color32[outputSize * outputSize];
        for (int i = 0; i < output.Length; i++) output[i] = BackgroundColor;

        bool[] inkMask = BuildInkMask(source);
        if (!TryComputeBoundingBox(inkMask, sourceWidth, sourceHeight, out int minX, out int maxX, out int minY, out int maxY))
            return output; // 잉크가 없으면 전부 배경.

        int[] sat = BuildSummedAreaTable(inkMask, sourceWidth, sourceHeight);

        int gridSize = outputSize * GridMultiplier;
        int dilateRadius = Mathf.CeilToInt((minStrokeTexels * GridMultiplier - 1) / 2f);

        float bboxWidth = maxX - minX + 1;
        float bboxHeight = maxY - minY + 1;
        float centerX = minX + (bboxWidth / 2f);
        float centerY = minY + (bboxHeight / 2f);
        float halfDiag = 0.5f * Mathf.Sqrt((bboxWidth * bboxWidth) + (bboxHeight * bboxHeight));

        float designRadius = (DesignRadiusRatio * gridSize / 2f) - dilateRadius;
        // 원본 px → 중간 px 배율. 방향(좌우·상하)은 보존하고 축은 균일하게 축소한다.
        float scale = designRadius / halfDiag;

        // 1단계: 원본 해상도에서 최대 풀링해 중간 격자를 채운다.
        bool[] gridInk = new bool[gridSize * gridSize];
        for (int gy = 0; gy < gridSize; gy++)
        {
            float yOrigStart = ((gy - (gridSize / 2f)) / scale) + centerY;
            float yOrigEnd = ((gy + 1 - (gridSize / 2f)) / scale) + centerY;
            int y0 = Mathf.FloorToInt(Mathf.Min(yOrigStart, yOrigEnd));
            int y1 = Mathf.CeilToInt(Mathf.Max(yOrigStart, yOrigEnd)) - 1;
            if (y1 < y0) y1 = y0;
            // 크롭 bbox 밖은 잉크가 없다. 가장자리로 클램프하면 bbox 경계의 잉크가 격자 모서리까지 번진다.
            y0 = Mathf.Max(y0, minY);
            y1 = Mathf.Min(y1, maxY);

            for (int gx = 0; gx < gridSize; gx++)
            {
                float xOrigStart = ((gx - (gridSize / 2f)) / scale) + centerX;
                float xOrigEnd = ((gx + 1 - (gridSize / 2f)) / scale) + centerX;
                int x0 = Mathf.FloorToInt(Mathf.Min(xOrigStart, xOrigEnd));
                int x1 = Mathf.CeilToInt(Mathf.Max(xOrigStart, xOrigEnd)) - 1;
                if (x1 < x0) x1 = x0;
                x0 = Mathf.Max(x0, minX);
                x1 = Mathf.Min(x1, maxX);

                gridInk[(gy * gridSize) + gx] = x0 <= x1 && y0 <= y1
                    && RegionHasInk(sat, sourceWidth, x0, x1, y0, y1);
            }
        }

        // 2단계: 중간 격자에서 정사각 커널 최대 필터를 가로·세로로 나눠 적용(분리 가능 팽창).
        bool[] dilated = Dilate(gridInk, gridSize, dilateRadius);

        // 3단계: K×K 블록의 잉크 비율로 출력 텍셀을 정한다.
        int block = GridMultiplier;
        float blockArea = block * block;
        bool[] inkOutput = new bool[outputSize * outputSize];
        for (int oy = 0; oy < outputSize; oy++)
        {
            for (int ox = 0; ox < outputSize; ox++)
            {
                int count = 0;
                for (int by = 0; by < block; by++)
                {
                    int gy = (oy * block) + by;
                    for (int bx = 0; bx < block; bx++)
                    {
                        int gx = (ox * block) + bx;
                        if (dilated[(gy * gridSize) + gx]) count++;
                    }
                }

                inkOutput[(oy * outputSize) + ox] = (count / blockArea) >= coverageThreshold;
            }
        }

        // 4단계: 음각 음영. 빛이 들어오는 쪽으로 이웃 텍셀이 배경(또는 범위 밖)이면 홈 벽이 빛을
        // 등지므로 그림자를 칠한다. 두 축 다 평가해 모서리에 몰린 홈도 그림자가 진다.
        for (int oy = 0; oy < outputSize; oy++)
        {
            for (int ox = 0; ox < outputSize; ox++)
            {
                if (!inkOutput[(oy * outputSize) + ox]) continue;

                bool shadow =
                    (lightFrom.x != 0 && !IsInkAt(inkOutput, outputSize, ox + lightFrom.x, oy)) ||
                    (lightFrom.y != 0 && !IsInkAt(inkOutput, outputSize, ox, oy + lightFrom.y));

                output[(oy * outputSize) + ox] = shadow ? InkShadowColor : InkColor;
            }
        }

        return output;
    }

    private static bool IsInkAt(bool[] inkOutput, int outputSize, int x, int y)
    {
        if (x < 0 || x >= outputSize || y < 0 || y >= outputSize) return false;
        return inkOutput[(y * outputSize) + x];
    }

    private static bool[] BuildInkMask(Color32[] source)
    {
        bool[] mask = new bool[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            Color32 c = source[i];
            int luminance = (c.r + c.g + c.b) / 3;
            mask[i] = c.a >= 128 && luminance < 128;
        }

        return mask;
    }

    private static bool TryComputeBoundingBox(bool[] mask, int width, int height,
        out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = int.MaxValue;
        maxX = int.MinValue;
        minY = int.MaxValue;
        maxY = int.MinValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!mask[(y * width) + x]) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        return maxX >= minX;
    }

    /// <summary>(width+1)×(height+1) 누적합 테이블. sat[(y*(width+1))+x]는 (0,0)~(x-1,y-1) 잉크 개수.</summary>
    private static int[] BuildSummedAreaTable(bool[] mask, int width, int height)
    {
        int satWidth = width + 1;
        int[] sat = new int[satWidth * (height + 1)];
        for (int y = 0; y < height; y++)
        {
            int rowSum = 0;
            for (int x = 0; x < width; x++)
            {
                rowSum += mask[(y * width) + x] ? 1 : 0;
                sat[((y + 1) * satWidth) + (x + 1)] = sat[(y * satWidth) + (x + 1)] + rowSum;
            }
        }

        return sat;
    }

    private static bool RegionHasInk(int[] sat, int width, int x0, int x1, int y0, int y1)
    {
        int satWidth = width + 1;
        int sum = sat[((y1 + 1) * satWidth) + (x1 + 1)]
                  - sat[(y0 * satWidth) + (x1 + 1)]
                  - sat[((y1 + 1) * satWidth) + x0]
                  + sat[(y0 * satWidth) + x0];
        return sum > 0;
    }

    private static bool[] Dilate(bool[] grid, int gridSize, int radius)
    {
        if (radius <= 0) return grid;

        bool[] horizontal = new bool[grid.Length];
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                bool hit = false;
                int lo = Mathf.Max(0, x - radius);
                int hi = Mathf.Min(gridSize - 1, x + radius);
                for (int nx = lo; nx <= hi && !hit; nx++) hit = grid[(y * gridSize) + nx];
                horizontal[(y * gridSize) + x] = hit;
            }
        }

        bool[] result = new bool[grid.Length];
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                bool hit = false;
                int lo = Mathf.Max(0, y - radius);
                int hi = Mathf.Min(gridSize - 1, y + radius);
                for (int ny = lo; ny <= hi && !hit; ny++) hit = horizontal[(ny * gridSize) + x];
                result[(y * gridSize) + x] = hit;
            }
        }

        return result;
    }
}
