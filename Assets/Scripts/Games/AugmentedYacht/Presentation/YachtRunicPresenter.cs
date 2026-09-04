using UnityEngine;
using Tessera.Games.Yacht;
using Tessera.Tabletop;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 룬 슬레이트 창구(M10-T8).
    ///
    /// 추가 턴 적립·소모와 증강에 의한 점수 덮어쓰기, 그리고 룬 디버그 버튼을 한곳에서 다룬다.
    /// 프롭 참조가 비면 씬에서 한 번 찾아 붙이고, 상태가 바뀌면 버튼 라벨을 따라 갱신한다.
    /// </summary>
    public sealed class YachtRunicPresenter : MonoBehaviour
    {
        private RunicSlateMatrix runicSlateMatrix;
        private ParchmentScoreSheet scoreSheet;
        private YachtSceneAssembler.DebugButtons debugButtons;

        public void Bind(RunicSlateMatrix matrix, ParchmentScoreSheet sheet, YachtSceneAssembler.DebugButtons buttons)
        {
            runicSlateMatrix = matrix;
            scoreSheet = sheet;
            debugButtons = buttons;
            ResolveMatrix();
            RefreshDebugLabels();
        }

        public void AdvanceDebugRuneLighting()
        {
            ResolveMatrix();
            runicSlateMatrix?.AdvanceDebugRuneLighting();
            RefreshDebugLabels();
        }

        public void CycleDebugRuneStones()
        {
            ResolveMatrix();
            runicSlateMatrix?.CycleDebugRuneStoneCount();
            RefreshDebugLabels();
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

        /// <summary>증강이 확정한 점수를 점수표에 덮어쓰고 추가 턴을 적립한다.</summary>
        public bool ApplyScoreOverwrite(int playerIndex, ScoreCategory category, int score, int grantedExtraTurns)
        {
            if (scoreSheet == null) scoreSheet = FindFirstObjectByType<ParchmentScoreSheet>();
            if (scoreSheet == null || !scoreSheet.OverwriteScoreFromAugment(playerIndex, category, score)) return false;

            GrantExtraTurns(grantedExtraTurns);
            return true;
        }

        public void RefreshDebugLabels()
        {
            YachtSceneAssembler.UpdateRuneDebugLabels(debugButtons, runicSlateMatrix);
        }

        private void ResolveMatrix()
        {
            if (runicSlateMatrix == null) runicSlateMatrix = FindFirstObjectByType<RunicSlateMatrix>();
            if (runicSlateMatrix == null) return;

            runicSlateMatrix.StateChanged -= RefreshDebugLabels;
            runicSlateMatrix.StateChanged += RefreshDebugLabels;
        }

        private void OnDestroy()
        {
            if (runicSlateMatrix != null) runicSlateMatrix.StateChanged -= RefreshDebugLabels;
        }
    }
}
