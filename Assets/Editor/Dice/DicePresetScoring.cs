using System.Collections.Generic;
using Tessera.Core;
using UnityEngine;

/// <summary>평가를 마친 후보 한 개.</summary>
public sealed class DicePresetCandidate
{
    public DiceSimulationResult Result;
    public float Score;
}

/// <summary>
/// 베이킹 결과 평가와 선별(M10.9-T4). 평가 기준은 세 가지다.
///
/// 1. 분포도 — 주사위가 보드에 고르게 퍼질수록 점수가 높다. 순위용 점수다.
/// 2. 인식도 — 주사위 위에 올라탔거나 기울어져 면이 위를 보지 않으면 그 클립은 버린다.
/// 3. 판정 시간 — 안착 시각이 허용 구간 밖이면 버린다. 구간 안에서는 빠르다고 더 좋지 않다.
///
/// 3번의 "구간 안에 고르게"는 점수가 아니라 선별 단계에서 시간 구간 층화로 처리한다.
/// </summary>
public static class DicePresetScoring
{
    public const float MinSettleTime = 1.0f;
    public const float MaxSettleTime = 2.0f;
    public const int ClipsPerFile = 20;

    // 면이 위를 향한다고 볼 각도 여유. D6는 약 8도, D8은 약 11도.
    private const float FlatDotD6 = 0.990f;
    private const float FlatDotD8 = 0.980f;
    private const float RestHeightTolerance = 0.066f;
    private const float BoundsEpsilon = 0.02f;

    private static readonly Vector3[] OctaFaceNormals = BuildOctaFaceNormals();

    /// <summary>탈락 사유. null이면 통과다.</summary>
    public static string Reject(DicePresetBakeRig rig, DiceSimulationResult result)
    {
        if (!result.Settled) return "안착 실패";
        if (result.Positions.Length < 2) return "프레임 부족";
        if (result.SettleTime < MinSettleTime) return "판정 시간 미달";
        if (result.SettleTime > MaxSettleTime) return "판정 시간 초과";

        Vector3[] positions = result.Positions[^1];
        Quaternion[] rotations = result.Rotations[^1];

        for (int index = 0; index < positions.Length; index++)
        {
            if (!IsFaceUp(rotations[index], rig.IsOcta(index))) return "면이 위를 향하지 않음";
            if (Mathf.Abs(positions[index].y - rig.RestHeight(index)) > RestHeightTolerance) return "바닥에 안착하지 않음";
            if (!IsInsidePlayArea(positions[index])) return "플레이 영역 이탈";
        }

        for (int left = 0; left < positions.Length; left++)
        {
            for (int right = left + 1; right < positions.Length; right++)
            {
                float planarDistance = PlanarDistance(positions[left], positions[right]);
                float heightGap = Mathf.Abs(positions[left].y - positions[right].y);
                if (planarDistance < DiceBoardMetrics.DieSize * 0.85f && heightGap > DiceBoardMetrics.DieSize * 0.45f)
                {
                    return "주사위가 겹쳐 쌓임";
                }
            }
        }

        return null;
    }

    /// <summary>분포도 점수. 퍼질수록 높고 한쪽에 몰릴수록 낮다.</summary>
    public static float DistributionScore(DiceSimulationResult result)
    {
        Vector3[] positions = result.Positions[^1];
        float halfSpan = 0.5f * Mathf.Min(
            DiceBoardMetrics.PlayBoundsMaxX - DiceBoardMetrics.PlayBoundsMinX,
            DiceBoardMetrics.PlayBoundsMaxZ - DiceBoardMetrics.PlayBoundsMinZ);

        Vector3 centroid = Vector3.zero;
        foreach (Vector3 position in positions) centroid += position;
        centroid /= positions.Length;

        float separation = 1f;
        float spread = 1f;
        if (positions.Length >= 2)
        {
            float minPair = float.MaxValue;
            float meanDistance = 0f;
            for (int left = 0; left < positions.Length; left++)
            {
                meanDistance += PlanarDistance(positions[left], centroid);
                for (int right = left + 1; right < positions.Length; right++)
                {
                    minPair = Mathf.Min(minPair, PlanarDistance(positions[left], positions[right]));
                }
            }
            meanDistance /= positions.Length;
            separation = Mathf.Clamp01((minPair / DiceBoardMetrics.DieSize - 1f) / 1.5f);
            spread = Mathf.Clamp01(meanDistance / (0.45f * halfSpan));
        }

        Vector3 playCenter = new(
            (DiceBoardMetrics.PlayBoundsMinX + DiceBoardMetrics.PlayBoundsMaxX) * 0.5f,
            0f,
            (DiceBoardMetrics.PlayBoundsMinZ + DiceBoardMetrics.PlayBoundsMaxZ) * 0.5f);
        float offCenter = Mathf.Clamp01(PlanarDistance(centroid, playCenter) / (0.5f * halfSpan));

        return 60f * separation + 30f * spread - 20f * offCenter;
    }

