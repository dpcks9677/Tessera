using System;
using System.Collections.Generic;
using Tessera.Core;
using Tessera.Games.Yacht;
using Tessera.Tabletop;
using UnityEngine;
using UnityEngine.Rendering;
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

        /// <summary>
        /// 선택 카드가 눕는 평면의 높이.
        ///
        /// 정렬 주사위는 중심이 약 2.81이고 반지름이 약 0.51이라 위쪽 면이 약 3.32까지 올라온다.
        /// 그 위로 올려야 주사위가 카드를 뚫고 나오지 않는다.
        /// </summary>
        private const float DraftPlaneY = 3.6f;

        /// <summary>선택 카드 사이 간격. 트레이 격벽(0.20)보다 조금 넓게 벌린다.</summary>
        private const float DraftCardGap = .3f;

        /// <summary>선택 중 판을 가리는 어두운 판의 한 변 길이와, 카드보다 얼마나 아래에 놓이는지.</summary>
        private const float DraftDimSize = 40f;
        private const float DraftDimDrop = .02f;

        private const string DraftRootName = "Yacht Augment Draft Cards";
        private const string DraftCardNamePrefix = "Draft Augment Card";

        private GameObject draftOverlay;
        private Text draftTitle;
        private Text effectText;
        private Text hoverDetailText;
        private readonly Button[] actionButtons = new Button[ManualAugmentIds.Length];
        private Button tableFlipButton;

        private Transform draftRoot;
        private Renderer draftDim;
        private Material draftDimMaterial;
        private readonly AugmentTrayCardView[] draftCards = new AugmentTrayCardView[YachtAugmentRuntime.DraftOptionCount];

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

            // 카드 본체는 트레이 카드와 같은 3D 두루마리라 월드에 있다. 화면 오버레이는 제목과 입력 차단만 맡으므로
            // 배경을 투명하게 비워 월드 카드가 그대로 보이게 한다. 판을 어둡게 덮는 일은 월드의 딤 판이 한다.
            draftOverlay = YachtHudFactory.CreateFullScreenOverlay(canvas, "Yacht Augment Draft Overlay");
            Image overlayBackground = draftOverlay.GetComponent<Image>();
            overlayBackground.color = Color.clear;
            draftTitle = YachtHudFactory.CreateText(draftOverlay.transform, "Draft Title", "증강 선택", new Vector2(0f, 250f),
                new Vector2(760f, 60f), new Vector2(0.5f, 0.5f), 34, TextAnchor.MiddleCenter);
            draftTitle.color = new Color32(255, 222, 151, 255);
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
            // 선택 중에는 딤이 판을 덮으므로 보유 카드를 감춘다. 보유 카드 글자는 Crisp UI로 합성돼
            // 딤 위에 그대로 떠 버리기 때문에 켜 두면 선택 화면이 지저분해진다.
            bool drafting = augmented && gameInProgress && session.IsDrafting;
            RefreshOwnedCardTray(session, augmented, gameInProgress && !drafting);
            RefreshDraftCards(session, drafting);
        }

        /// <summary>
        /// 선택 카드를 트레이 카드와 같은 3D 두루마리로 세운다.
        ///
        /// 트레이 슬롯과 같은 월드 크기로 만들고 트레이와 같은 부모 아래에 두므로,
        /// 메시·머티리얼·씬 조명·픽셀 필터가 모두 트레이 카드와 같은 경로를 탄다.
        /// </summary>
        private void RefreshDraftCards(YachtGameSession session, bool drafting)
        {
            if (!drafting)
            {
                for (int i = 0; i < draftCards.Length; i++) draftCards[i]?.SetVisible(false);
                if (draftDim != null) draftDim.gameObject.SetActive(false);
                return;
            }

            EnsureDraftCardViews();
            if (draftDim != null) draftDim.gameObject.SetActive(true);

            int playerIndex = session.State.Draft.PlayerIndex;
            if (draftTitle != null)
                draftTitle.text = $"P{playerIndex + 1} 증강 선택 · {session.CurrentRound}라운드";

            IReadOnlyList<string> options = session.State.Draft.Options;
            IReadOnlyList<int> presets = session.State.Draft.OptionCardPresetIds;
            for (int i = 0; i < draftCards.Length; i++)
            {
                AugmentTrayCardView view = draftCards[i];
                if (view == null) continue;
                bool active = i < options.Count;
                view.SetVisible(active);
                if (!active) continue;
                int presetId = i < (presets?.Count ?? 0) ? presets[i] : 0;
                view.Bind(YachtAugmentRuntime.Lookup(options[i]), presetId, AugmentCardDisplayState.Available);
            }
        }

        /// <summary>선택 카드 3장과 판을 덮는 딤을 트레이와 같은 좌표계에 세운다.</summary>
        private void EnsureDraftCardViews()
        {
            if (cardTray == null || worldCamera == null) return;
            if (draftRoot == null)
            {
                Transform existing = cardTray.transform.Find(DraftRootName);
                draftRoot = existing != null ? existing : new GameObject(DraftRootName).transform;
                draftRoot.SetParent(cardTray.transform, false);
                draftRoot.gameObject.layer = TesseraLayers.Decoration;
            }

            // 직교 카메라 시야 한가운데가 되도록 시선을 선택 평면까지 늘려 중심을 잡는다.
            Vector3 eye = worldCamera.transform.position;
            Vector3 forward = worldCamera.transform.forward;
            float distance = (eye.y - DraftPlaneY) / Mathf.Max(.001f, -forward.y);
            Vector3 center = eye + forward * distance;
            draftRoot.SetPositionAndRotation(
                new Vector3(center.x, DraftPlaneY, center.z), Quaternion.identity);

            if (draftDim == null) draftDim = CreateDraftDim(draftRoot);

            Vector2 slotSize = cardTray.CardSlotLocalSize;
            float step = slotSize.x + DraftCardGap;
            for (int i = 0; i < draftCards.Length; i++)
            {
                if (draftCards[i] == null)
                {
                    Transform existing = draftRoot.Find($"{DraftCardNamePrefix} {i + 1}");
                    draftCards[i] = existing != null
                        ? existing.GetComponent<AugmentTrayCardView>()
                        : AugmentTrayCardView.Create(draftRoot, slotSize, i, DraftCardNamePrefix);
                }
                if (draftCards[i] != null)
                    draftCards[i].transform.localPosition = new Vector3((i - 1) * step, 0f, 0f);
            }
        }

        /// <summary>
        /// 선택 중 테이블을 덮는 반투명 어두운 판.
        ///
        /// 월드(픽셀) 패스에 둔다. Crisp UI 레이어에 올리면 족보 표까지 함께 어두워지는데,
        /// 알파가 0.82라 족보가 18%만 남아 읽을 수 없게 된다. 족보는 선택 중에도 평소 밝기로
        /// 읽혀야 하므로, 이 판은 테이블·트레이·주사위만 어둡게 하고 족보는 건드리지 않는다.
        /// 카드와 족보가 겹치는 부분은 딤이 아니라 카드의 깊이 마스크가 잘라낸다.
        ///
        /// 불투명 카드가 먼저 깊이를 쓰고 이 판은 깊이 테스트만 하므로 카드 위를 덮지 않는다.
        /// </summary>
        private Renderer CreateDraftDim(Transform parent)
        {
            Transform existing = parent.Find("Draft Dim");
            if (existing != null) return existing.GetComponent<Renderer>();

            GameObject dimObject = new("Draft Dim", typeof(MeshFilter), typeof(MeshRenderer));
            dimObject.layer = TesseraLayers.Decoration;
            dimObject.transform.SetParent(parent, false);
            dimObject.transform.localPosition = new Vector3(0f, -DraftDimDrop, 0f);
            dimObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            dimObject.transform.localScale = new Vector3(DraftDimSize, DraftDimSize, 1f);
            dimObject.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            Color dim = new(.035f, .025f, .04f, .82f);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            draftDimMaterial = new Material(shader) { name = "Runtime Draft Dim", color = dim };
            if (draftDimMaterial.HasProperty("_BaseColor")) draftDimMaterial.SetColor("_BaseColor", dim);
            if (draftDimMaterial.HasProperty("_Surface")) draftDimMaterial.SetFloat("_Surface", 1f);
            if (draftDimMaterial.HasProperty("_Blend")) draftDimMaterial.SetFloat("_Blend", 0f);
            if (draftDimMaterial.HasProperty("_SrcBlend")) draftDimMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (draftDimMaterial.HasProperty("_DstBlend")) draftDimMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (draftDimMaterial.HasProperty("_ZWrite")) draftDimMaterial.SetFloat("_ZWrite", 0f);
            draftDimMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            draftDimMaterial.renderQueue = (int)RenderQueue.Transparent;

            MeshRenderer renderer = dimObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = draftDimMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
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

            IReadOnlyList<string> owned = augmented && gameInProgress && playerIndex >= 0
                ? session.State.AugmentPlayers[playerIndex].OwnedIds
                : Array.Empty<string>();
            IReadOnlyList<int> presets = playerIndex >= 0
                ? session.State.AugmentPlayers[playerIndex].OwnedCardPresetIds
                : Array.Empty<int>();
            if (selectedSlot >= owned.Count) selectedSlot = -1;

            for (int i = 0; i < ownedCards.Length; i++)
            {
                AugmentTrayCardView view = ownedCards[i];
                if (view == null) continue;
                bool visible = i < owned.Count;
                view.SetVisible(visible);
                if (!visible) continue;
                int presetId = i < (presets?.Count ?? 0) ? presets[i] : 0;
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
                || playerIndex < 0 || playerIndex >= session.State.AugmentPlayers.Count) return false;
            IReadOnlyList<string> owned = session.State.AugmentPlayers[playerIndex].OwnedIds;
            for (int i = 0; i < owned.Count; i++)
            {
                if (string.Equals(owned[i], augmentId, StringComparison.Ordinal)) return true;
            }
            return false;
        }
        /// <summary>가리킨 카드를 바꾼다. null이면 안내를 숨긴다.</summary>
        public void SetHoveredCard(AugmentTrayCardView card)
        {
            int draftIndex = card == null ? -1 : Array.IndexOf(draftCards, card);
            SetHoveredDraftIndex(draftIndex);
            if (draftIndex >= 0) return;
            SetHoveredSlot(card == null ? -1 : Array.IndexOf(ownedCards, card));
        }

        /// <summary>선택 카드는 하나만 떠오른다. 나머지는 내려 둔다.</summary>
        private void SetHoveredDraftIndex(int index)
        {
            for (int i = 0; i < draftCards.Length; i++) draftCards[i]?.SetHovered(i == index);
        }

        /// <summary>선택 카드를 누르면 그 후보를 고르고, 보유 카드는 선택을 켜고 끈다.</summary>
        public void ToggleSelection(AugmentTrayCardView card)
        {
            int draftIndex = Array.IndexOf(draftCards, card);
            if (draftIndex >= 0)
            {
                DraftOptionSelected?.Invoke(draftIndex);
                return;
            }

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

        private void OnDestroy()
        {
            if (draftDimMaterial == null) return;
            if (Application.isPlaying) Destroy(draftDimMaterial);
            else DestroyImmediate(draftDimMaterial);
        }
    }
}
