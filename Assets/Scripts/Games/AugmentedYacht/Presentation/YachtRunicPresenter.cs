using UnityEngine;
using Tessera.Games.Yacht;
using Tessera.Tabletop;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 룬 슬레이트 창구(M10-T8).
    ///
    /// 추가 턴 적립·소모와 증강에 의한 점수 덮어쓰기, 그리고 룬 디버그 조작을 한곳에서 다룬다.
    /// 프롭 참조가 비면 씬에서 한 번 찾아 붙인다. 현재 진행도는 <see cref="YachtDebugPanel"/>이 직접 읽는다.
    /// </summary>
    public sealed class YachtRunicPresenter : MonoBehaviour
    {
        private RunicSlateMatrix runicSlateMatrix;
        private ParchmentScoreSheet scoreSheet;

        public void Bind(RunicSlateMatrix matrix, ParchmentScoreSheet sheet)
        {
            runicSlateMatrix = matrix;
            scoreSheet = sheet;
            ResolveMatrix();
        }

        public void AdvanceDebugRuneLighting()
        {
            ResolveMatrix();
            runicSlateMatrix?.AdvanceDebugRuneLighting();
        }

        public void CycleDebugRuneStones()
        {
            ResolveMatrix();
            runicSlateMatrix?.CycleDebugRuneStoneCount();
        }

        public void GrantExtraTurns(int amount)
        {
            ResolveMatrix();
            runicSlateMatrix?.GrantExtraTurns(amount);
        }

        public bool ConsumeExtraTurn()
        {
            ResolveMatrix();
            return runicSlateMatrix != null && runicSlateMatrix.ConsumeExtraTurn();
        }

        private void ResolveMatrix()
        {
            if (runicSlateMatrix == null) runicSlateMatrix = FindFirstObjectByType<RunicSlateMatrix>();
        }
    }
}
