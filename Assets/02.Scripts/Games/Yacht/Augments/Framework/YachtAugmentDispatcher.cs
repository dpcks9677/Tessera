using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 보유 증강 중 특정 발동 시점 인터페이스를 구현한 처리기를 골라 호출 순서대로 돌려줍니다.
    /// 정렬은 <c>Order</c>가 먼저이고 같으면 카탈로그 등록 순서를 따르므로 결과가 결정적입니다.
    /// </summary>
    public static class YachtAugmentDispatcher
    {
        public static List<T> Collect<T>(YachtGameState state, int playerIndex) where T : class
        {
            var result = new List<T>();
            if (state?.AugmentPlayers == null || playerIndex < 0 || playerIndex >= state.AugmentPlayers.Length)
                return result;

            Append(state.AugmentPlayers[playerIndex].OwnedIds, result);
            Append(state.GlobalAugmentIds, result);
            if (result.Count > 1) result.Sort(CompareHandlers);
            return result;
        }

        /// <summary>
        /// 보유자를 가리지 않고 판에 존재하는 모든 증강에서 고릅니다. 결투처럼 양쪽 플레이어의
        /// 결과를 함께 보고 판정하는 시점에서는 처리 주체와 보유자가 다르므로 이쪽을 씁니다.
        /// </summary>
        public static List<T> CollectAll<T>(YachtGameState state) where T : class
        {
            var result = new List<T>();
            if (state?.AugmentPlayers == null) return result;

            for (int i = 0; i < state.AugmentPlayers.Length; i++)
                Append(state.AugmentPlayers[i]?.OwnedIds, result);
            Append(state.GlobalAugmentIds, result);
            if (result.Count > 1) result.Sort(CompareHandlers);
            return result;
        }

        private static void Append<T>(IReadOnlyList<string> augmentIds, List<T> result) where T : class
        {
            for (int i = 0; i < (augmentIds?.Count ?? 0); i++)
            {
                IAugmentHandler handler = YachtAugmentCatalog.Find(augmentIds[i]);
                if (handler is T typed && !Contains(result, typed)) result.Add(typed);
            }
        }

        private static bool Contains<T>(List<T> result, T candidate) where T : class
        {
            for (int i = 0; i < result.Count; i++)
                if (ReferenceEquals(result[i], candidate)) return true;
            return false;
        }

        private static int CompareHandlers<T>(T left, T right) where T : class
        {
            var leftHandler = (IAugmentHandler)left;
            var rightHandler = (IAugmentHandler)right;
            int byOrder = leftHandler.Order.CompareTo(rightHandler.Order);
            return byOrder != 0
                ? byOrder
                : YachtAugmentCatalog.IndexOf(leftHandler).CompareTo(YachtAugmentCatalog.IndexOf(rightHandler));
        }
    }
}
