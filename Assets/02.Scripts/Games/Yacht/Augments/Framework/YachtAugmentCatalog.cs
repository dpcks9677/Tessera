using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 활성 증강 처리기의 등록 목록입니다. 리플렉션으로 찾지 않고 명시적으로 나열해
    /// IL2CPP/AOT 빌드에서도 동작을 보장합니다. 증강을 추가할 때는 처리기 파일 하나를
    /// 만들고 아래 배열에 한 줄을 더합니다.
    /// </summary>
    public static class YachtAugmentCatalog
    {
        private static readonly IAugmentHandler[] Handlers =
        {
            // 변형 18종. 등록 순서가 드래프트 후보와 정의 목록의 노출 순서가 됩니다.
            new LuckySevens(), // 럭키 세븐
            new PerfectSquares(), // 퍼펙트 스퀘어
            new Gambler(), // 갬블러
            new ThreeOfAKind(), // 쓰리 오브 어 카인드
            new TinyHouse(), // 타이니 하우스
            new TwoPair(), // 투 페어
            new HeadAndTail(), // 머리와 몸통
            new Evens(), // 에번스
            new Odds(), // 오즈
            new DoubleLargeStraight(), // 더블 라지 스트레이트
            new PrimeCollection(), // 프라임 컬렉션
            new DuplexHouse(), // 땅콩주택
            new Mountain(), // 마운틴
            new HighDice(), // 하이 다이스
            new SecondChoice(), // 두 번째 초이스
            new FibonacciNumbers(), // 피보나치 넘버즈
            new ReverseChoice(), // 리버스 초이스
            new Blackjack21(), // 블랙잭 21

            // 강화 11종 (주사위 6 + 상시/특수 5)
            new WeightedDice(), // 묵직한 주사위
            new GoldenDie(), // 황금 주사위
            new Octahedron(), // 8면 주사위
            new PromotionDie(), // 프로모션 주사위
            new CoupleDice(), // 커플 주사위
            new SevensDice(), // 세븐스 다이스
            new YachtBank(), // 요트 뱅크
            new Momentum(), // 추진력
            new Duel(), // 결투
            new PiggyBank(), // 저금통
            new RandomBox(), // 랜덤 박스

            // 퀘스트 11종
            new NoTimeToWaste(), // 낭비할 시간 없다
            new BountyHunter(), // 현상금 사냥꾼
            new StepByStep(), // 차근차근
            new FastStraight(), // 재빠른 스트레이트
            new Holdout(), // 알박기
            new CautiousStraight(), // 신중한 스트레이트
            new EveryLittleCounts(), // 티끌 모아 태산
            new Copycat(), // 카피캣
            new Prophet(), // 예지자
            new Doubling(), // 더블링
            new Nozdormu(), // 노즈도르무

            // 수동 행동 6종
            new TableFlip(), // 판 뒤집기
            new EquivalentExchange(), // 등가교환
            new Gambit(), // 갬빗
            new DoubleDown(), // 더블 다운
            new DiceAlchemy(), // 주사위 연금술
            new CoinToss() // 코인 토스
        };

        public static IReadOnlyList<IAugmentHandler> All => Handlers;

        public static IAugmentHandler Find(string augmentId)
        {
            for (int i = 0; i < Handlers.Length; i++)
                if (string.Equals(Handlers[i].Id, augmentId, StringComparison.Ordinal)) return Handlers[i];
            return null;
        }

        /// <summary>등록 목록의 순서를 반환합니다. 같은 <c>Order</c>일 때의 정렬 기준입니다.</summary>
        internal static int IndexOf(IAugmentHandler handler)
        {
            for (int i = 0; i < Handlers.Length; i++)
                if (ReferenceEquals(Handlers[i], handler)) return i;
            return int.MaxValue;
        }
    }
}
