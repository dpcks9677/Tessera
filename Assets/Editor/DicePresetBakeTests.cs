using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Core;
using Tessera.Dice;
using UnityEngine;

/// <summary>
/// 프리셋 베이킹 좌표 변환과 평가 규칙 검증(M10.9).
///
/// 저장 좌표와 재생 좌표가 정확히 왕복해야 베이킹 결과가 화면에서 그대로 재현된다.
/// </summary>
public class DicePresetBakeTests
{
    [Test]
    public void 인코딩한_좌표는_재생_변환을_거쳐_원래_자세로_돌아온다()
    {
        Vector3[] worldPositions =
        {
            new(0f, DiceBoardMetrics.FloorRestY, 0f),
            new(2.4f, 1.85f, -1.7f),
            new(-3.1f, 0.65f, 1.4f)
        };
        Quaternion[] worldRotations =
        {
            Quaternion.identity,
            Quaternion.Euler(12f, 143f, 87f),
            Quaternion.Euler(-64f, 5f, 200f)
        };

        for (int index = 0; index < worldPositions.Length; index++)
        {
            WebPresetDie stored = new(
                DicePresetWriter.EncodePosition(worldPositions[index]),
                DicePresetWriter.EncodeRotation(worldRotations[index]));

            WebPresetDie played = BakedDiceController.TransformPresetDie(stored, false);

            Assert.That(Vector3.Distance(played.Position, worldPositions[index]), Is.LessThan(1e-3f));
            Assert.That(Quaternion.Angle(played.Rotation, worldRotations[index]), Is.LessThan(0.05f));
        }
    }

    [Test]
    public void 바닥에_놓인_주사위는_저장_좌표에서_프리셋_바닥_높이가_된다()
    {
        Vector3 stored = DicePresetWriter.EncodePosition(new Vector3(1f, DiceBoardMetrics.FloorRestY, -2f));
        Assert.That(stored.y, Is.EqualTo(DiceBoardMetrics.PresetFloorY).Within(1e-4f));
    }

    [Test]
    public void 미러_재생은_X만_뒤집는다()
    {
        WebPresetDie stored = new(
            DicePresetWriter.EncodePosition(new Vector3(2.0f, 0.9f, -1.1f)),
            DicePresetWriter.EncodeRotation(Quaternion.Euler(30f, 40f, 50f)));

        WebPresetDie plain = BakedDiceController.TransformPresetDie(stored, false);
        WebPresetDie mirrored = BakedDiceController.TransformPresetDie(stored, true);

        Assert.That(mirrored.Position.x, Is.EqualTo(-plain.Position.x).Within(1e-4f));
        Assert.That(mirrored.Position.y, Is.EqualTo(plain.Position.y).Within(1e-4f));
        Assert.That(mirrored.Position.z, Is.EqualTo(plain.Position.z).Within(1e-4f));
    }

    [Test]
    public void 기울어진_주사위는_면이_위를_향하지_않는다고_본다()
    {
        Assert.IsTrue(DicePresetScoring.IsFaceUp(Quaternion.identity, false));
        Assert.IsTrue(DicePresetScoring.IsFaceUp(Quaternion.Euler(0f, 37f, 90f), false));
        Assert.IsFalse(DicePresetScoring.IsFaceUp(Quaternion.Euler(30f, 0f, 0f), false));

        // 8면체는 면 법선이 (1,1,1) 방향이므로 그 방향을 월드 업으로 돌리면 면으로 선다.
        Assert.IsTrue(DicePresetScoring.IsFaceUp(Quaternion.FromToRotation(new Vector3(1f, 1f, 1f), Vector3.up), true));
        Assert.IsFalse(DicePresetScoring.IsFaceUp(Quaternion.identity, true));
    }

    [Test]
    public void 층화_선별은_판정_시간_구간마다_최고_점수를_하나씩_고른다()
    {
        List<DicePresetCandidate> pool = new();
        for (int bin = 0; bin < DicePresetScoring.ClipsPerFile; bin++)
        {
            float settleTime = DicePresetScoring.MinSettleTime + bin * 0.05f + 0.01f;
            pool.Add(MakeCandidate(settleTime, 10f));
            pool.Add(MakeCandidate(settleTime + 0.01f, 90f));
        }

        List<DicePresetCandidate> selected = DicePresetScoring.SelectStratified(pool);

        Assert.That(selected.Count, Is.EqualTo(DicePresetScoring.ClipsPerFile));
        foreach (DicePresetCandidate candidate in selected)
        {
            Assert.That(candidate.Score, Is.EqualTo(90f));
        }
        for (int index = 1; index < selected.Count; index++)
        {
            Assert.That(selected[index].Result.SettleTime, Is.GreaterThanOrEqualTo(selected[index - 1].Result.SettleTime));
        }
    }

    private static DicePresetCandidate MakeCandidate(float settleTime, float score)
    {
        return new DicePresetCandidate
        {
            Result = new DiceSimulationResult
            {
                Positions = new[] { new[] { Vector3.zero } },
                Rotations = new[] { new[] { Quaternion.identity } },
                SettleTime = settleTime,
                ImpactTime = 0.3f,
                Settled = true
            },
            Score = score
        };
    }
}
