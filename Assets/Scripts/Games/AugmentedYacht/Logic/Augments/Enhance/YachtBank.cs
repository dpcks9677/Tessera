using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class YachtBankState : IAugmentState
    {
        public int RemainingTurns = 3;
        public int Balance;
        public bool PayoutPending;
        public bool Paid;

        public IAugmentState Clone() => new YachtBankState
        {
            RemainingTurns = RemainingTurns,
            Balance = Balance,
            PayoutPending = PayoutPending,
            Paid = Paid
        };
    }

    /// <summary>3턴 동안 가장 왼쪽 킵 주사위를 점수에서 제외해 최대 15까지 저축하고 다음 내 턴에 받습니다.</summary>
    public sealed class YachtBank : EnhanceAugment, IOnAugmentSelected, IOnTurnStarted, IScoringDiceFilter, IAfterScoreCommit
    {
        public const int MaxBalance = 15;

        public override string Id => YachtAugmentRuntime.YachtBankId;

        public override string DisplayName => "요트 뱅크";

        public override string Description => "3턴 동안 가장 왼쪽 킵 주사위를 점수에서 제외해 최대 15까지 저축하고 다음 내 턴에 받습니다.";

        public void OnSelected(AugmentSelectionContext context)
        {
            var state = context.State<YachtBankState>();
            state.RemainingTurns = 3;
            state.Balance = 0;
            state.PayoutPending = false;
            state.Paid = false;

            context.Player.YachtBankRemainingTurns = 3;
            context.Player.YachtBankBalance = 0;
            context.Player.YachtBankPayoutPending = false;
            context.Player.YachtBankPaid = false;
        }

        private static YachtBankState GetOrSync(AugmentContext context)
        {
            var state = context.State<YachtBankState>();
            if (state.RemainingTurns == 0 && !state.PayoutPending && !state.Paid && context.Player.YachtBankRemainingTurns > 0)
            {
                state.RemainingTurns = context.Player.YachtBankRemainingTurns;
                state.Balance = context.Player.YachtBankBalance;
                state.PayoutPending = context.Player.YachtBankPayoutPending;
                state.Paid = context.Player.YachtBankPaid;
            }
            return state;
        }

        public void OnTurnStarted(AugmentTurnContext context)
        {
            var state = GetOrSync(context);
            if (state.PayoutPending && !state.Paid)
            {
                state.Paid = true;
                state.PayoutPending = false;
                context.AddBonus(state.Balance, $"요트 뱅크 지급: +{state.Balance}점");

                context.Player.YachtBankPaid = true;
                context.Player.YachtBankPayoutPending = false;
            }
        }

        public IReadOnlyList<YachtDieState> FilterScoringDice(AugmentQueryContext context, IReadOnlyList<YachtDieState> dice)
        {
            var state = GetOrSync(context);
            if (state.RemainingTurns <= 0) return dice;

            int excludedIndex = FindLowestKeptIndex(dice);
            if (excludedIndex < 0) return dice;

            var result = new List<YachtDieState>(dice.Count - 1);
            for (int i = 0; i < dice.Count; i++)
            {
                if (i != excludedIndex) result.Add(dice[i]);
            }
            return result;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            if (state.RemainingTurns <= 0) return;

            int lowestSlot = int.MaxValue;
            YachtDieState banked = null;
            for (int i = 0; i < (context.Dice?.Count ?? 0); i++)
            {
                YachtDieState die = context.Dice[i];
                if (!die.IsKept) continue;
                int slot = die.KeepSlotIndex >= 0 ? die.KeepSlotIndex : i;
                if (slot >= lowestSlot) continue;
                lowestSlot = slot;
                banked = die;
            }

            if (banked != null)
            {
                state.Balance = Math.Min(MaxBalance, state.Balance + banked.Value);
            }

            state.RemainingTurns--;
            if (state.RemainingTurns <= 0)
            {
                state.PayoutPending = true;
            }

            context.Player.YachtBankBalance = state.Balance;
            context.Player.YachtBankRemainingTurns = state.RemainingTurns;
            context.Player.YachtBankPayoutPending = state.PayoutPending;
        }

        private static int FindLowestKeptIndex(IReadOnlyList<YachtDieState> dice)
        {
            int excludedIndex = -1;
            int lowestSlot = int.MaxValue;
            for (int i = 0; i < (dice?.Count ?? 0); i++)
            {
                if (!dice[i].IsKept) continue;
                int slot = dice[i].KeepSlotIndex >= 0 ? dice[i].KeepSlotIndex : i;
                if (slot >= lowestSlot) continue;
                lowestSlot = slot;
                excludedIndex = i;
            }
            return excludedIndex;
        }
    }
}
