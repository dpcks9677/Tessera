using System.IO;
using Newtonsoft.Json.Linq;
using Tessera.Dice;
using UnityEditor;
using UnityEngine;

/// <summary>
/// `coin-toss` 증강의 동전 스핀 궤적을 해석적으로 굽는다(M17-T24).
///
/// 물리 시뮬 대신 고정 체공 시간의 포물선 낙하 + 등속 X축 회전 + 착지 후 감쇠 흔들림으로 구성한다.
/// spinIndex별로 회전 바퀴 수만 달라 스핀 속도가 다르게 보인다. 회전량은 전부 180°의 짝수배라
/// 착지 시 항상 앞면(+Y)으로 선다.
///
/// 자동 호출하지 않는다. 궤적 값을 바꾼 뒤 메뉴로 다시 굽고 결과 JSON을 커밋한다.
/// </summary>
public static class CoinSpinPresetBaker
{
    private const string OutputFolder = "Assets/Resources/" + CoinTossVfx.ClipResourceFolder;

    // 공중 구간: 체공 시간 고정 포물선.
    private const float AirDuration = 0.9f;
    private const float PeakHeight = 3.0f;

    // spinIndex 0~4가 도는 180° 배수. 전부 짝수라 착지 시 앞면(+Y)이 유지된다.
    private static readonly int[] SpinHalfTurns = { 4, 6, 8, 10, 12 };

    // 착지 후 감쇠 흔들림.
    private const float SettleDuration = 0.45f;
    private const float SettleAmplitudeDeg = 8f;
    private const float SettleFrequencyHz = 6f;
    private const float SettleDecayRate = 8f;

    [MenuItem("Tessera/Bake/Coin Spin Presets")]
    public static void Bake()
    {
        Directory.CreateDirectory(OutputFolder);

        for (int spinIndex = 0; spinIndex < CoinTossVfx.PresetCount; spinIndex++)
        {
            CoinSpinClip clip = BakeClip(spinIndex);
            string path = Path.Combine(OutputFolder, $"{CoinTossVfx.ClipFileNamePrefix}{spinIndex}.json");
            File.WriteAllText(path, ToJson(clip));
        }

        AssetDatabase.Refresh();
        Debug.Log($"[CoinSpinPresetBaker] 스핀 프리셋 {CoinTossVfx.PresetCount}종 베이킹 완료.");
    }

    /// <summary>spinIndex(0~4)에 해당하는 동전 스핀 궤적을 계산한다.</summary>
    public static CoinSpinClip BakeClip(int spinIndex)
    {
        int halfTurns = SpinHalfTurns[spinIndex];
        int airFrameCount = Mathf.RoundToInt(AirDuration * CoinTossVfx.Fps);
        int landFrame = airFrameCount;
        int settleFrameCount = Mathf.RoundToInt(SettleDuration * CoinTossVfx.Fps);

        var frames = new CoinSpinFrame[landFrame + settleFrameCount + 1];

        // 공중 구간: y(t) = 4·H·(t/T)·(1-t/T), X축 등속 회전 halfTurns×180°.
        for (int i = 0; i <= airFrameCount; i++)
        {
            float t = i / (float)CoinTossVfx.Fps;
            float ratio = t / AirDuration;
            float y = 4f * PeakHeight * ratio * (1f - ratio);
            float angleX = halfTurns * 180f * ratio;
            frames[i] = new CoinSpinFrame(new Vector3(0f, y, 0f), Quaternion.Euler(angleX, 0f, 0f));
        }

        // 착지 후 구간: X·Z 틸트가 지수 감쇠하는 진동. 마지막 프레임은 정확히 flat으로 강제한다.
        for (int s = 1; s <= settleFrameCount; s++)
        {
            float t = s / (float)CoinTossVfx.Fps;
            bool isLast = s == settleFrameCount;
            float amplitude = isLast ? 0f : SettleAmplitudeDeg * Mathf.Exp(-SettleDecayRate * t);
            float tiltX = amplitude * Mathf.Sin(2f * Mathf.PI * SettleFrequencyHz * t);
            float tiltZ = amplitude * Mathf.Sin(2f * Mathf.PI * SettleFrequencyHz * t + Mathf.PI * 0.5f);

            frames[landFrame + s] = new CoinSpinFrame(
                Vector3.zero, Quaternion.Euler(halfTurns * 180f + tiltX, 0f, tiltZ));
        }

        return new CoinSpinClip(CoinTossVfx.Fps, landFrame, frames);
    }

    /// <summary>
    /// JsonUtility는 2차원 배열을 못 다루므로 Newtonsoft로 프레임을 [px,py,pz,qx,qy,qz,qw] 평탄 배열로 적는다.
    /// <see cref="CoinTossVfx.Parse"/>가 같은 형태를 읽는다.
    /// </summary>
    public static string ToJson(CoinSpinClip clip)
    {
        var frameTokens = new JArray();
        foreach (CoinSpinFrame frame in clip.Frames)
        {
            frameTokens.Add(new JArray(
                frame.Position.x, frame.Position.y, frame.Position.z,
                frame.Rotation.x, frame.Rotation.y, frame.Rotation.z, frame.Rotation.w));
        }

        var root = new JObject
        {
            ["fps"] = clip.Fps,
            ["landFrame"] = clip.LandFrame,
            ["frames"] = frameTokens
        };
        return root.ToString(Newtonsoft.Json.Formatting.Indented);
    }
}
