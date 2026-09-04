using System;
using Tessera.Games.Yacht;
using Tessera.Tabletop;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 증강 카드 UI를 소유하고 갱신한다(M10-T7).
    ///
    /// 선택 창·보유 카드 트레이·발동 버튼·안내 문구가 한 묶음으로 움직인다.
    /// 컨트롤러에 흔어져 있을 때는 위젯 12개와 슬롯 상태 3개가 본문에 섞여
    /// 어느 갱신이 어느 상태를 건드리는지 보이지 않았다.
    ///
    /// 이 클래스는 게임 상태를 갖지 않는다. <see cref="Refresh"/>가 받은 세션을 그자리에서
    /// 읽기만 하고, 버튼 입력은 사건으로 넘긴다.
    /// </summary>
    public sealed class AugmentTrayPresenter : MonoBehaviour
    {
        /// <summary>버튼으로 직접 발동하는 증강. 배열 순서가 화면 배치 순서다.</summary>
        private static readonly string[] ManualAugmentIds =
        {
            YachtAugmentRuntime.TableFlipId,
            YachtAugmentRuntime.EquivalentExchangeId,
            YachtAugmentRuntime.GambitId,
            YachtAugmentRuntime.DoubleDownId,
            YachtAugmentRuntime.DiceAlchemyId
        };

        private static readonly string[] ManualAugmentLabels =
        {
            "판 뒤집기", "등가교환", "갬빗", "더블 다운", "주사위 연금술"
        };

        /// <summary>직전 실행이 남긴 위젯. 다시 만들기 전에 치운다.</summary>
        private static readonly string[] StaleObjectNames =
        {
            "Yacht Augment Draft Overlay",
            "Yacht Augment Owned Text",
            "Yacht Augment Effect Text",
            "Yacht Augment Hover Detail Text",
            "Use Table Flip",
        };

        private GameObject draftOverlay;
        private Text draftTitle;
        private Text effectText;
        private Text hoverDetailText;
        private readonly Button[] draftButtons = new Button[YachtAugmentRuntime.DraftOptionCount];
        private readonly AugmentCardView[] draftCards = new AugmentCardView[YachtAugmentRuntime.DraftOptionCount];
        private readonly Button[] actionButtons = new Button[ManualAugmentIds.Length];
        private Button tableFlipButton;

        private readonly AugmentTrayCardView[] ownedCards = new AugmentTrayCardView[3];
        private AugmentCardTray cardTray;
        private Camera worldCamera;
        private int displayedPlayer = -1;
        private int hoveredSlot = -1;
        private int selectedSlot = -1;

        /// <summary>선택 창에서 몇 번째 후보를 골랐는지.</summary>
        public event Action<int> DraftOptionSelected;

        /// <summary>발동 버튼을 눌렀다. 인자는 증강 ID.</summary>
        public event Action<string> ActionRequested;

        public void Bind(AugmentCardTray tray, Camera camera)
        {
            cardTray = tray;
            worldCamera = camera;
        }

        /// <summary>오버레이와 버튼을 다시 만든다. 카드 본체 굽기는 호출 전에 픽셀 격자가 정해져 있어야 한다.</summary>
        public void BuildUi(Transform canvas)
        {
            if (canvas == null) return;
            for (int i = 0; i < StaleObjectNames.Length; i++)
            {
                Transform stale = canvas.Find(StaleObjectNames[i]);
                if (stale == null) continue;
                if (Application.isPlaying) Destroy(stale.gameObject);
                else DestroyImmediate(stale.gameObject);
            }

            draftOverlay = YachtHudFactory.CreateFullScreenOverlay(canvas, "Yacht Augment Draft Overlay");
            float draftCardWidth = 460f;
            float draftCardAspect = cardTray != null
                ? cardTray.CardSlotAspectRatio
                : AugmentCardView.TrayCardAspectRatio;
            float draftCardHeight = draftCardWidth / Mathf.Max(1f, draftCardAspect);
            float draftCardSpacing = draftCardWidth + 24f;
            draftTitle = YachtHudFactory.CreateText(draftOverlay.transform, "Draft Title", "증강 선택", new Vector2(0f, draftCardHeight * 0.5f + 72f),
                new Vector2(760f, 60f), new Vector2(0.5f, 0.5f), 34, TextAnchor.MiddleCenter);
            draftTitle.color = new Color32(255, 222, 151, 255);
            for (int i = 0; i < draftButtons.Length; i++)
            {
                int optionIndex = i;
                draftCards[i] = AugmentCardView.Create(
                    draftOverlay.transform,
                    $"Draft Option {i + 1}",
                    new Vector2((i - 1) * draftCardSpacing, -8f),
                    new Vector2(draftCardWidth, draftCardHeight),
                    new Vector2(0.5f, 0.5f),
                    () => DraftOptionSelected?.Invoke(optionIndex));
                draftCards[i].SetParchmentPreset((AugmentParchmentPreset)i);
                draftButtons[i] = draftCards[i].Button;
            }
            draftOverlay.SetActive(false);

            effectText = YachtHudFactory.CreateText(canvas, "Yacht Augment Effect Text", "", new Vector2(0f, 58f),
                new Vector2(760f, 44f), new Vector2(0.5f, 0f), 18, TextAnchor.MiddleCenter);
            effectText.color = new Color32(255, 205, 95, 255);
            hoverDetailText = YachtHudFactory.CreateText(canvas, "Yacht Augment Hover Detail Text", "", new Vector2(0f, 126f),
                new Vector2(820f, 64f), new Vector2(0.5f, 0f), 16, TextAnchor.MiddleCenter);
            hoverDetailText.color = new Color32(255, 226, 151, 255);
            hoverDetailText.gameObject.SetActive(false);
            for (int i = 0; i < actionButtons.Length; i++)
            {
                string augmentId = ManualAugmentIds[i];
                actionButtons[i] = YachtHudFactory.CreateButton(
                    canvas,
                    $"Use {augmentId}",
                    ManualAugmentLabels[i],
                    new Vector2((i - 2) * 152f, 18f),
                    new Vector2(142f, 48f),
                    new Vector2(0.5f, 0f),
                    () => ActionRequested?.Invoke(augmentId));
                actionButtons[i].gameObject.SetActive(false);
            }
            tableFlipButton = actionButtons[0];
        }

        public void Refresh(YachtGameSession session, bool interactive, string message)
        {
            bool augmented = session != null && session.Mode == YachtGameMode.Augmented;
            bool gameInProgress = augmented
                && session.Phase != YachtGamePhase.WaitingToStart
                && session.Phase != YachtGamePhase.GameOver;
            if (draftOverlay != null)
            {
                bool showDraft = gameInProgress && session.IsDrafting;
                draftOverlay.SetActive(showDraft);
                if (showDraft)
                    draftOverlay.transform.SetAsLastSibling();
            }
            if (effectText != null)
            {
                effectText.gameObject.SetActive(augmented && !string.IsNullOrEmpty(message));
                if (!string.IsNullOrEmpty(message)) effectText.text = message;
            }
            if (tableFlipButton != null)
            {
                tableFlipButton.interactable = gameInProgress && session.CanUseTableFlip && interactive;
            }
            for (int i = 0; i < actionButtons.Length; i++)
            {
                Button button = actionButtons[i];
                if (button == null) continue;
                bool owned = gameInProgress && !session.IsDrafting
                    && IsOwned(session, session.CurrentPlayerIndex, ManualAugmentIds[i]);
                button.gameObject.SetActive(owned);
                if (i > 0) button.interactable = owned && interactive;
            }
            RefreshOwnedCardTray(session, augmented, gameInProgress);
            if (!augmented) return;

            if (!session.IsDrafting) return;

            int playerIndex = session.State.Draft.PlayerIndex;
            if (draftTitle != null)
                draftTitle.text = $"P{playerIndex + 1} 증강 선택 · {session.CurrentRound}라운드";
            string[] options = session.State.Draft.Options;
            for (int i = 0; i < draftButtons.Length; i++)
            {
                Button button = draftButtons[i];
                if (button == null) continue;
                bool active = i < options.Length;
                button.gameObject.SetActive(active);
                if (!active) continue;
                YachtAugmentDefinition definition = YachtAugmentRuntime.Lookup(options[i]);
                int presetId = i < (session.State.Draft.OptionCardPresetIds?.Length ?? 0)
                    ? session.State.Draft.OptionCardPresetIds[i]
                    : 0;
                draftCards[i]?.SetParchmentPreset(AugmentParchmentVisuals.Normalize(presetId));
                draftCards[i]?.Bind(definition, AugmentCardDisplayState.Available);
            }
        }

        public void RefreshDraftCardParchment()
        {
            for (int i = 0; i < draftCards.Length; i++)
                draftCards[i]?.SetParchmentPreset(draftCards[i].ParchmentPreset);
        }

        public void EnsureOwnedCardViews()
        {
            if (cardTray == null || worldCamera == null) return;
            Vector2 slotSize = cardTray.CardSlotLocalSize;
            int count = Mathf.Min(ownedCards.Length, cardTray.SlotCount);
            for (int i = 0; i < count; i++)
            {
                if (ownedCards[i] != null) continue;
                Transform anchor = cardTray.GetSlotAnchor(i);
                if (anchor == null) continue;
                Transform existing = anchor.Find($"Owned Augment Card {i + 1}");
                ownedCards[i] = existing != null
                    ? existing.GetComponent<AugmentTrayCardView>()
                    : AugmentTrayCardView.Create(anchor, slotSize, i);
            }
        }

        private void RefreshOwnedCardTray(YachtGameSession session, bool augmented, bool gameInProgress)
        {
            if (!augmented || !gameInProgress)
            {
                for (int i = 0; i < ownedCards.Length; i++) ownedCards[i]?.SetVisible(false);
                return;
            }
            EnsureOwnedCardViews();
            int playerIndex = session != null && session.IsDrafting
                ? session.State.Draft.PlayerIndex
                : session?.CurrentPlayerIndex ?? -1;
            if (displayedPlayer != playerIndex)
            {
                displayedPlayer = playerIndex;
                selectedSlot = -1;
                SetHoveredSlot(-1);
            }

            string[] owned = augmented && gameInProgress && playerIndex >= 0
                ? session.State.AugmentPlayers[playerIndex].OwnedIds
                : Array.Empty<string>();
            int[] presets = playerIndex >= 0
                ? session.State.AugmentPlayers[playerIndex].OwnedCardPresetIds
                : Array.Empty<int>();
            if (selectedSlot >= owned.Length) selectedSlot = -1;

            for (int i = 0; i < ownedCards.Length; i++)
            {
                AugmentTrayCardView view = ownedCards[i];
                if (view == null) continue;
                bool visible = i < owned.Length;
                view.SetVisible(visible);
                if (!visible) continue;
                int presetId = i < (presets?.Length ?? 0) ? presets[i] : 0;
                view.Bind(YachtAugmentRuntime.Lookup(owned[i]), presetId);
                view.SetSelected(i == selectedSlot);
            }
        }

        private void SetHoveredSlot(int slotIndex)
        {
            if (hoveredSlot == slotIndex) return;
            if (hoveredSlot >= 0 && hoveredSlot < ownedCards.Length)
                ownedCards[hoveredSlot]?.SetHovered(false);

            hoveredSlot = slotIndex;
            YachtAugmentDefinition definition = null;
            if (slotIndex >= 0 && slotIndex < ownedCards.Length)
            {
                AugmentTrayCardView view = ownedCards[slotIndex];
                view?.SetHovered(true);
                definition = view?.Definition;
            }

            if (hoverDetailText == null) return;
            bool show = definition != null;
            hoverDetailText.gameObject.SetActive(show);
            if (show)
                hoverDetailText.text = $"{definition.DisplayName}\n{definition.Description}";
        }

        private static bool IsOwned(YachtGameSession session, int playerIndex, string augmentId)
        {
            if (session?.State?.AugmentPlayers == null
                || playerIndex < 0 || playerIndex >= session.State.AugmentPlayers.Length) return false;
            string[] owned = session.State.AugmentPlayers[playerIndex].OwnedIds;
            return Array.IndexOf(owned, augmentId) >= 0;
        }
        /// <summary>가리킨 카드를 바꾼다. null이면 안내를 숨긴다.</summary>
        public void SetHoveredCard(AugmentTrayCardView card)
        {
            SetHoveredSlot(card == null ? -1 : Array.IndexOf(ownedCards, card));
        }

        /// <summary>이미 고른 카드를 다시 누르면 선택이 풀린다.</summary>
        public void ToggleSelection(AugmentTrayCardView card)
        {
            int slot = Array.IndexOf(ownedCards, card);
            if (slot < 0) return;

            selectedSlot = selectedSlot == slot ? -1 : slot;
            for (int i = 0; i < ownedCards.Length; i++)
            {
                if (ownedCards[i] != null && ownedCards[i].gameObject.activeSelf)
                {
                    ownedCards[i].SetSelected(i == selectedSlot);
                }
            }
        }
    }
}
