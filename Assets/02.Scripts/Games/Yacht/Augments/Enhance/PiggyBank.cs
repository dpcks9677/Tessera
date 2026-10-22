using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class PiggyBankState : IAugmentState
    {
        public int Balance;

        public IAugmentState Clone() => new PiggyBankState { Balance = Balance };
    }

    /// <summary>턴 종료 시 남은 굴림 횟수당 3원을 저금통에 모아 12원이 될 때마다 +12점 보너스를 얻습니다.</summary>
    public sealed class PiggyBank : EnhanceAugment, IAfterScoreCommit
    {
        public const int PayoutThreshold = 12;
        public const int RollReward = 3;

        public override string Id => YachtAugmentRuntime.PiggyBankId;

        public override string DisplayName => "저금통";

        public override string Description => "턴 종료 시 남은 굴림 횟수당 3원을 저금통에 모아 12원이 될 때마다 +12점 보너스를 얻습니다.";

        private static PiggyBankState GetOrSync(AugmentContext context)
        {
            var state = context.State<PiggyBankState>();
            if (state.Balance == 0 && context.Player.PiggyBankBalance > 0)
            {
                state.Balance = context.Player.PiggyBankBalance;
            }
            return state;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            state.Balance += context.Game.RollsRemaining * RollReward;

            while (state.Balance >= PayoutThreshold)
            {
                state.Balance -= PayoutThreshold;
                context.AddBonus(PayoutThreshold, $"저금통: +{PayoutThreshold}점");
            }

            context.Player.PiggyBankBalance = state.Balance;
        }
    }
}
