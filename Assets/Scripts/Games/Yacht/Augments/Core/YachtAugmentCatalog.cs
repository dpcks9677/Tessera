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
            // M7.5-R2부터 변형·강화·퀘스트 처리기를 여기에 등록합니다.
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
