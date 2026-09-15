using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Core;
using Tessera.Dice;
using UnityEngine;

/// <summary>동전 스핀 프리셋 베이킹과 재생 유틸 검증(M17-T24).</summary>
public class CoinSpinPresetTests
{
    [Test]
    public void AllPresetsLandFlatAtOrigin()
    {
        for (int spinIndex = 0; spinIndex < CoinTossVfx.PresetCount; spinIndex++)
        {
            CoinSpinClip clip = CoinSpinPresetBaker.BakeClip(spinIndex);

            CoinSpinFrame last = clip.Frames[^1];
            Assert.That((last.Rotation * Vector3.up).y, Is.GreaterThan(0.99f), $"spinIndex={spinIndex} 마지막 프레임");
            Assert.That(last.Position.magnitude, Is.LessThan(1e-3f), $"spinIndex={spinIndex} 마지막 프레임 위치");

            CoinSpinFrame landed = clip.Frames[clip.LandFrame];
            Assert.That((landed.Rotation * Vector3.up).y, Is.GreaterThan(0.99f), $"spinIndex={spinIndex} landFrame");
            Assert.That(landed.Position.y, Is.EqualTo(0f).Within(1e-3f), $"spinIndex={spinIndex} landFrame y");
        }
    }

    [Test]
    public void SpinSpeedIncreasesWithSpinIndex()
    {
        float previousTotalAngle = -1f;
        for (int spinIndex = 0; spinIndex < CoinTossVfx.PresetCount; spinIndex++)
        {
            CoinSpinClip clip = CoinSpinPresetBaker.BakeClip(spinIndex);

            float totalAngle = 0f;
            for (int i = 1; i <= clip.LandFrame; i++)
            {
                totalAngle += Quaternion.Angle(clip.Frames[i - 1].Rotation, clip.Frames[i].Rotation);
            }

            Assert.That(totalAngle, Is.GreaterThan(previousTotalAngle), $"spinIndex={spinIndex}");
            previousTotalAngle = totalAngle;
        }
    }

    [Test]
    public void AirPhaseRisesAboveOrigin()
    {
        CoinSpinClip clip = CoinSpinPresetBaker.BakeClip(0);
        CoinSpinFrame midAir = clip.Frames[clip.LandFrame / 2];
        Assert.That(midAir.Position.y, Is.GreaterThan(0f));
    }

    [Test]
    public void ApplyFaceTailsFlipsLastFrameToFaceDown()
    {
        CoinSpinClip clip = CoinSpinPresetBaker.BakeClip(0);
        CoinSpinFrame last = clip.Frames[^1];

        Quaternion flipped = CoinTossVfx.ApplyFace(last.Rotation, false);

        Assert.That((flipped * Vector3.up).y, Is.LessThan(-0.99f));
    }

    [Test]
    public void JsonRoundTripPreservesFrames()
    {
        CoinSpinClip original = CoinSpinPresetBaker.BakeClip(2);
        string json = CoinSpinPresetBaker.ToJson(original);

        CoinSpinClip parsed = CoinTossVfx.Parse(json);

        Assert.That(parsed.Fps, Is.EqualTo(original.Fps));
        Assert.That(parsed.LandFrame, Is.EqualTo(original.LandFrame));
        Assert.That(parsed.Frames.Length, Is.EqualTo(original.Frames.Length));
        for (int i = 0; i < original.Frames.Length; i++)
        {
            Assert.That(Vector3.Distance(parsed.Frames[i].Position, original.Frames[i].Position), Is.LessThan(1e-4f));
            Assert.That(Quaternion.Angle(parsed.Frames[i].Rotation, original.Frames[i].Rotation), Is.LessThan(0.05f));
        }
    }

    [Test]
    public void IsHeadsReadsBitPerCoinIndex()
    {
        // faces = 0b101: 코인 0=앞면, 코인 1=뒷면, 코인 2=앞면.
        int faces = 0b101;
        Assert.IsTrue(CoinTossVfx.IsHeads(faces, 0));
        Assert.IsFalse(CoinTossVfx.IsHeads(faces, 1));
        Assert.IsTrue(CoinTossVfx.IsHeads(faces, 2));
    }

    [Test]
    public void PickPresetsNeverDuplicatesAcrossSeeds()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            int[] picked = CoinTossVfx.PickPresets(new System.Random(seed));

            Assert.That(picked.Length, Is.EqualTo(3));
            var seen = new HashSet<int>(picked);
            Assert.That(seen.Count, Is.EqualTo(3), $"seed={seed}");
            foreach (int index in picked)
            {
                Assert.That(index, Is.InRange(0, CoinTossVfx.PresetCount - 1));
            }
        }
    }

    [Test]
    public void ResolveAnchorStaysWithinTrayPlayBounds()
    {
        const float coinRadius = 0.8f;
        for (int i = 0; i < CoinTossVfx.CoinOffsetsX.Length; i++)
        {
            Vector3 anchor = CoinTossVfx.ResolveAnchor(i, centerSectionX: 0f);

            Assert.That(anchor.x - coinRadius, Is.GreaterThanOrEqualTo(DiceBoardMetrics.PlayBoundsMinX), $"i={i}");
            Assert.That(anchor.x + coinRadius, Is.LessThanOrEqualTo(DiceBoardMetrics.PlayBoundsMaxX), $"i={i}");
            Assert.That(anchor.z, Is.InRange(DiceBoardMetrics.PlayBoundsMinZ, DiceBoardMetrics.PlayBoundsMaxZ), $"i={i}");
        }
    }
}
