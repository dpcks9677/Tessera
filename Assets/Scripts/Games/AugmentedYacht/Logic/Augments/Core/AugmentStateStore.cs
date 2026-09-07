using System;

namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 플레이어 한 명이 보유한 증강별 진행 상태를 증강 ID로 보관합니다.
    /// 삽입 순서를 유지하는 병렬 배열이라 복제와 순회 결과가 결정적입니다.
    /// Unity 직렬화는 인터페이스 배열을 다루지 못하므로 이 저장소는 직렬화 대상이 아닙니다.
    /// 네트워크 스냅샷이 필요한 M9에서 <c>{id, typeTag, payload}</c> 형태로 평탄화합니다.
    /// </summary>
    public sealed class AugmentStateStore
    {
        private string[] ids = Array.Empty<string>();
        private IAugmentState[] states = Array.Empty<IAugmentState>();

        public int Count => ids.Length;

        public string IdAt(int index) => ids[index];

        public IAugmentState StateAt(int index) => states[index];

        /// <summary>해당 증강의 상태를 반환하고, 없으면 만들어서 보관합니다.</summary>
        public T GetOrCreate<T>(string augmentId) where T : class, IAugmentState, new()
        {
            int index = IndexOf(augmentId);
            if (index >= 0)
            {
                if (states[index] is T existing) return existing;
                throw new InvalidOperationException(
                    $"증강 '{augmentId}'의 상태가 이미 {states[index].GetType().Name}으로 보관되어 있어 {typeof(T).Name}으로 읽을 수 없습니다.");
            }

            var created = new T();
            Array.Resize(ref ids, ids.Length + 1);
            Array.Resize(ref states, states.Length + 1);
            ids[ids.Length - 1] = augmentId;
            states[states.Length - 1] = created;
            return created;
        }

        /// <summary>상태를 만들지 않고 조회합니다. 없으면 null입니다.</summary>
        public IAugmentState Find(string augmentId)
        {
            int index = IndexOf(augmentId);
            return index >= 0 ? states[index] : null;
        }

        public void Remove(string augmentId)
        {
            int index = IndexOf(augmentId);
            if (index < 0) return;
            var nextIds = new string[ids.Length - 1];
            var nextStates = new IAugmentState[states.Length - 1];
            for (int i = 0, write = 0; i < ids.Length; i++)
            {
                if (i == index) continue;
                nextIds[write] = ids[i];
                nextStates[write] = states[i];
                write++;
            }
            ids = nextIds;
            states = nextStates;
        }

        public AugmentStateStore Clone()
        {
            var clone = new AugmentStateStore
            {
                ids = (string[])ids.Clone(),
                states = new IAugmentState[states.Length]
            };
            for (int i = 0; i < states.Length; i++) clone.states[i] = states[i]?.Clone();
            return clone;
        }

        private int IndexOf(string augmentId)
        {
            for (int i = 0; i < ids.Length; i++)
                if (string.Equals(ids[i], augmentId, StringComparison.Ordinal)) return i;
            return -1;
        }
    }
}
