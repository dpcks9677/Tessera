using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tessera.Games.Yacht;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// <see cref="IAugmentProgressText"/>가 퀘스트 증강 전체에 빠짐없이 구현됐는지,
    /// 웹 원본과 같은 줄 문구·판정 경계가 맞는지 검사한다.
    /// </summary>
    [TestFixture]
    public sealed class AugmentProgressTextTests
    {
        private static AugmentProgressQuery Query(int turnsTaken = 0, bool fullHouseFilled = false)
        {
            var player = new YachtAugmentPlayerState { TurnsTaken = turnsTaken };
            var scores = new PlayerScoreData();
            if (fullHouseFilled) scores.lowerFilled[(int)ScoreCategory.FullHouse - 7] = true;
            return new AugmentProgressQuery(player, scores);
        }

        [Test]
        public void AllQuestAugmentStates_ImplementProgressText()
        {
            // 퀘스트 상태 판정 기준: QuestAugment를 상속하는 핸들러(Logic/Augments/Quest/ 소속)는
            // 자기 상태 클래스를 "핸들러 이름 + State" 규약으로 같은 네임스페이스에 둔다
            // (예: BountyHunter → BountyHunterState). 이 규약으로 찾은 상태 타입 전부가
            // IAugmentProgressText를 구현해야, 새 퀘스트 증강 추가 시 구현 누락을 여기서 잡는다.
            var assembly = typeof(QuestAugment).Assembly;
            var questHandlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(QuestAugment).IsAssignableFrom(t));

            var missing = new List<string>();
            foreach (var handlerType in questHandlerTypes)
            {
                var stateType = assembly.GetType($"{handlerType.Namespace}.{handlerType.Name}State");
                Assert.IsNotNull(stateType, $"{handlerType.Name}의 상태 타입을 찾을 수 없습니다 ({handlerType.Name}State 규약).");
                if (!typeof(IAugmentProgressText).IsAssignableFrom(stateType))
                    missing.Add(stateType.Name);
            }

            Assert.IsEmpty(missing, $"IAugmentProgressText 미구현 상태: {string.Join(", ", missing)}");
        }

        [Test]
        public void FastStraight_ProgressLinesAndBoundaryFailure()
        {
            var inProgress = new FastStraightState { SmallScored = true };
            AugmentProgress p1 = inProgress.DescribeProgress(Query(turnsTaken: 3));
            Assert.AreEqual(AugmentProgressOutcome.InProgress, p1.Outcome);
            Assert.AreEqual("8턴 안에 S. Straight 기입", p1.Lines[0].Text);
            Assert.AreEqual("8턴 안에 L. Straight 기입", p1.Lines[1].Text);
            Assert.IsTrue(p1.Lines[0].Done);
            Assert.IsFalse(p1.Lines[1].Done);

            // 경계: TurnsTaken 7이면 아직 실패가 아니다 (7+1=8 <= DeadlineTurn).
            var notYetFailed = new FastStraightState();
            Assert.AreEqual(AugmentProgressOutcome.InProgress, notYetFailed.DescribeProgress(Query(turnsTaken: 7)).Outcome);

            // 경계: TurnsTaken 8이면 실패다.
            var failed = new FastStraightState();
            Assert.AreEqual(AugmentProgressOutcome.Failed, failed.DescribeProgress(Query(turnsTaken: 8)).Outcome);

            var succeeded = new FastStraightState { SmallScored = true, LargeScored = true, Rewarded = true };
            Assert.AreEqual(AugmentProgressOutcome.Succeeded, succeeded.DescribeProgress(Query()).Outcome);
        }

        [Test]
        public void NoTimeToWaste_CountsDownAndReportsFailure()
        {
            var state = new NoTimeToWasteState { RemainingTurns = 1 };
            AugmentProgress progress = state.DescribeProgress(Query());
            Assert.AreEqual("리롤 없이 족보 기입 (2/3)", progress.Lines[0].Text);
            Assert.IsFalse(progress.Lines[0].Done);
            Assert.AreEqual(AugmentProgressOutcome.InProgress, progress.Outcome);

            var failed = new NoTimeToWasteState { Failed = true };
            Assert.AreEqual(AugmentProgressOutcome.Failed, failed.DescribeProgress(Query()).Outcome);

            var rewarded = new NoTimeToWasteState { RemainingTurns = 0, Rewarded = true };
            AugmentProgress done = rewarded.DescribeProgress(Query());
            Assert.IsTrue(done.Lines[0].Done);
            Assert.AreEqual(AugmentProgressOutcome.Succeeded, done.Outcome);
        }

        [Test]
        public void StepByStep_TracksCategoryIndexAsStepProgress()
        {
            var state = new StepByStepState { CategoryIndex = 2 };
            AugmentProgress progress = state.DescribeProgress(Query());
            Assert.AreEqual("Aces부터 Sixes까지 순서대로 기입 (2/6)", progress.Lines[0].Text);
            Assert.IsFalse(progress.Lines[0].Done);
            Assert.AreEqual(AugmentProgressOutcome.InProgress, progress.Outcome);

            var failed = new StepByStepState { Failed = true };
            Assert.AreEqual(AugmentProgressOutcome.Failed, failed.DescribeProgress(Query()).Outcome);

            var rewarded = new StepByStepState { CategoryIndex = 6, Rewarded = true };
            Assert.AreEqual(AugmentProgressOutcome.Succeeded, rewarded.DescribeProgress(Query()).Outcome);
        }

        [Test]
        public void Holdout_FailsWhenFullHouseAlreadyFilledWithoutReward()
        {
            var inProgress = new HoldoutState();
            Assert.AreEqual(AugmentProgressOutcome.InProgress, inProgress.DescribeProgress(Query()).Outcome);

            // 경계: 보상 없이 Full House 칸만 채워지면 실패다.
            var failed = new HoldoutState();
            AugmentProgress failedProgress = failed.DescribeProgress(Query(fullHouseFilled: true));
            Assert.AreEqual(AugmentProgressOutcome.Failed, failedProgress.Outcome);
            Assert.AreEqual("9턴 이후에 Full House 기입", failedProgress.Lines[0].Text);

            var rewarded = new HoldoutState { Rewarded = true };
            AugmentProgress rewardedProgress = rewarded.DescribeProgress(Query(fullHouseFilled: true));
            Assert.AreEqual(AugmentProgressOutcome.Succeeded, rewardedProgress.Outcome);
            Assert.IsTrue(rewardedProgress.Lines[0].Done);
        }

        [Test]
        public void CautiousStraight_TwoLinesTrackOrderAndFailure()
        {
            var state = new CautiousStraightState { SmallScored = true };
            AugmentProgress progress = state.DescribeProgress(Query());
            Assert.AreEqual("S. Straight를 L. Straight 보다 먼저 기입", progress.Lines[0].Text);
            Assert.AreEqual("L. Straight 기입", progress.Lines[1].Text);
            Assert.IsTrue(progress.Lines[0].Done);
            Assert.IsFalse(progress.Lines[1].Done);

            var failed = new CautiousStraightState { Failed = true };
            Assert.AreEqual(AugmentProgressOutcome.Failed, failed.DescribeProgress(Query()).Outcome);

            var rewarded = new CautiousStraightState { SmallScored = true, Rewarded = true };
            Assert.AreEqual(AugmentProgressOutcome.Succeeded, rewarded.DescribeProgress(Query()).Outcome);
        }

        [Test]
        public void EveryLittleCounts_TracksCountWithoutFailure()
        {
            var state = new EveryLittleCountsState { Count = 3 };
            AugmentProgress progress = state.DescribeProgress(Query());
            Assert.AreEqual("족보 기입에 사용한 1의 눈 모으기 (3/7)", progress.Lines[0].Text);
            Assert.AreEqual(AugmentProgressOutcome.InProgress, progress.Outcome);

            var rewarded = new EveryLittleCountsState { Count = 7, Rewarded = true };
            AugmentProgress done = rewarded.DescribeProgress(Query());
            Assert.IsTrue(done.Lines[0].Done);
            Assert.AreEqual(AugmentProgressOutcome.Succeeded, done.Outcome);
        }

        [Test]
        public void Copycat_ShowsSpecialClearSuffixWhenRewardedBeforeThreeCounts()
        {
            var state = new CopycatState { Count = 1 };
            AugmentProgress progress = state.DescribeProgress(Query());
            Assert.AreEqual("상대방이 이미 기입한 족보와 동일한 족보 기입 (1/3)", progress.Lines[0].Text);

            // 경계: 동점 즉시 완료라 3회 미만에서 Rewarded==true가 된다. 이때만 "(조건 달성!)"이 붙는다.
            var special = new CopycatState { Count = 1, Rewarded = true };
            AugmentProgress specialProgress = special.DescribeProgress(Query());
            Assert.AreEqual("상대방이 이미 기입한 족보와 동일한 족보 기입 (1/3) (조건 달성!)", specialProgress.Lines[0].Text);
            Assert.AreEqual(AugmentProgressOutcome.Succeeded, specialProgress.Outcome);

            var normalComplete = new CopycatState { Count = 3, Rewarded = true };
            AugmentProgress normalProgress = normalComplete.DescribeProgress(Query());
            Assert.AreEqual("상대방이 이미 기입한 족보와 동일한 족보 기입 (3/3)", normalProgress.Lines[0].Text);
        }

        [Test]
        public void Doubling_TracksSingleRewardStep()
        {
            var inProgress = new DoublingState();
            Assert.AreEqual("동일한 점수로 족보를 두 번 등록 (0/1)", inProgress.DescribeProgress(Query()).Lines[0].Text);

            var rewarded = new DoublingState { Rewarded = true };
            AugmentProgress progress = rewarded.DescribeProgress(Query());
            Assert.AreEqual("동일한 점수로 족보를 두 번 등록 (1/1)", progress.Lines[0].Text);
            Assert.AreEqual(AugmentProgressOutcome.Succeeded, progress.Outcome);
        }

        [Test]
        public void Nozdormu_CountsDownToTargetTurnBoundary()
        {
            var state = new NozdormuState { TargetTurn = 5 };

            // 경계: TargetTurn-1턴 진행했으면 아직 1턴 남는다.
            AugmentProgress before = state.DescribeProgress(Query(turnsTaken: 4));
            Assert.AreEqual("턴 타이머가 15초인 상태로 플레이하기 (1턴 남음!)", before.Lines[0].Text);

            // 경계: TargetTurn에 도달하면 0턴 남는다.
            AugmentProgress at = state.DescribeProgress(Query(turnsTaken: 5));
            Assert.AreEqual("턴 타이머가 15초인 상태로 플레이하기 (0턴 남음!)", at.Lines[0].Text);

            var rewarded = new NozdormuState { TargetTurn = 5, Rewarded = true };
            Assert.AreEqual(AugmentProgressOutcome.Succeeded, rewarded.DescribeProgress(Query(turnsTaken: 5)).Outcome);
        }

        [Test]
        public void BountyHunter_ShowsTargetNoteUntilCompleted()
        {
            var withTarget = new BountyHunterState { Successes = 1, TargetCategory = (int)ScoreCategory.FullHouse };
            AugmentProgress progress = withTarget.DescribeProgress(Query());
            Assert.AreEqual("타겟으로 지정된 족보를 3회 기입하기 (1/3)", progress.Lines[0].Text);
            Assert.AreEqual(2, progress.Lines.Count);
            Assert.IsTrue(progress.Lines[1].IsTargetNote);
            Assert.AreEqual("Full House", progress.Lines[1].Text);

            var noTarget = new BountyHunterState { TargetCategory = -1 };
            Assert.AreEqual("미지정", noTarget.DescribeProgress(Query()).Lines[1].Text);

            var completed = new BountyHunterState { Successes = 3 };
            AugmentProgress completedProgress = completed.DescribeProgress(Query());
            Assert.AreEqual(1, completedProgress.Lines.Count);
            Assert.AreEqual(AugmentProgressOutcome.Succeeded, completedProgress.Outcome);
        }

        [Test]
        public void Prophet_ShowsWaitingWhenTargetsEmptyAndCompletesAtZero()
        {
            var waiting = new ProphetState { TurnsRemaining = 2, Targets = System.Array.Empty<int>() };
            AugmentProgress progress = waiting.DescribeProgress(Query());
            Assert.AreEqual("제시 숫자 [대기 중]와 같은 점수 기입 (2턴 남음)", progress.Lines[0].Text);
            Assert.AreEqual(AugmentProgressOutcome.InProgress, progress.Outcome);

            var withTargets = new ProphetState { TurnsRemaining = 1, Targets = new[] { 3, 12, 25 } };
            AugmentProgress withTargetsProgress = withTargets.DescribeProgress(Query());
            Assert.AreEqual("제시 숫자 [3, 12, 25]와 같은 점수 기입 (1턴 남음)", withTargetsProgress.Lines[0].Text);

            var done = new ProphetState { TurnsRemaining = 0 };
            Assert.AreEqual(AugmentProgressOutcome.Succeeded, done.DescribeProgress(Query()).Outcome);
        }

        [Test]
        public void ScoreCategoryNames_CoversAllScorableCategoriesAcrossTheBonusGap()
        {
            Assert.AreEqual("Aces", ScoreCategoryNames.Get(ScoreCategory.Aces));
            Assert.AreEqual("Sixes", ScoreCategoryNames.Get(ScoreCategory.Sixes));
            Assert.AreEqual("", ScoreCategoryNames.Get(ScoreCategory.Bonus));
            // Bonus=6 틈 때문에 Choice(7)가 밀리지 않는지 확인한다.
            Assert.AreEqual("Choice", ScoreCategoryNames.Get(ScoreCategory.Choice));
            Assert.AreEqual("4 of a Kind", ScoreCategoryNames.Get(ScoreCategory.FourOfAKind));
            Assert.AreEqual("Full House", ScoreCategoryNames.Get(ScoreCategory.FullHouse));
            Assert.AreEqual("S. Straight", ScoreCategoryNames.Get(ScoreCategory.SmallStraight));
            Assert.AreEqual("L. Straight", ScoreCategoryNames.Get(ScoreCategory.LargeStraight));
            Assert.AreEqual("Yacht", ScoreCategoryNames.Get(ScoreCategory.Yacht));
            Assert.AreEqual("", ScoreCategoryNames.Get(ScoreCategory.Total));
        }

        [Test]
        public void Find_ReturnsNullAndDoesNotCreateState_ForUnknownId()
        {
            var store = new AugmentStateStore();

            IAugmentState first = store.Find("no-such-augment");
            IAugmentState second = store.Find("no-such-augment");

            Assert.IsNull(first);
            Assert.IsNull(second);
        }
    }
}
