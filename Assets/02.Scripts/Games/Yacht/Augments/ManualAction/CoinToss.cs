using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class CoinTossState : IAugmentState, IAugmentProgressText
    {
        public bool IsUsed;

        /// <summary>동전 i의 앞면 여부를 비트 i에 담습니다.</summary>
        public int Faces;

        public int Heads;

        public IAugmentState Clone() => new CoinTossState { IsUsed = IsUsed, Faces = Faces, Heads = Heads };

        public AugmentProgress DescribeProgress(in AugmentProgressQuery query)
        {
            if (!IsUsed) return new AugmentProgress(AugmentProgressOutcome.InProgress, null);

            string text = $"앞면 {Heads}개 · {DescribeEffect(Heads)}";
            AugmentProgressOutcome outcome = Heads == 0 ? AugmentProgressOutcome.Failed : AugmentProgressOutcome.Succeeded;
            var lines = new[] { new AugmentProgressLine(text, IsUsed) };
            return new AugmentProgress(outcome, lines);
        }

        private static string DescribeEffect(int heads) => heads switch
        {
            0 => "-5점",
            1 => "보너스 +3점",
            2 => "리롤 +1",
            3 => "보너스 기준 57",
            _ => ""
        };
    }

    /// <summary>게임당 한 번, 첫 굴림 후 동전 3개를 던져 앞면 수에 따라 효과가 갈립니다.</summary>
    public sealed class CoinToss : EnhanceAugment, IManualActionAugment
    {
        public const int PenaltyScore = -5;
        public const int BonusScore = 3;
        public const int LoweredUpperBonusThreshold = 57;

        public override string Id => YachtAugmentRuntime.CoinTossId;

        public override string DisplayName => "코인 토스";

        public override string Description =>
            "게임당 한 번, 첫 굴림 후 동전 3개를 던져 앞면 수에 따라 효과가 갈립니다. (0개: -5점 / 1개: 보너스 +3점 / 2개: 리롤 +1 / 3개: 보너스 기준 57)";

        public YachtGamePhase RequiredPhase => YachtGamePhase.ScoreSelection;

        public bool RerollsDice => false;

        public bool CanUse(AugmentActionContext context, out YachtCommandErrorCode code, out string message)
        {
            code = YachtCommandErrorCode.None;
            message = null;
            if (!context.Owns(Id))
            {
                code = YachtCommandErrorCode.AugmentRequired;
                message = "코인 토스 증강을 보유하지 않았습니다.";
                return false;
            }
            var state = context.State<CoinTossState>();
            if (state.IsUsed)
            {
                code = YachtCommandErrorCode.AugmentAlreadyUsed;
                message = "코인 토스를 이미 사용했습니다.";
                return false;
            }
            if (!context.Game.HasRolled)
            {
                code = YachtCommandErrorCode.RollRequired;
                message = "첫 굴림 후 사용할 수 있습니다.";
                return false;
            }
            return true;
        }

        public void Use(AugmentActionContext context)
        {
            var state = context.State<CoinTossState>();

            int faces = 0;
            int heads = 0;
            for (int i = 0; i < 3; i++)
            {
                if (context.Random.NextBool())
                {
                    faces |= 1 << i;
                    heads++;
                }
            }
            state.Faces = faces;
            state.Heads = heads;

            switch (heads)
            {
                case 0:
                    context.AddBonus(PenaltyScore, "코인 토스: 앞면 0개 — 보너스 -5점");
                    break;
                case 1:
                    context.AddBonus(BonusScore, "코인 토스: 앞면 1개 — 보너스 +3점");
                    break;
                case 2:
                    context.Game.RollsRemaining++;
                    context.Game.BonusRolls++;
                    break;
                case 3:
                    PlayerScoreData score = context.Score;
                    score.upperBonusThreshold = Math.Min(score.upperBonusThreshold, LoweredUpperBonusThreshold);
                    score.RecalculateTotal();
                    break;
            }

            state.IsUsed = true;
        }
    }
}
