using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>베이킹 시나리오 한 줄. 파일명 규칙은 런타임 선택 코드와 맞춰야 한다.</summary>
public sealed class DicePresetScenario
{
    public string File;
    public string Mode;
    public int DiceCount;
    public int OctaCount;
    public string Label;

    public DicePresetScenario(string file, string mode, int diceCount, int octaCount, string label)
    {
        File = file;
        Mode = mode;
        DiceCount = diceCount;
        OctaCount = octaCount;
        Label = label;
    }
}

/// <summary>
/// 주사위 착지 프리셋을 Unity 물리로 굽는다(M10.9-T5).
///
/// 자동 호출하지 않는다. 트레이 크기나 주사위 크기를 바꾼 뒤 메뉴로 다시 굽고 결과 JSON을 커밋한다.
/// 이 메뉴는 REST로도 호출되므로 모달 대화상자를 띄우지 않는다.
/// </summary>
public static class DicePresetBaker
{
    private const string OutputFolder = "Assets/StreamingAssets/WebSource/presets";
    private const int BatchSize = 100;
    private const int MaxAttempts = 1200;

    private static readonly DicePresetScenario[] Scenarios =
    {
        new("dice_presets_normal_1.json", "normal", 1, 0, "일반 주사위 1개"),
        new("dice_presets_normal_2.json", "normal", 2, 0, "일반 주사위 2개"),
        new("dice_presets_normal_3.json", "normal", 3, 0, "일반 주사위 3개"),
        new("dice_presets_normal_4.json", "normal", 4, 0, "일반 주사위 4개"),
        new("dice_presets_normal_5.json", "normal", 5, 0, "일반 주사위 5개"),
        new("dice_presets_normal_6.json", "normal", 6, 0, "일반 주사위 6개 (확장)"),

        new("dice_presets_flip_1.json", "flip", 1, 0, "판 뒤집기 1개"),
        new("dice_presets_flip_2.json", "flip", 2, 0, "판 뒤집기 2개"),
        new("dice_presets_flip_3.json", "flip", 3, 0, "판 뒤집기 3개"),
        new("dice_presets_flip_4.json", "flip", 4, 0, "판 뒤집기 4개"),
        new("dice_presets_flip_5.json", "flip", 5, 0, "판 뒤집기 5개"),

        new("dice_presets_mixed_0normal_1octa.json", "octahedron", 1, 1, "8면체 1개"),
        new("dice_presets_mixed_0normal_2octa.json", "octahedron", 2, 2, "8면체 2개"),
        new("dice_presets_mixed_1normal_1octa.json", "octahedron", 2, 1, "일반 1개 + 8면체 1개"),
        new("dice_presets_mixed_1normal_2octa.json", "octahedron", 3, 2, "일반 1개 + 8면체 2개"),
        new("dice_presets_mixed_2normal_1octa.json", "octahedron", 3, 1, "일반 2개 + 8면체 1개"),
        new("dice_presets_mixed_2normal_2octa.json", "octahedron", 4, 2, "일반 2개 + 8면체 2개"),
        new("dice_presets_mixed_3normal_1octa.json", "octahedron", 4, 1, "일반 3개 + 8면체 1개"),
        new("dice_presets_mixed_3normal_2octa.json", "octahedron", 5, 2, "일반 3개 + 8면체 2개"),
        new("dice_presets_mixed_4normal_2octa.json", "octahedron", 6, 2, "일반 4개 + 8면체 2개 (6개 확장)")
    };

    [MenuItem("Tessera/Bake/Dice Presets")]
    public static void BakeAll()
    {
        Directory.CreateDirectory(OutputFolder);
        List<int> clipCounts = new();
        try
        {
            for (int index = 0; index < Scenarios.Length; index++)
            {
                DicePresetScenario scenario = Scenarios[index];
                EditorUtility.DisplayProgressBar(
                    "주사위 프리셋 베이킹",
                    $"{scenario.Label} ({index + 1}/{Scenarios.Length})",
                    index / (float)Scenarios.Length);
                clipCounts.Add(BakeScenario(scenario));
            }

            File.WriteAllText(Path.Combine(OutputFolder, "index.json"), DicePresetWriter.BuildIndex(Scenarios, clipCounts));
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        Debug.Log($"[DicePresetBaker] 시나리오 {Scenarios.Length}종 베이킹 완료.");
    }

    /// <summary>형상이나 발사 값을 손볼 때 한 종만 빠르게 확인하는 경로.</summary>
    [MenuItem("Tessera/Bake/Dice Presets (일반 5개만)")]
    public static void BakeNormalFive()
    {
        try
        {
            BakeScenario(Scenarios[4]);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        AssetDatabase.Refresh();
    }

    private static int BakeScenario(DicePresetScenario scenario)
    {
        System.Random random = new(scenario.File.GetHashCode());
        List<DicePresetCandidate> pool = new();
        Dictionary<string, int> rejections = new();
        int attempts = 0;

        using (DicePresetBakeRig rig = new(scenario.DiceCount, scenario.OctaCount))
        {
            bool isFlip = scenario.Mode == "flip";
            while (attempts < MaxAttempts)
            {
                for (int index = 0; index < BatchSize; index++)
                {
                    attempts++;
                    DiceSimulationResult result = rig.Simulate(random, isFlip);
                    string rejection = DicePresetScoring.Reject(rig, result);
                    if (rejection != null)
                    {
                        rejections.TryGetValue(rejection, out int count);
                        rejections[rejection] = count + 1;
                        continue;
                    }
                    pool.Add(new DicePresetCandidate
                    {
                        Result = result,
                        Score = DicePresetScoring.DistributionScore(result)
                    });
                }

                if (DicePresetScoring.FilledBinCount(pool) >= DicePresetScoring.ClipsPerFile) break;
            }
        }

        List<DicePresetCandidate> selected = DicePresetScoring.SelectStratified(pool);
        if (selected.Count < DicePresetScoring.ClipsPerFile)
        {
            Debug.LogError(
                $"[DicePresetBaker] {scenario.File}: 유효 클립 {selected.Count}개로 {DicePresetScoring.ClipsPerFile}개를 채우지 못했습니다. " +
                $"시도 {attempts}회, 탈락 사유 {Describe(rejections)}");
            return selected.Count;
        }

        File.WriteAllText(
            Path.Combine(OutputFolder, scenario.File),
            DicePresetWriter.BuildFile(scenario.Mode, scenario.DiceCount, scenario.OctaCount, selected));

        Debug.Log(
            $"[DicePresetBaker] {scenario.File}: 시도 {attempts}회, 통과 {pool.Count}개, 채택 {selected.Count}개, " +
            $"판정 시간 {selected[0].Result.SettleTime:F2}~{selected[^1].Result.SettleTime:F2}초, 탈락 사유 {Describe(rejections)}");
        return selected.Count;
    }

    private static string Describe(Dictionary<string, int> rejections)
    {
        if (rejections.Count == 0) return "없음";
        List<string> parts = new();
        foreach (KeyValuePair<string, int> entry in rejections)
        {
            parts.Add($"{entry.Key} {entry.Value}");
        }
        return string.Join(", ", parts);
    }
}
