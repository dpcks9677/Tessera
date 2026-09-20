using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Games.Yacht;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 증강 모드 게임을 무작위 입력으로 여러 판 완주시켜, 드래프트·굴림·수동 행동·기입 경로가
    /// 예외 없이 끝까지 진행되는지 확인한다. 특정 시드의 결정론 시나리오가 아니라
    /// "많은 무작위 조합에서 진행이 멈추지 않는다"를 검증하는 것이 목적이다.
    /// </summary>
    [TestFixture]
    public sealed class YachtRandomFullGamePlaythroughTests
    {
        private const int SeedCount = 60;
        private const int MaxIterationsPerGame = 2000;

        [Test]
        public void RandomPlaythrough_CompletesWithoutError_AcrossManySeeds()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                PlaythroughStats stats;
                try
                {
                    stats = PlayOneGame(seed);
                }
                catch (Exception exception)
                {
                    Assert.Fail($"seed {seed}: 진행 중 예외 발생 - {exception}");
                    return;
                }

                Assert.That(stats.Session.Phase, Is.EqualTo(YachtGamePhase.GameOver), $"seed {seed}: 게임 종료 상태가 아님");
                Assert.That(stats.Session.CurrentRound, Is.EqualTo(YachtGameSession.LastRound), $"seed {seed}: 마지막 라운드에 도달하지 못함");

                for (int playerIndex = 0; playerIndex < YachtGameSession.PlayerCount; playerIndex++)
                {
                    for (int i = 0; i < YachtScoreCalculator.ScorableCategories.Length; i++)
                    {
                        ScoreCategory category = YachtScoreCalculator.ScorableCategories[i];
                        Assert.That(stats.Session.IsCategoryFilled(playerIndex, category), Is.True,
                            $"seed {seed}: 플레이어 {playerIndex}의 {category} 칸이 비어 있음");
                    }

                    PlayerScoreData player = stats.Players[playerIndex];
                    int trackedTotal = player.totalScore;
                    player.RecalculateTotal();
                    Assert.That(player.totalScore, Is.EqualTo(trackedTotal),
                        $"seed {seed}: 플레이어 {playerIndex}의 합계 점수가 PlayerScoreData 재계산 결과와 불일치");
                }
            }
        }

        [Test]
        public void RandomPlaythrough_ExercisesAugmentAndManualActionPaths()
        {
            const int seedCount = 20;
            var distinctAugmentIds = new HashSet<string>();
            int manualActionUses = 0;

            for (int seed = 0; seed < seedCount; seed++)
            {
                PlaythroughStats stats = PlayOneGame(seed);
                foreach (string augmentId in stats.SelectedAugmentIds) distinctAugmentIds.Add(augmentId);
                manualActionUses += stats.ManualActionUses;
            }

            Assert.That(distinctAugmentIds.Count, Is.GreaterThanOrEqualTo(10),
                $"선택된 서로 다른 증강 종류가 너무 적음: {distinctAugmentIds.Count}종");
            Assert.That(manualActionUses, Is.GreaterThanOrEqualTo(1), "수동 행동 증강이 한 번도 발동되지 않음");
        }

        /// <summary>후보 목록을 난수원으로 뒤섞은 복사본을 만든다(Fisher-Yates).</summary>
        private static List<string> ShuffleOptions(IReadOnlyList<string> options, IRandomSource random)
        {
            var shuffled = new List<string>(options);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int swapIndex = random.NextInt(0, i + 1);
                (shuffled[i], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[i]);
            }
            return shuffled;
        }

        private static PlaythroughStats PlayOneGame(int seed)
        {
            var random = new SeededRandomSource(seed);
            var players = new[] { new PlayerScoreData(), new PlayerScoreData() };
            var session = new YachtGameSession(players[0], players[1], new YachtGameOptions { Mode = YachtGameMode.Augmented }, random);
            session.StartNewGame();

            int playerTurnsCompleted = 0;
            int manualActionUses = 0;
            var selectedAugmentIds = new List<string>();

            for (int iteration = 0; session.Phase != YachtGamePhase.GameOver; iteration++)
            {
                if (iteration >= MaxIterationsPerGame)
                {
                    Assert.Fail($"seed {seed}: 진행이 멈춤. round={session.CurrentRound}, phase={session.Phase}");
                }

                if (session.Phase == YachtGamePhase.Draft)
                {
                    // 후보 중 일부는 이미 보유한 증강과 충돌해 거부될 수 있으므로, 무작위 순서로 시도해
                    // 받아들여지는 첫 후보를 고른다. 모두 거부되면 실제 회귀다.
                    List<string> shuffledOptions = ShuffleOptions(session.State.Draft.Options, random);
                    Assert.That(shuffledOptions.Count, Is.GreaterThan(0), $"seed {seed}: 드래프트 후보가 비어 있음");
                    string picked = null;
                    for (int i = 0; i < shuffledOptions.Count; i++)
                    {
                        string candidate = shuffledOptions[i];
                        if (!session.TrySelectAugment(candidate, out YachtGameCommandResult draftResult)) continue;
                        picked = candidate;
                        break;
                    }
                    Assert.That(picked, Is.Not.Null, $"seed {seed}: 제시된 증강 후보가 전부 거부됨");
                    selectedAugmentIds.Add(picked);
                    continue;
                }

                Assert.That(session.TryRoll(out YachtGameCommandResult firstRollResult), Is.True,
                    $"seed {seed}: 첫 굴림 실패 {firstRollResult.ErrorCode}, {firstRollResult.ErrorMessage}");

                int extraRolls = random.NextInt(0, 3);
                for (int i = 0; i < extraRolls && session.CanRoll; i++)
                {
                    IReadOnlyList<IReadOnlyYachtDieState> dice = session.State.Dice;
                    for (int dieIndex = 0; dieIndex < dice.Count; dieIndex++)
                        session.TrySetDieKept(dieIndex, random.NextBool());
                    session.TryRoll(out _);
                }

                int currentPlayer = session.CurrentPlayerIndex;
                IReadOnlyList<string> owned = session.State.AugmentPlayers[currentPlayer].OwnedIds;
                for (int i = 0; i < owned.Count; i++)
                {
                    string augmentId = owned[i];
                    if (!session.CanUseAugmentAction(augmentId, out _, out _)) continue;
                    if (!random.NextBool()) continue;
                    if (session.TryUseAugmentAction(augmentId, out _)) manualActionUses++;
                }

                var fillableCategories = new List<ScoreCategory>();
                for (int i = 0; i < YachtScoreCalculator.ScorableCategories.Length; i++)
                {
                    ScoreCategory category = YachtScoreCalculator.ScorableCategories[i];
                    if (!session.IsCategoryFilled(currentPlayer, category)) fillableCategories.Add(category);
                }
                Assert.That(fillableCategories.Count, Is.GreaterThan(0), $"seed {seed}: 기입 가능한 족보가 없음");
                ScoreCategory chosenCategory = fillableCategories[random.NextInt(0, fillableCategories.Count)];
                Assert.That(session.TryCommitScore(chosenCategory, out YachtTurnResult turnResult), Is.True,
                    $"seed {seed}: 점수 기입 실패 {chosenCategory}");
                playerTurnsCompleted++;

                if (!turnResult.GameEnded)
                    Assert.That(session.AdvanceTurnAfterAnimation(), Is.True, $"seed {seed}: 턴 전환 실패");
            }

            return new PlaythroughStats(session, players, playerTurnsCompleted, selectedAugmentIds, manualActionUses);
        }

        /// <summary>System.Random을 감싸는 시드 가능한 난수원. SystemRandomSource와 같은 min 포함/max 배타 규약을 따른다.</summary>
        private sealed class SeededRandomSource : IRandomSource
        {
            private readonly Random random;

            public SeededRandomSource(int seed) => random = new Random(seed);

            public int NextInt(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);

            public bool NextBool() => random.Next(0, 2) == 1;
        }

        private readonly struct PlaythroughStats
        {
            public readonly YachtGameSession Session;
            public readonly PlayerScoreData[] Players;
            public readonly int PlayerTurnsCompleted;
            public readonly List<string> SelectedAugmentIds;
            public readonly int ManualActionUses;

            public PlaythroughStats(YachtGameSession session, PlayerScoreData[] players, int playerTurnsCompleted, List<string> selectedAugmentIds, int manualActionUses)
            {
                Session = session;
                Players = players;
                PlayerTurnsCompleted = playerTurnsCompleted;
                SelectedAugmentIds = selectedAugmentIds;
                ManualActionUses = manualActionUses;
            }
        }
    }
}
