using UnityEngine;

namespace Tessera.Dice
{
    public sealed class DiceKeepTarget : MonoBehaviour
    {
        public int Index;

        /// <summary>
        /// 이 주사위에 적용된 화면 종류(M7-T5).
        ///
        /// 재질·형상·면 값 표가 모두 이 값을 기준으로 갈린다. 주사위마다 달라질 수 있으므로
        /// 풀이 아니라 주사위 오브젝트가 들고 있어야 <c>Transform</c> 하나만으로 판정할 수 있다.
        /// </summary>
        public DieType Type = DieType.Normal;
    }
}
