using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class DuelState : IAugmentState
    {
        public int DuelRound;
        public bool DuelResolved;

        public IAugmentState Clone() => new DuelState
        {
            DuelRound = DuelRound,
            DuelResolved = DuelResolved
        };
    }

    /// <summary>획득한 라운드에 상대보다 점수가 높으면 +10점, 같으면 +5점 보너스를 얻습니다.</summary>
    public sealed class Duel : EnhanceAugment, IOnAugmentSelected
    {
        public override string Id => YachtAugmentRuntime.DuelId;

        public override string DisplayName => "결투";

        public override string Description => "획득한 라운드에 상대보다 점수가 높으면 +10점, 같으면 +5점 보너스를 얻습니다.";

        public void OnSelected(AugmentSelectionContext context)
        {
            var state = context.State<DuelState>();
            state.DuelRound = context.Game.CurrentRound;
            state.DuelResolved = false;

            context.Player.DuelRound = context.Game.CurrentRound;
            context.Player.DuelResolved = false;
        }

        public static void RecordAndResolve(
            YachtGameState state,
            int playerIndex,
            int finalScore,
            ICollection<YachtGameEvent> events)
        {
            if (state.RoundScoresRound != state.CurrentRound)
            {
                state.RoundScoresRound = state.CurrentRound;
                state.RoundScores = new[] { int.MinValue, int.MinValue };
            }
            state.RoundScores[playerIndex] = finalScore;
            if (state.RoundScores[0] == int.MinValue || state.RoundScores[1] == int.MinValue) return;

            for (int owner = 0; owner < state.AugmentPlayers.Length; owner++)
            {
                YachtAugmentPlayerState player = state.AugmentPlayers[owner];
                var duelState = player.States.Find(YachtAugmentRuntime.DuelId) as DuelState;
                bool resolved = duelState?.DuelResolved ?? player.DuelResolved;
                int round = duelState?.DuelRound ?? player.DuelRound;

                bool ownsDuel = false;
                for (int i = 0; i < (player.OwnedIds?.Length ?? 0); i++)
                    if (player.OwnedIds[i] == YachtAugmentRuntime.DuelId) { ownsDuel = true; break; }

                if (!ownsDuel || resolved || round != state.CurrentRound) continue;

                int opponent = owner == 0 ? 1 : 0;
                int bonus = state.RoundScores[owner] > state.RoundScores[opponent] ? 10
                    : state.RoundScores[owner] == state.RoundScores[opponent] ? 5 : 0;

                if (duelState != null) duelState.DuelResolved = true;
                player.DuelResolved = true;

                if (bonus > 0)
                {
                    state.Players[owner].augmentBonusScore += bonus;
                    state.Players[owner].RecalculateTotal();
                    events?.Add(new YachtGameEvent
                    {
                        Type = YachtGameEventType.AugmentTriggered,
                        PlayerIndex = owner,
                        AugmentId = YachtAugmentRuntime.DuelId,
                        Score = bonus,
                        Message = $"결투: +{bonus}점"
                    });
                }
            }
        }
    }
}