    /// <summary>
    /// 판정 시간 구간을 균등 분할하고 구간마다 분포 점수가 가장 높은 클립을 하나씩 뽑는다.
    /// 빈 구간이 남으면 남은 후보 중 점수가 높은 순으로 채워 정확히 ClipsPerFile개를 만든다.
    /// </summary>
    public static List<DicePresetCandidate> SelectStratified(List<DicePresetCandidate> pool)
    {
        DicePresetCandidate[] bins = new DicePresetCandidate[ClipsPerFile];
        float binWidth = (MaxSettleTime - MinSettleTime) / ClipsPerFile;

        foreach (DicePresetCandidate candidate in pool)
        {
            int bin = Mathf.Clamp(
                Mathf.FloorToInt((candidate.Result.SettleTime - MinSettleTime) / binWidth),
                0,
                ClipsPerFile - 1);
            if (bins[bin] == null || candidate.Score > bins[bin].Score) bins[bin] = candidate;
        }

        List<DicePresetCandidate> selected = new();
        foreach (DicePresetCandidate candidate in bins)
        {
            if (candidate != null) selected.Add(candidate);
        }

        if (selected.Count < ClipsPerFile)
        {
            List<DicePresetCandidate> leftovers = new();
            foreach (DicePresetCandidate candidate in pool)
            {
                if (!selected.Contains(candidate)) leftovers.Add(candidate);
            }
            leftovers.Sort((left, right) => right.Score.CompareTo(left.Score));
            for (int index = 0; index < leftovers.Count && selected.Count < ClipsPerFile; index++)
            {
                selected.Add(leftovers[index]);
            }
        }

        selected.Sort((left, right) => left.Result.SettleTime.CompareTo(right.Result.SettleTime));
        return selected;
    }

    /// <summary>남은 후보로 채울 수 있는 시간 구간이 아직 비어 있는지. 시뮬레이션을 더 돌릴지 판단한다.</summary>
    public static int FilledBinCount(List<DicePresetCandidate> pool)
    {
        bool[] bins = new bool[ClipsPerFile];
        float binWidth = (MaxSettleTime - MinSettleTime) / ClipsPerFile;
        foreach (DicePresetCandidate candidate in pool)
        {
            int bin = Mathf.Clamp(
                Mathf.FloorToInt((candidate.Result.SettleTime - MinSettleTime) / binWidth),
                0,
                ClipsPerFile - 1);
            bins[bin] = true;
        }

        int filled = 0;
        foreach (bool bin in bins)
        {
            if (bin) filled++;
        }
        return filled;
    }

    public static bool IsFaceUp(Quaternion rotation, bool isOcta)
    {
        float best = 0f;
        if (isOcta)
        {
            foreach (Vector3 normal in OctaFaceNormals)
            {
                best = Mathf.Max(best, Mathf.Abs((rotation * normal).y));
            }
            return best >= FlatDotD8;
        }

        best = Mathf.Max(best, Mathf.Abs((rotation * Vector3.right).y));
        best = Mathf.Max(best, Mathf.Abs((rotation * Vector3.up).y));
        best = Mathf.Max(best, Mathf.Abs((rotation * Vector3.forward).y));
        return best >= FlatDotD6;
    }

    private static bool IsInsidePlayArea(Vector3 position)
    {
        return position.x >= DiceBoardMetrics.PlayBoundsMinX - BoundsEpsilon
            && position.x <= DiceBoardMetrics.PlayBoundsMaxX + BoundsEpsilon
            && position.z >= DiceBoardMetrics.PlayBoundsMinZ - BoundsEpsilon
            && position.z <= DiceBoardMetrics.PlayBoundsMaxZ + BoundsEpsilon;
    }

    private static float PlanarDistance(Vector3 left, Vector3 right)
    {
        return new Vector2(left.x - right.x, left.z - right.z).magnitude;
    }

    private static Vector3[] BuildOctaFaceNormals()
    {
        Vector3[] normals = new Vector3[8];
        int index = 0;
        for (int signX = 0; signX < 2; signX++)
        {
            for (int signY = 0; signY < 2; signY++)
            {
                for (int signZ = 0; signZ < 2; signZ++)
                {
                    normals[index++] = new Vector3(
                        signX == 0 ? 1f : -1f,
                        signY == 0 ? 1f : -1f,
                        signZ == 0 ? 1f : -1f).normalized;
                }
            }
        }
        return normals;
    }
}
