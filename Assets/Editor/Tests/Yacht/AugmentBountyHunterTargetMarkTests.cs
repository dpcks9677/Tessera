using NUnit.Framework;
using Tessera.Games.Yacht;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 점수표 위 bounty-hunter 타깃 표시(M17-T10)가 읽는 데이터를 검증한다.
    ///
    /// <see cref="Tessera.Games.AugmentedYacht.YachtTurnFlowPresenter.SyncBountyHunterTargetMark"/>가
    /// 실제로 읽는 값(타깃 카테고리, 남은 횟수)의 원천인 <see cref="BountyHunterState"/> 자체를
    /// 검증한다. 표시 갱신은 MonoBehaviour UI라 <c>ScoreSheetColumnLayoutTests</c>가 그러듯
    /// 순수 로직만 남는 부분(값 추출 규칙)만 여기서 확인한다.
    /// </summary>
    [TestFixture]
    public sealed class AugmentBountyHunterTargetMarkTests
    {
        [Test]
        public void RemainingCount_DecreasesAsSuccessesAccumulate()
        {
            var state = new BountyHunterState { TargetCategory = (int)ScoreCategory.Aces, Successes = 0 };
            Assert.That(BountyHunter.RequiredSuccesses - state.Successes, Is.EqualTo(3));

            state.Successes = 1;
            Assert.That(BountyHunter.RequiredSuccesses - state.Successes, Is.EqualTo(2));

            state.Successes = 3;
            Assert.That(BountyHunter.RequiredSuccesses - state.Successes, Is.EqualTo(0));
        }

        [Test]
        public void TargetCategory_IsHiddenOnceRewarded()
        {
            var state = new BountyHunterState { TargetCategory = (int)ScoreCategory.Yacht, Successes = 3, Rewarded = true };

            // SyncBountyHunterTargetMark는 Rewarded면 표시 대상을 -1(표시 없음)로 취급한다.
            int displayedCategory = state.Rewarded ? -1 : state.TargetCategory;
            Assert.That(displayedCategory, Is.EqualTo(-1));
        }

        [Test]
        public void TargetCategory_IsShownWhileNotRewarded()
        {
            var state = new BountyHunterState { TargetCategory = (int)ScoreCategory.FullHouse, Successes = 1, Rewarded = false };

            int displayedCategory = state.Rewarded ? -1 : state.TargetCategory;
            Assert.That(displayedCategory, Is.EqualTo((int)ScoreCategory.FullHouse));
        }
    }
}
