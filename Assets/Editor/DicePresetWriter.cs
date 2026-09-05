using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Tessera.Core;
using UnityEngine;

/// <summary>
/// 시뮬레이션 결과를 기존 프리셋 JSON 포맷으로 직렬화한다(M10.9-T3).
///
/// 시뮬레이션은 재생 좌표(주사위 루트 로컬)로 나오지만 저장 포맷은 소스 단위(주사위 한 변 1.62)다.
/// BakedDiceController.TransformPresetDie의 정확한 역함수로 되돌려 적는다. 미러는 런타임이
/// 무작위로 적용하는 기능이므로 비미러 상태로만 굽는다.
/// </summary>
public static class DicePresetWriter
{
    private const float SoundVolume = 0.65f;
    private const float FallbackImpactTime = 0.25f;
    private const int TrailingFrames = 3;

    private static readonly Quaternion InverseThrowYaw = Quaternion.Inverse(Quaternion.Euler(0f, 180f, 0f));

    /// <summary>재생 좌표 -> 저장 좌표.</summary>
    public static Vector3 EncodePosition(Vector3 world)
    {
        return new Vector3(
            -world.x / DiceBoardMetrics.SourceToUnityScale,
            DiceBoardMetrics.PresetFloorY + (world.y - DiceBoardMetrics.FloorRestY) / DiceBoardMetrics.SourceToUnityScale,
            -world.z / DiceBoardMetrics.SourceToUnityScale);
    }

    /// <summary>재생 자세 -> 저장 자세.</summary>
    public static Quaternion EncodeRotation(Quaternion world)
    {
        return InverseThrowYaw * world;
    }

    /// <summary>클립 하나를 JSON 오브젝트 문자열로 만든다.</summary>
    public static string BuildClip(string mode, int diceCount, int octaCount, DicePresetCandidate candidate)
    {
        DiceSimulationResult result = candidate.Result;
        int settleFrame = Mathf.CeilToInt(result.SettleTime * DicePresetBakeRig.OutputFps);
        int frameCount = Mathf.Clamp(settleFrame + TrailingFrames + 1, 2, result.Positions.Length);
        float impactTime = result.ImpactTime >= 0f ? result.ImpactTime : FallbackImpactTime;

        StringBuilder builder = new();
        builder.Append("  {\n");
        builder.Append($"    \"mode\": \"{mode}\",\n");
        builder.Append($"    \"diceCount\": {diceCount},\n");
        builder.Append($"    \"octaCount\": {octaCount},\n");
        builder.Append($"    \"score\": {Number(candidate.Score, 1)},\n");
        builder.Append($"    \"settleTime\": {Number(result.SettleTime, 3)},\n");
        builder.Append("    \"isValid\": true,\n");
        builder.Append("    \"disqualificationReason\": \"정상 통과\",\n");
        builder.Append("    \"frames\": [\n");
        for (int frame = 0; frame < frameCount; frame++)
        {
            builder.Append("      [");
            for (int die = 0; die < diceCount; die++)
            {
                Vector3 position = EncodePosition(result.Positions[frame][die]);
                Quaternion rotation = EncodeRotation(result.Rotations[frame][die]);
                if (die > 0) builder.Append(", ");
                builder.Append('[');
                builder.Append(Number(position.x, 3)).Append(", ");
                builder.Append(Number(position.y, 3)).Append(", ");
                builder.Append(Number(position.z, 3)).Append(", ");
                builder.Append(Number(rotation.x, 4)).Append(", ");
                builder.Append(Number(rotation.y, 4)).Append(", ");
                builder.Append(Number(rotation.z, 4)).Append(", ");
                builder.Append(Number(rotation.w, 4));
                builder.Append(']');
            }
            builder.Append(frame == frameCount - 1 ? "]\n" : "],\n");
        }
        builder.Append("    ],\n");
        builder.Append($"    \"length\": {frameCount},\n");
        builder.Append($"    \"fps\": {DicePresetBakeRig.OutputFps},\n");
        builder.Append("    \"soundEvents\": [\n");
        builder.Append($"      {{ \"time\": {Number(impactTime, 3)}, \"type\": \"roll\", \"volume\": {Number(SoundVolume, 2)}, \"startOffset\": 0 }}\n");
        builder.Append("    ]\n");
        builder.Append("  }");
        return builder.ToString();
    }

    public static string BuildFile(string mode, int diceCount, int octaCount, List<DicePresetCandidate> clips)
    {
        StringBuilder builder = new();
        builder.Append("[\n");
        for (int index = 0; index < clips.Count; index++)
        {
            builder.Append(BuildClip(mode, diceCount, octaCount, clips[index]));
            builder.Append(index == clips.Count - 1 ? "\n" : ",\n");
        }
        builder.Append("]\n");
        return builder.ToString();
    }

    public static string BuildIndex(IReadOnlyList<DicePresetScenario> scenarios, IReadOnlyList<int> clipCounts)
    {
        StringBuilder builder = new();
        builder.Append("[\n");
        for (int index = 0; index < scenarios.Count; index++)
        {
            DicePresetScenario scenario = scenarios[index];
            builder.Append("  {\n");
            builder.Append($"    \"file\": \"{scenario.File}\",\n");
            builder.Append($"    \"label\": \"{scenario.Label}\",\n");
            builder.Append($"    \"diceCount\": {scenario.DiceCount},\n");
            builder.Append($"    \"octaCount\": {scenario.OctaCount},\n");
            builder.Append($"    \"mode\": \"{scenario.Mode}\",\n");
            builder.Append($"    \"clipCount\": {clipCounts[index]}\n");
            builder.Append(index == scenarios.Count - 1 ? "  }\n" : "  },\n");
        }
        builder.Append("]\n");
        return builder.ToString();
    }

    private static string Number(float value, int digits)
    {
        float rounded = (float)System.Math.Round(value, digits);
        if (Mathf.Approximately(rounded, 0f)) return "0";
        return rounded.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
