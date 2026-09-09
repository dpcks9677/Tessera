using System.Collections.Generic;
using Tessera.Games.Yacht;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>변형 증강 스티커가 낼 수 있는 연출입니다. 사양 문서 §3.1.1의 `S0`·`S2`와 같습니다.</summary>
    public enum AugmentVfxCue
    {
        /// <summary>`S0` 부착. 증강을 얻은 순간 스티커가 대상 칸에 눌러 붙습니다.</summary>
        StickerAttach,

        /// <summary>`S2` 낙인. 그 칸으로 점수를 확정한 순간 스티커가 눌립니다.</summary>
        StickerStamp,

        /// <summary>연기가 주사위를 가린 사이에 눈이 바뀐다. 56 `dice-alchemy` 전용이다.</summary>
        DiceSmokeSwap
    }

    public readonly struct AugmentVfxRequest
    {
        public AugmentVfxRequest(AugmentVfxCue cue, int playerIndex, string augmentId, ScoreCategory category)
        {
            Cue = cue;
            PlayerIndex = playerIndex;
            AugmentId = augmentId;
            Category = category;
        }

        public AugmentVfxCue Cue { get; }
        public int PlayerIndex { get; }
        public string AugmentId { get; }
        public ScoreCategory Category { get; }
    }

    /// <summary>
    /// 권위가 낸 이벤트 배열을 연출 재생 요청으로 옮깁니다.
    ///
    /// <see cref="UnityEngine.MonoBehaviour"/>가 아닌 순수 클래스라 화면 없이 검증할 수 있습니다.
    /// 변형 증강은 발동 이벤트(<c>AugmentTriggered</c>)를 내지 않으므로 여기서도 그것을 보지 않습니다.
    /// 획득(<c>AugmentSelected</c>·<c>AugmentReplaced</c>)과 점수 확정(<c>ScoreCommitted</c>)에 더해,
    /// 수동 행동 발동(<c>AugmentActionUsed</c>)도 봅니다.
    /// 근거는 <c>docs/augmented_yacht_m17_vfx_spec.md</c> §3.1.1·§9.5에 있습니다.
    /// </summary>
    public static class AugmentVfxPlanner
    {
        /// <summary>
        /// 이벤트 순서를 그대로 지키며 요청을 <paramref name="output"/> 뒤에 덧붙입니다.
        /// 비우지 않으므로 여러 명령의 결과를 이어 붙일 수 있습니다.
        /// </summary>
        public static void Plan(
            IReadOnlyList<YachtGameEvent> events,
            IReadOnlyYachtGameState state,
            List<AugmentVfxRequest> output)
        {
            if (events == null || output == null) return;

            for (int i = 0; i < events.Count; i++)
            {
                YachtGameEvent gameEvent = events[i];
                if (gameEvent == null) continue;

                switch (gameEvent.Type)
                {
                    case YachtGameEventType.AugmentSelected:
                        TryAddAttach(gameEvent.AugmentId, gameEvent.PlayerIndex, output);
                        break;

                    // 랜덤 박스가 준 증강이 변형이면 그것도 새로 붙는다. AugmentId는 랜덤 박스 자신이다.
                    case YachtGameEventType.AugmentReplaced:
                        TryAddAttach(gameEvent.RelatedAugmentId, gameEvent.PlayerIndex, output);
                        break;

                    case YachtGameEventType.ScoreCommitted:
                        TryAddStamp(gameEvent, state, output);
                        break;

                    case YachtGameEventType.AugmentActionUsed:
                        TryAddDiceSmokeSwap(gameEvent, output);
                        break;
                }
            }
        }

        private static void TryAddAttach(string augmentId, int playerIndex, List<AugmentVfxRequest> output)
        {
            YachtAugmentDefinition definition = YachtAugmentRuntime.Lookup(augmentId);
            if (!AugmentStickerCatalog.TryGetTargetCategory(definition, out ScoreCategory category)) return;
            output.Add(new AugmentVfxRequest(AugmentVfxCue.StickerAttach, playerIndex, augmentId, category));
        }

        /// <summary>확정한 칸을 교체한 변형 증강을 보유 목록에서 찾습니다. 없으면 연출도 없습니다.</summary>
        private static void TryAddStamp(
            YachtGameEvent gameEvent,
            IReadOnlyYachtGameState state,
            List<AugmentVfxRequest> output)
        {
            IReadOnlyList<string> owned = OwnedIds(state, gameEvent.PlayerIndex);
            if (owned == null) return;

            for (int i = 0; i < owned.Count; i++)
            {
                YachtAugmentDefinition definition = YachtAugmentRuntime.Lookup(owned[i]);
                if (!AugmentStickerCatalog.TryGetTargetCategory(definition, out ScoreCategory category)) continue;
                if (category != gameEvent.Category) continue;

                output.Add(new AugmentVfxRequest(
                    AugmentVfxCue.StickerStamp, gameEvent.PlayerIndex, owned[i], category));
                return;
            }
        }

        /// <summary>56 `dice-alchemy`가 눈을 바꾸는 순간에만 연기 가림 요청을 냅니다. 대상 주사위는 싣지 않고,
        /// 소비하는 쪽이 상태의 <c>IsKept</c>로 판정합니다. <see cref="ScoreCategory"/>는 이 큐에서 의미가 없어 기본값을 넣습니다.</summary>
        private static void TryAddDiceSmokeSwap(YachtGameEvent gameEvent, List<AugmentVfxRequest> output)
        {
            if (!string.Equals(gameEvent.AugmentId, YachtAugmentRuntime.DiceAlchemyId, System.StringComparison.Ordinal)) return;
            output.Add(new AugmentVfxRequest(AugmentVfxCue.DiceSmokeSwap, gameEvent.PlayerIndex, gameEvent.AugmentId, default));
        }

        private static IReadOnlyList<string> OwnedIds(IReadOnlyYachtGameState state, int playerIndex)
        {
            IReadOnlyList<IReadOnlyYachtAugmentPlayerState> players = state?.AugmentPlayers;
            if (players == null || playerIndex < 0 || playerIndex >= players.Count) return null;
            return players[playerIndex]?.OwnedIds;
        }
    }
}
