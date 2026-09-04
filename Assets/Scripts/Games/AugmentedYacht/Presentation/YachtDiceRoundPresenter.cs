using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tessera.Core;
using Tessera.Dice;
using Tessera.Games.Yacht;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 한 턴 동안의 주사위 화면 상태를 맡는다(M10-T8).
    ///
    /// 권위 계층이 확정한 눈과 킵 여부를 화면 쪽 사본으로 들고, 굴림 궤적 재생과 킵 정렬
    /// 애니메이션을 돌린다. 규칙 판정은 하지 않는다. 무엇을 굴릴지·킵해도 되는지는
    /// <see cref="YachtTurnFlowPresenter"/>가 권위 계층에 물어 정한 뒤 여기로 내려보낸다.
    /// </summary>
    public sealed class YachtDiceRoundPresenter : MonoBehaviour
    {
        private readonly List<GameObject> activeDice = new();
        private readonly List<bool> keptDice = new();
        private readonly List<int> diceValues = new();
        private readonly List<int> keptSlotIndices = new();

        private DiceVisualPool dicePool;
        private BakedDiceController bakedDiceController;
        private DicePresetCatalog presetCatalog;
        private int diceCount = 5;

        private Coroutine keepRoutine;
        private int rollIndex;

        /// <summary>킵 정렬 애니메이션이 시작·종료됐다. 흐름 쪽이 단계와 상태 문구를 갱신한다.</summary>
        public event Action ArrangeStarted;
        public event Action ArrangeCompleted;

        public IReadOnlyList<int> Values => diceValues;
        public int DiceCount => diceCount;
        public int KeptCount => keptDice.FindAll(kept => kept).Count;
        public bool AllKept => keptDice.Count > 0 && keptDice.TrueForAll(kept => kept);
        public int VisualCount => activeDice.Count;

        /// <summary>포인터가 올라간 주사위 번호. 없으면 -1이다.</summary>
        public int HoveredIndex { get; set; } = -1;

        public bool IsKept(int index) => index >= 0 && index < keptDice.Count && keptDice[index];

        public bool HasVisual(int index) => index >= 0 && index < activeDice.Count && activeDice[index] != null;

        public int GetValue(int index) => index >= 0 && index < diceValues.Count ? diceValues[index] : 0;

        public void Bind(DiceVisualPool pool, BakedDiceController baked, DicePresetCatalog catalog, int count)
        {
            dicePool = pool;
            bakedDiceController = baked;
            presetCatalog = catalog;
            diceCount = Mathf.Max(1, count);
        }

        /// <summary>주사위 개수만큼 비주얼을 만들어 둔다. 개수가 이미 맞으면 아무것도 하지 않는다.</summary>
        public void EnsureDiceState()
        {
            if (dicePool == null) return;
            if (activeDice.Count == diceCount && keptDice.Count == diceCount && diceValues.Count == diceCount) return;

            foreach (GameObject die in activeDice)
            {
                if (die != null) Destroy(die);
            }
            activeDice.Clear();
            keptDice.Clear();
            diceValues.Clear();
            keptSlotIndices.Clear();

            for (int index = 0; index < diceCount; index++)
            {
                activeDice.Add(dicePool.CreateVisualDie(index + 1));
                keptDice.Add(false);
                diceValues.Add(index + 1); // 기본 1~5 눈 설정
                keptSlotIndices.Add(-1);
            }

            dicePool.ArrangeInitialPositions(activeDice, diceValues);
        }

        /// <summary>새 턴을 위해 킵 슬롯을 비우고 주사위를 처음 자리로 되돌린다.</summary>
        public void ResetForTurn(YachtDieState[] authorityDice)
        {
            SyncFromAuthority(authorityDice);
            for (int i = 0; i < keptSlotIndices.Count; i++) keptSlotIndices[i] = -1;
            HoveredIndex = -1;
            dicePool?.ArrangeInitialPositions(activeDice, diceValues);
        }

        /// <summary>권위 계층이 확정한 눈과 킵 여부를 화면 사본에 복사한다.</summary>
        public void SyncFromAuthority(YachtDieState[] authorityDice)
        {
            if (authorityDice == null) return;
            int count = Mathf.Min(authorityDice.Length, Mathf.Min(keptDice.Count, diceValues.Count));
            for (int i = 0; i < count; i++)
            {
                keptDice[i] = authorityDice[i].IsKept;
                diceValues[i] = authorityDice[i].Value;
            }
        }

        /// <summary>화면 사본의 킵 표시만 지운다. 권위 해제는 부른 쪽이 이미 마쳤다고 본다.</summary>
        public void ClearKeepMarks()
        {
            for (int i = 0; i < keptDice.Count; i++) keptDice[i] = false;
            for (int i = 0; i < keptSlotIndices.Count; i++) keptSlotIndices[i] = -1;
        }

        public void SetDieType(DieType type)
        {
            dicePool?.SetDieType(type, activeDice);
        }

        /// <summary>
        /// 킵 상태를 반영하고 재정렬 애니메이션을 돌린다.
        /// 킵 슬롯은 왼쪽부터 비어 있는 가장 빠른 자리를 쓰며 기존 킵 주사위를 밀어내지 않는다.
        /// </summary>
        public void ApplyKeep(int index, bool kept)
        {
            if (index < 0 || index >= keptDice.Count) return;

            keptDice[index] = kept;
            if (kept)
            {
                bool[] occupied = new bool[diceCount];
                for (int i = 0; i < diceCount; i++)
                {
                    if (keptDice[i] && i != index && keptSlotIndices.Count > i && keptSlotIndices[i] >= 0 && keptSlotIndices[i] < diceCount)
                    {
                        occupied[keptSlotIndices[i]] = true;
                    }
                }
                int targetSlot = 0;
                for (int s = 0; s < diceCount; s++)
                {
                    if (!occupied[s])
                    {
                        targetSlot = s;
                        break;
                    }
                }
                while (keptSlotIndices.Count <= index) keptSlotIndices.Add(-1);
                keptSlotIndices[index] = targetSlot;
            }
            else
            {
                if (keptSlotIndices.Count > index) keptSlotIndices[index] = -1;
            }

            if (keepRoutine != null) StopCoroutine(keepRoutine);
            keepRoutine = StartCoroutine(AnimateKeepToggleRoutine());
        }

        /// <summary>진행 중인 굴림·킵 애니메이션을 끊는다. 새 게임 시작에서 쓴다.</summary>
        public void StopAnimations()
        {
            StopAllCoroutines();
            keepRoutine = null;
        }

        /// <summary>
        /// 굴림 궤적을 재생하고 결과를 보드 중앙에 정렬한다.
        /// 눈과 프리셋은 권위 명령 결과에서 이미 확정된 값이며 여기서 다시 뽑지 않는다.
        /// </summary>
        public IEnumerator PlayRoll(RollPresentation presentation)
        {
            HoveredIndex = -1;
            rollIndex++;

            int clipIndex = presentation.PresetIndex;
            presetCatalog.TryGetClip(presentation.PresetFile, clipIndex, out WebPresetClip clip);
            bool isMirrored = presentation.IsMirrored;

            List<int> rolledValues = new();
            List<int> keptValues = new();
            for (int i = 0; i < diceCount; i++)
            {
                if (keptDice[i]) keptValues.Add(diceValues[i]);
                else rolledValues.Add(diceValues[i]);
            }
            Debug.Log($"<color=#2EA3FF>[주사위 굴림 #{rollIndex}]</color> Preset #{clipIndex + 1} (미러링: {isMirrored}) | 굴린 눈: [{string.Join(", ", rolledValues)}], 킵된 눈: [{string.Join(", ", keptValues)}], 전체 결과: [{string.Join(", ", diceValues)}]");

            var diceTransforms = new Transform[activeDice.Count];
            for (int i = 0; i < activeDice.Count; i++)
            {
                if (activeDice[i] != null)
                {
                    activeDice[i].transform.localScale = Vector3.one * DiceBoardMetrics.DieSize;
                    diceTransforms[i] = activeDice[i].transform;
                }
            }

            yield return bakedDiceController.Play(
                diceTransforms,
                clipIndex,
                clip,
                keptDice,
                diceValues,
                isMirrored);

            // 굴림 완료 후 보드 중앙 정렬 (작은 눈 -> 큰 눈 오름차순)
            yield return dicePool.AnimateLayout(0.45f, activeDice, keptDice, keptSlotIndices, diceValues, bakedDiceController);
        }

        private IEnumerator AnimateKeepToggleRoutine()
        {
            ArrangeStarted?.Invoke();
            yield return dicePool.AnimateLayout(0.32f, activeDice, keptDice, keptSlotIndices, diceValues, bakedDiceController);
            keepRoutine = null;
            ArrangeCompleted?.Invoke();
        }
    }
}
