using System;
using System.Collections.Generic;

namespace Tessera.Games.AugmentedYacht
{
    public enum YachtGamePhase
    {
        WaitingToStart,
        TurnReady,
        Rolling,
        ScoreSelection,
        TurnTransition,
        GameOver
    }

    public readonly struct YachtTurnResult
    {
        public YachtTurnResult(int scoredPlayerIndex, ScoreCategory category, int score, bool gameEnded)
        {
            ScoredPlayerIndex = scoredPlayerIndex;
            Category = category;
            Score = score;
            GameEnded = gameEnded;
        }

        public int ScoredPlayerIndex { get; }
        public ScoreCategory Category { get; }
        public int Score { get; }
        public bool GameEnded { get; }
    }

    public static class YachtScoreCalculator
    {
        public static readonly ScoreCategory[] ScorableCategories =
        {
            ScoreCategory.Aces,
            ScoreCategory.Deuces,
            ScoreCategory.Threes,
            ScoreCategory.Fours,
            ScoreCategory.Fives,
            ScoreCategory.Sixes,
            ScoreCategory.Choice,
            ScoreCategory.FourOfAKind,
            ScoreCategory.FullHouse,
            ScoreCategory.SmallStraight,
            ScoreCategory.LargeStraight,
            ScoreCategory.Yacht
        };

        public static Dictionary<ScoreCategory, int> Calculate(IReadOnlyList<int> dice)
        {
            if (dice == null || dice.Count != 5)
            {
                throw new ArgumentException("기본 요트 점수 계산에는 주사위 5개가 필요합니다.", nameof(dice));
            }

            int[] counts = new int[7];
            int sum = 0;
            for (int i = 0; i < dice.Count; i++)
            {
                int value = dice[i];
                if (value < 1 || value > 6)
                {
                    throw new ArgumentOutOfRangeException(nameof(dice), "기본 주사위 눈은 1~6이어야 합니다.");
                }

                counts[value]++;
                sum += value;
            }

            bool fourOfAKind = false;
            bool yacht = false;
            bool hasThree = false;
            bool hasTwo = false;
            for (int value = 1; value <= 6; value++)
            {
                fourOfAKind |= counts[value] >= 4;
                yacht |= counts[value] == 5;
                hasThree |= counts[value] == 3;
                hasTwo |= counts[value] == 2;
            }

            bool fullHouse = yacht || (hasThree && hasTwo);
            bool smallStraight = HasSequence(counts, 1, 4)
                || HasSequence(counts, 2, 5)
                || HasSequence(counts, 3, 6);
            bool largeStraight = HasSequence(counts, 1, 5)
                || HasSequence(counts, 2, 6);

            return new Dictionary<ScoreCategory, int>
            {
                [ScoreCategory.Aces] = counts[1],
                [ScoreCategory.Deuces] = counts[2] * 2,
                [ScoreCategory.Threes] = counts[3] * 3,
                [ScoreCategory.Fours] = counts[4] * 4,
                [ScoreCategory.Fives] = counts[5] * 5,
                [ScoreCategory.Sixes] = counts[6] * 6,
                [ScoreCategory.Choice] = sum,
                [ScoreCategory.FourOfAKind] = fourOfAKind ? sum : 0,
                [ScoreCategory.FullHouse] = fullHouse ? sum : 0,
                [ScoreCategory.SmallStraight] = smallStraight ? 15 : 0,
                [ScoreCategory.LargeStraight] = largeStraight ? 30 : 0,
                [ScoreCategory.Yacht] = yacht ? 50 : 0
            };
        }

        public static bool IsScorable(ScoreCategory category)
        {
            return category >= ScoreCategory.Aces && category <= ScoreCategory.Sixes
                || category >= ScoreCategory.Choice && category <= ScoreCategory.Yacht;
        }

        private static bool HasSequence(IReadOnlyList<int> counts, int first, int last)
        {
            for (int value = first; value <= last; value++)
            {
                if (counts[value] == 0) return false;
            }
            return true;
        }
    }

    public sealed class YachtGameSession
    {
        public const int PlayerCount = 2;
        public const int LastRound = 12;
        public const int MaxRolls = 3;

        private readonly PlayerScoreData[] players;
        private readonly Dictionary<ScoreCategory, int> currentCandidates = new();

        public YachtGameSession(PlayerScoreData playerOne, PlayerScoreData playerTwo)
        {
            players = new[]
            {
                playerOne ?? throw new ArgumentNullException(nameof(playerOne)),
                playerTwo ?? throw new ArgumentNullException(nameof(playerTwo))
            };
            ResetToWaiting();
        }

        public YachtGamePhase Phase { get; private set; }
        public int CurrentPlayerIndex { get; private set; }
        public int CurrentRound { get; private set; }
        public int RollsRemaining { get; private set; }
        public bool HasRolled { get; private set; }
        public IReadOnlyDictionary<ScoreCategory, int> CurrentCandidates => currentCandidates;
        public bool CanRoll => (Phase == YachtGamePhase.TurnReady || Phase == YachtGamePhase.ScoreSelection)
            && RollsRemaining > 0;
        public bool CanKeepDice => Phase == YachtGamePhase.ScoreSelection && HasRolled;

