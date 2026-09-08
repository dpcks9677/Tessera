using System;
using System.Collections.Generic;
using Tessera.Games.Yacht;
using UnityEngine;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>스티커 한 장이 붙을 자리입니다.</summary>
    public readonly struct AugmentStickerPlacement
    {
        public AugmentStickerPlacement(ScoreCategory category, string augmentId)
        {
            Category = category;
            AugmentId = augmentId;
        }

        public ScoreCategory Category { get; }
        public string AugmentId { get; }
    }

    /// <summary>
    /// 변형 증강 스티커를 찍는 데 필요한 정보를 모읍니다. 색상코드와 붙일 족보 칸입니다.
    /// 증강마다 정하는 것은 베이스 색상코드 하나뿐이고, 테두리는 계열 공통 앤틱 골드입니다.
    /// 시각 사양은 <c>docs/augmented_yacht_m17_vfx_spec.md</c> §3.1.1을 따릅니다.
    /// </summary>
    public static class AugmentStickerCatalog
    {
        /// <summary>계열 기본 베이스입니다. 예외로 지정하지 않은 변형 증강은 전부 이 색입니다.</summary>
        public static readonly Color32 DefaultBase = AugmentStickerTexture.DefaultBase;

        /// <summary>계열 공통 내부 테두리입니다.</summary>
        public static readonly Color32 BorderColor = AugmentStickerTexture.DefaultBorder;

        /// <summary>
        /// 기본 버건디를 쓰지 않는 증강만 적습니다.
        /// 색을 바꾸는 데는 이유가 있어야 하며, 이유는 사양 문서 §3.1.1의 예외 표에 남깁니다.
        /// </summary>
        private static readonly Dictionary<string, Color32> BaseOverrides = new(StringComparer.Ordinal)
        {
            // 리버스 초이스는 30-합계라 잘 굴릴수록 점수가 낮아진다. 역방향임을 색으로 드러낸다.
            { YachtAugmentRuntime.ReverseChoiceId, new Color32(0x36, 0x4b, 0x6e, 0xff) }
        };

        /// <summary>
        /// 스티커에 찍는 축약 표기입니다. 원본은 `augmented-dice` 프로젝트 `src/augments.json`의
        /// `mark` 태그입니다.
        ///
        /// `head-and-tail`만 원본과 다릅니다. 원본 `mark`는 길이 때문에 `Head &amp; Run`으로 줄여 놓은
        /// 대체어이고, 실제 이름은 `Head &amp; Tail`입니다. 여기 칸은 그 길이가 들어가므로 제 이름을 씁니다.
        ///
        /// 증강 이름(<see cref="YachtAugmentDefinition.DisplayName"/>)은 한글이고 "더블 라지 스트레이트"처럼
        /// 길어서 족보 칸에 들어가지 않습니다. 스티커는 그 칸의 규칙이 무엇으로 바뀌었는지 알리는
        /// 표기이므로, 같은 칸에 들어가는 다른 족보 이름들과 같은 언어·같은 축약 방식을 씁니다.
        /// </summary>
        private static readonly Dictionary<string, string> Marks = new(StringComparer.Ordinal)
        {
            { YachtAugmentRuntime.LuckySevensId, "L. Sevens" },
            { YachtAugmentRuntime.PerfectSquaresId, "P. Squares" },
            { YachtAugmentRuntime.GamblerId, "Gambler" },
            { YachtAugmentRuntime.ThreeOfAKindId, "3 of a Kind" },
            { YachtAugmentRuntime.TinyHouseId, "Tiny House" },
            { YachtAugmentRuntime.TwoPairId, "Two Pair" },
            { YachtAugmentRuntime.HeadAndTailId, "Head & Tail" },
            { YachtAugmentRuntime.EvensId, "Evens" },
            { YachtAugmentRuntime.OddsId, "Odds" },
            { YachtAugmentRuntime.DoubleLargeStraightId, "L. Straight" },
            { YachtAugmentRuntime.PrimeCollectionId, "P. Collection" },
            { YachtAugmentRuntime.DuplexHouseId, "D. House" },
            { YachtAugmentRuntime.MountainId, "Mountain" },
            { YachtAugmentRuntime.HighDiceId, "High Dice" },
            { YachtAugmentRuntime.SecondChoiceId, "2nd Choice" },
            { YachtAugmentRuntime.FibonacciId, "Fib. Numbers" },
            { YachtAugmentRuntime.ReverseChoiceId, "R. Choice" },
            { YachtAugmentRuntime.BlackjackId, "Blackjack" }
        };

        /// <summary>
        /// 스티커에 찍을 표기입니다. 축약어가 없으면 <paramref name="fallback"/>(증강 이름)을 씁니다.
        /// 새 변형 증강을 넣고 <see cref="Marks"/>를 채우지 않으면 그 칸만 한글로 나옵니다.
        /// </summary>
        public static string MarkLabel(string augmentId, string fallback) =>
            augmentId != null && Marks.TryGetValue(augmentId, out string mark) ? mark : fallback;

        /// <summary>스티커를 붙이는 증강인지 봅니다. 변형 계열만 대상입니다.</summary>
        public static bool HasSticker(YachtAugmentDefinition definition) =>
            definition != null && definition.Kind == YachtAugmentKind.Modification;

        public static Color32 BaseColor(string augmentId) =>
            augmentId != null && BaseOverrides.TryGetValue(augmentId, out Color32 color) ? color : DefaultBase;

        /// <summary>
        /// 스티커가 붙을 족보 칸입니다. 정의의 <see cref="YachtAugmentDefinition.Target"/>은 문자열이라
        /// 여기서 한 번만 해석합니다.
        /// </summary>
        public static bool TryGetTargetCategory(YachtAugmentDefinition definition, out ScoreCategory category)
        {
            category = default;
            if (!HasSticker(definition) || string.IsNullOrEmpty(definition.Target)) return false;
            return Enum.TryParse(definition.Target, out category);
        }

        /// <summary>
        /// 한 플레이어가 지금 붙이고 있어야 할 스티커를 모읍니다.
        /// 점수표는 플레이어마다 Categories 열을 하나씩 가지므로 두 사람 것을 각각 부릅니다.
        /// <paramref name="output"/>은 비우고 채웁니다.
        /// </summary>
        public static void CollectFor(
            IReadOnlyYachtGameState state,
            int playerIndex,
            List<AugmentStickerPlacement> output)
        {
            if (output == null) return;
            output.Clear();

            IReadOnlyList<IReadOnlyYachtAugmentPlayerState> players = state?.AugmentPlayers;
            if (players == null || playerIndex < 0 || playerIndex >= players.Count) return;

            IReadOnlyList<string> owned = players[playerIndex]?.OwnedIds;
            if (owned == null) return;

            for (int i = 0; i < owned.Count; i++)
            {
                string augmentId = owned[i];
                YachtAugmentDefinition definition = YachtAugmentRuntime.Lookup(augmentId);
                if (!TryGetTargetCategory(definition, out ScoreCategory category)) continue;

                output.Add(new AugmentStickerPlacement(category, augmentId));
            }
        }
    }
}
