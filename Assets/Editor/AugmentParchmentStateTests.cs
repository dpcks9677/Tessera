using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Games.Yacht;

public sealed class AugmentParchmentStateTests
{
    [Test]
    public void Draft_세옵션에중복없는프리셋을배정하고_선택값을보유상태로옮긴다()
    {
        YachtGameState state = CreateDraftState();
        var runtime = new YachtAugmentRuntime();
        runtime.Initialize(state, 2);
        var gameRandom = new MinimumRandomSource();
        var visualRandom = new MinimumRandomSource();

        Assert.That(runtime.TryBeginDraft(state, gameRandom, visualRandom, out _), Is.True);
        Assert.That(state.Draft.OptionCardPresetIds.Length, Is.EqualTo(3));
        Assert.That(new HashSet<int>(state.Draft.OptionCardPresetIds).Count, Is.EqualTo(3));

        string selected = state.Draft.Options[0];
        int selectedPreset = state.Draft.OptionCardPresetIds[0];
        Assert.That(runtime.TrySelectAugment(state, 0, selected, gameRandom, visualRandom,
            out _, out _, out _), Is.True);

        int ownedIndex = System.Array.IndexOf(state.AugmentPlayers[0].OwnedIds, selected);
        if (ownedIndex >= 0)
            Assert.That(state.AugmentPlayers[0].OwnedCardPresetIds[ownedIndex], Is.EqualTo(selectedPreset));
        YachtGameState clone = state.Clone();
        Assert.That(clone.Draft.OptionCardPresetIds, Is.Not.SameAs(state.Draft.OptionCardPresetIds));
        Assert.That(clone.AugmentPlayers[0].OwnedCardPresetIds, Is.Not.SameAs(state.AugmentPlayers[0].OwnedCardPresetIds));
    }

    [Test]
    public void RandomBox_결과카드가_기존양피지프리셋을상속한다()
    {
        YachtGameState state = CreateDraftState();
        var runtime = new YachtAugmentRuntime();
        runtime.Initialize(state, 2);
        state.Phase = YachtGamePhase.Draft;
        state.Draft.IsActive = true;
        state.Draft.PlayerIndex = 0;
        state.Draft.SelectionCounts = new[] { 0, 1 };
        state.Draft.Options = new[] { YachtAugmentRuntime.RandomBoxId };
        state.Draft.OptionCardPresetIds = new[] { 4 };
        var random = new MinimumRandomSource();

        Assert.That(runtime.TrySelectAugment(state, 0, YachtAugmentRuntime.RandomBoxId, random, random,
            out _, out _, out _), Is.True);
        Assert.That(state.AugmentPlayers[0].OwnedIds, Has.Length.EqualTo(1));
        Assert.That(state.AugmentPlayers[0].OwnedIds[0], Is.Not.EqualTo(YachtAugmentRuntime.RandomBoxId));
        Assert.That(state.AugmentPlayers[0].OwnedCardPresetIds, Is.EqualTo(new[] { 3 }));
    }

    [TestCase(-1, 0)]
    [TestCase(5, 0)]
    [TestCase(4, 3)]
    [TestCase(3, 3)]
    public void CardPreset_유효하지않은값은_첫프리셋으로폴백한다(int value, int expected)
    {
        Assert.That(YachtAugmentRuntime.NormalizeCardPreset(value), Is.EqualTo(expected));
    }

    private static YachtGameState CreateDraftState() => new()
    {
        Mode = YachtGameMode.Augmented,
        Phase = YachtGamePhase.TurnReady,
        CurrentRound = 1,
        CurrentPlayerIndex = 0,
        Players = new[] { new PlayerScoreData(), new PlayerScoreData() }
    };

    private sealed class MinimumRandomSource : IRandomSource
    {
        public int NextInt(int minInclusive, int maxExclusive) => minInclusive;
        public bool NextBool() => false;
    }
}