        public void ResetToWaiting()
        {
            ResetScores();
            CurrentPlayerIndex = 0;
            CurrentRound = 1;
            RollsRemaining = MaxRolls;
            HasRolled = false;
            currentCandidates.Clear();
            Phase = YachtGamePhase.WaitingToStart;
        }

        public void StartNewGame()
        {
            ResetScores();
            CurrentPlayerIndex = 0;
            CurrentRound = 1;
            BeginTurn();
        }

        public bool TryBeginRoll()
        {
            if (!CanRoll) return false;
            RollsRemaining--;
            currentCandidates.Clear();
            Phase = YachtGamePhase.Rolling;
            return true;
        }

        public bool CompleteRoll(IReadOnlyList<int> dice)
        {
            if (Phase != YachtGamePhase.Rolling) return false;

            Dictionary<ScoreCategory, int> scores = YachtScoreCalculator.Calculate(dice);
            currentCandidates.Clear();
            foreach (KeyValuePair<ScoreCategory, int> entry in scores)
            {
                if (!IsCategoryFilled(CurrentPlayerIndex, entry.Key))
                {
                    currentCandidates[entry.Key] = entry.Value;
                }
            }

            HasRolled = true;
            Phase = YachtGamePhase.ScoreSelection;
            return true;
        }

        public bool TryCommitScore(ScoreCategory category, out YachtTurnResult result)
        {
            result = default;
            if (Phase != YachtGamePhase.ScoreSelection || !HasRolled) return false;
            if (!currentCandidates.TryGetValue(category, out int score)) return false;
            return CommitAndBeginTransition(category, score, out result);
        }

        /// <summary>
        /// 턴 전환 연출이 끝난 뒤 다음 플레이어/라운드를 활성화합니다.
        /// </summary>
        public bool AdvanceTurnAfterAnimation()
        {
            if (Phase != YachtGamePhase.TurnTransition) return false;

            if (CurrentPlayerIndex == 0)
            {
                CurrentPlayerIndex = 1;
            }
            else
            {
                CurrentPlayerIndex = 0;
                CurrentRound++;
            }

            BeginTurn();
            return true;
        }

        public bool ResolveTimeout(out YachtTurnResult result)
        {
            result = default;
            if (Phase != YachtGamePhase.TurnReady && Phase != YachtGamePhase.ScoreSelection) return false;

            ScoreCategory selected = default;
            int bestScore = int.MinValue;
            bool found = false;

            foreach (ScoreCategory category in YachtScoreCalculator.ScorableCategories)
            {
                if (IsCategoryFilled(CurrentPlayerIndex, category)) continue;
                int candidate = HasRolled && currentCandidates.TryGetValue(category, out int score) ? score : 0;
                if (found && candidate <= bestScore) continue;

                selected = category;
                bestScore = candidate;
                found = true;
            }

            return found && CommitAndBeginTransition(selected, bestScore, out result);
        }

        public bool IsCategoryFilled(int playerIndex, ScoreCategory category)
        {
            if (playerIndex < 0 || playerIndex >= PlayerCount) return true;
            int categoryIndex = (int)category;
            if (categoryIndex >= 0 && categoryIndex <= 5)
            {
                return players[playerIndex].upperScores[categoryIndex] >= 0;
            }
            if (categoryIndex >= 7 && categoryIndex <= 12)
            {
                return players[playerIndex].lowerScores[categoryIndex - 7] >= 0;
            }
            return true;
        }

        public PlayerScoreData GetPlayer(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= PlayerCount) throw new ArgumentOutOfRangeException(nameof(playerIndex));
            return players[playerIndex];
        }

        private bool CommitAndBeginTransition(ScoreCategory category, int score, out YachtTurnResult result)
        {
            int scoredPlayer = CurrentPlayerIndex;
            SetScore(players[scoredPlayer], category, score);

            bool gameEnded = scoredPlayer == PlayerCount - 1 && CurrentRound >= LastRound;
            result = new YachtTurnResult(scoredPlayer, category, score, gameEnded);
            currentCandidates.Clear();
            HasRolled = false;

            if (gameEnded)
            {
                RollsRemaining = 0;
                Phase = YachtGamePhase.GameOver;
                return true;
            }

            Phase = YachtGamePhase.TurnTransition;
            return true;
        }

        private void BeginTurn()
        {
            RollsRemaining = MaxRolls;
            HasRolled = false;
            currentCandidates.Clear();
            Phase = YachtGamePhase.TurnReady;
        }

        private void ResetScores()
        {
            foreach (PlayerScoreData player in players)
            {
                player.Reset();
            }
        }

        private static void SetScore(PlayerScoreData data, ScoreCategory category, int score)
        {
            int categoryIndex = (int)category;
            if (categoryIndex >= 0 && categoryIndex <= 5)
            {
                data.upperScores[categoryIndex] = score;
            }
            else if (categoryIndex >= 7 && categoryIndex <= 12)
            {
                data.lowerScores[categoryIndex - 7] = score;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(category));
            }
            data.RecalculateTotal();
        }
    }
}
