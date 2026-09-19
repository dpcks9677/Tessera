using System;
using System.Collections.Generic;
using TMPro;
using Tessera.Games.Yacht;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore;
using UnityEngine.UI;

namespace Tessera.Games.AugmentedYacht
{
    public enum AugmentCardDisplayState
    {
        Available,
        Selected,
        Owned,
        Conflict,
        Used,
        Disabled
    }

    /// <summary>증강 이름·종류·효과를 같은 정보 계층으로 표시하는 공통 카드 뷰입니다.</summary>
    public sealed class AugmentCardView : MonoBehaviour
    {
        public const float TrayCardAspectRatio = 1.774f;

        // 퀘스트 진행 블록 레이아웃 상수. 웹 원본 getQuestProgressText 형식(라벨 + 점선 + 하위 목표 줄)을
        // 카드 하단에 그대로 옮긴다. 값 근거는 docs 계획서 "카드 레이아웃" 절 참고.
        // 헤더·본문·여백 배치는 Build()의 카드 공통 배치와 SetProgressBlock()의 본문 되돌림이
        // 같은 값을 알아야 하므로 여기 한 곳에 모은다.
        // 블록 높이는 고정이 아니라 행 수·줄바꿈에 따라 매 Bind마다 다시 잰다(SetProgressBlock 참고).
        private const float HeaderHeight = 46f;
        private const float FooterHeight = 22f;
        private const float DescriptionTopMargin = HeaderHeight + 16f;
        private const float DescriptionBottomDefault = FooterHeight + 6f;

        private const int RowSlotCount = 3;
        private const float BlockBottom = 0f;
        private const float RowGapAboveDash = 2f;
        private const float DashHeight = 3f;
        private const float GapAboveLabel = 2f;
        private const float LabelHeight = 15f;
        private const float DashSpanX = 4f;
        private const float DashGapX = 4f;

        // 콘텐츠 세이프 영역 아래에 남는 양피지 여백을 진행 블록이 잡아먹도록, 블록 아랫변을 그만큼 더
        // 내려 그린다(ContentSafeRect 자체는 트레이 오버레이 계산이 걸려 있어 건드리지 않는다).
        public const float FooterBleed = 10f;

        // 수동 발동 버튼 레이아웃 상수. 사용자가 실물을 보고 1.2배로 키워 달라고 요청했다(64→77).
        // 높이는 더 이상 FooterHeight에 묶이지 않는다 — BuildUseAction에서 아랫변을 푸터 아래
        // 양피지 여백(FooterBleed 안쪽)으로 4px 더 내려 22→26으로 키운다.
        private const float UseActionWidth = 77f;
        private const float UseActionMargin = 4f;
        private const float UseActionBorderThickness = 2f;
        private const float UseActionGlowSpread = 6f;

        // 웹 원본과 같은 상태 라벨 색이다. docs/reference/art_style_guide.md 톤과 별개로,
        // 이 세 색은 사용자가 원본 웹과 동일하게 지정했다.
        private static readonly Color StatusInProgress = new Color32(0x34, 0x98, 0xdb, 0xff);
        private static readonly Color StatusSucceeded = new Color32(0xD4, 0xAF, 0x37, 0xff);
        private static readonly Color StatusFailed = new Color32(0xe7, 0x4c, 0x3c, 0xff);

        private static readonly Color Parchment = new(0.97f, 0.95f, 0.91f, 1f);
        private static readonly Color Ink = new(0.16f, 0.10f, 0.07f, 1f);
        private static readonly Color Crimson = new(0.53f, 0.18f, 0.13f, 1f);
        private static readonly Color AntiqueGold = new(0.90f, 0.66f, 0.24f, 1f);
        private static readonly Color Indigo = new(0.21f, 0.29f, 0.43f, 1f);

        // RollCosmicCube의 nearHaloColor(0.02,1.80,3.60)·outerHaloColor(0.00,0.45,2.40)는 HDR이라
        // UGUI에 그대로 쓸 수 없다. 최대 성분으로 나눠 색조 방향만 남기고 양피지 위에서 죽지 않게 띄운 값이다.
        private static readonly Color NeonNear = new(0.10f, 0.72f, 1.00f, 1f);
        private static readonly Color NeonOuter = new(0.16f, 0.36f, 1.00f, 1f);

        /// <summary>
        /// 버튼 테두리다. 카드 아웃라인·구분선이 쓰는 앤틱 골드를 잉크 쪽으로 눌러, 양피지 위에
        /// 인쇄된 선처럼 보이게 한 값이다. 순수 잉크색은 카드의 따뜻한 톤에서 너무 튄다.
        /// </summary>
        private static readonly Color UseActionFrame = Color.Lerp(AntiqueGold, Ink, 0.30f);

        private static readonly Color UseActionFillTint = Color.Lerp(Parchment, AntiqueGold, 0.18f);

        /// <summary>버튼 안쪽 판이다. 양피지보다 살짝 따뜻하고 진해 "눌러 찍은 칸"으로 읽힌다.</summary>
        private static readonly Color UseActionFill = new(UseActionFillTint.r, UseActionFillTint.g, UseActionFillTint.b, .85f);
        private const float UseActionGlowMaxAlpha = .30f;
        private const float UseActionDisabledAlpha = .38f;

        /// <summary>진행 블록 행 하나. 취소선은 막대가 아니라 리치 텍스트 `&lt;s&gt;` 태그로 긋는다.</summary>
        public sealed class ProgressRow
        {
            public RectTransform Rect;
            public TextMeshProUGUI Text;
            public CanvasGroup Group;
        }

        private Image background;
        private Image header;
        private Image iconBacking;
        private Image stateAccent;
        private Outline outline;
        private Image icon;
        private RectTransform contentRoot;
        private Text nameText;
        private Text descriptionText;
        private Text kindText;
        private Text targetText;
        private Text statusLabel;
        private Image[] dashes = Array.Empty<Image>();
        private readonly ProgressRow[] progressRows = new ProgressRow[RowSlotCount];
        private GameObject progressBlockRoot;
        private RectTransform progressBlockRect;
        private Button button;
        private bool overlayContentOnly;
        private GameObject useActionButtonObject;
        private RectTransform useActionRect;
        private CanvasGroup useActionGroup;
        private Image useActionBorderImage;
        private Image useActionFillImage;
        private Image useActionGlowImage;
        private Text useActionLabelText;

        public Button Button => button;
        public Text NameText => nameText;
        public Text DescriptionText => descriptionText;
        public Text KindText => kindText;
        public Text TargetText => targetText;
        public Text StatusLabel => statusLabel;
        public GameObject ProgressBlockRoot => progressBlockRoot;
        public IReadOnlyList<ProgressRow> ProgressRows => progressRows;

        /// <summary>진행 블록의 실측 높이다. progress가 없으면 0이다.</summary>
        public float ProgressBlockHeight { get; private set; }

        /// <summary>테스트 지원용. 어떤 Text가 실제로 몇 개의 시각 줄로 렌더되는지 그대로 노출한다.</summary>
        public static int CountVisualLines(TMP_Text text) => text.GetTextInfo(text.text).lineCount;

        /// <summary>
        /// 테스트 지원용. row.Rect의 블록 위쪽 기준 (top, bottom) y를 되짚는다. 행 rect는 블록 위쪽에
        /// 고정한 anchor(위쪽 변)를 기준으로 놓이므로 offsetMax.y·offsetMin.y가 이미 그 값이다(둘 다 ≤0).
        /// </summary>
        public static (float Top, float Bottom) RowBlockLocalRange(RectTransform rowRect) =>
            (rowRect.offsetMax.y, rowRect.offsetMin.y);
        public Image Icon => icon;
        public Image Background => background;
        public Image StateAccent => stateAccent;
        public RectTransform ContentRoot => contentRoot;
        public Outline CardOutline => outline;
        public AugmentParchmentPreset ParchmentPreset { get; private set; }
        public AugmentCardDisplayState DisplayState { get; private set; }

        /// <summary>버튼 사각형을 카드 중심 기준 픽셀 좌표로 미리 계산해 둔 것. 콜라이더 동기화가
        /// 런타임 트랜스폼 조회 없이 결정적으로 도는 데 쓴다.</summary>
        public Rect UseActionCardRect { get; private set; }
        public bool UseActionVisible => useActionButtonObject != null && useActionButtonObject.activeSelf;
        public bool UseActionEnabled { get; private set; }
        public RectTransform UseActionRect => useActionRect;
        public Text UseActionLabel => useActionLabelText;
        public Color UseActionBorderColor => useActionBorderImage != null ? useActionBorderImage.color : default;
        public Color UseActionGlowColor => useActionGlowImage != null ? useActionGlowImage.color : default;

        public static AugmentCardView Create(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Vector2 anchor,
            UnityAction onClick)
        {
            GameObject cardObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline), typeof(RectMask2D), typeof(AugmentCardView));
            cardObject.transform.SetParent(parent, false);

            RectTransform rect = cardObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            AugmentCardView card = cardObject.GetComponent<AugmentCardView>();
            card.Build(onClick, size);
            return card;
        }

        public void Bind(YachtAugmentDefinition definition, AugmentCardDisplayState state, Sprite overrideIcon = null, AugmentProgress? progress = null)
        {
            if (definition == null)
            {
                nameText.text = "알 수 없는 증강";
                descriptionText.text = "표시 데이터를 찾을 수 없습니다.";
                kindText.text = "미확인";
                targetText.text = string.Empty;
                SetProgressBlock(null);
                icon.sprite = overrideIcon != null ? overrideIcon : AugmentPixelIconFactory.Get(YachtAugmentKind.Enhance);
                icon.color = overrideIcon != null ? Color.white : IconColor(YachtAugmentKind.Enhance);
                SetState(AugmentCardDisplayState.Disabled);
                return;
            }

            nameText.text = definition.DisplayName;
            descriptionText.text = Compact(definition.Description);
            kindText.text = KindLabel(definition.Kind);
            targetText.text = AugmentStickerCatalog.TryGetTargetCategory(definition, out _)
                ? AugmentStickerCatalog.MarkLabel(definition.Id, definition.DisplayName)
                : string.Empty;
            SetProgressBlock(progress);
            Sprite augmentIcon = overrideIcon ?? Resources.Load<Sprite>($"AugmentIcons/{definition.Id}");
            icon.sprite = augmentIcon != null ? augmentIcon : AugmentPixelIconFactory.Get(definition.Kind);
            // 증강 고유 아이콘은 앤틱 잉크색이 구워져 있으므로 틴트하지 않는다.
            icon.color = augmentIcon != null ? Color.white : IconColor(definition.Kind);
            SetState(state);
        }

        /// <summary>
        /// 퀘스트 진행 블록을 켜고 채우거나(progress 있음), 끄고 본문 여백을 되돌린다(progress 없음).
        /// 행은 재사용만 하고 파괴하지 않는다 — Build에서 미리 만들어 둔 3개 슬롯을 매번 다시 채운다.
        /// 블록 높이는 실제 줄 수만큼만 차지하도록 매번 다시 재고, 본문 아래 여백도 그 높이에 맞춘다.
        /// </summary>
        private void SetProgressBlock(AugmentProgress? progress)
        {
            bool hasProgress = progress.HasValue && progress.Value.Lines.Count > 0;
            progressBlockRoot.SetActive(hasProgress);

            if (!hasProgress)
            {
                SetStretch(descriptionText.rectTransform, 4f, 4f, DescriptionTopMargin, DescriptionBottomDefault);
                ProgressBlockHeight = 0f;
                for (int i = 0; i < progressRows.Length; i++) SetRowActive(progressRows[i], false);
                return;
            }

            AugmentProgress value = progress.Value;
            statusLabel.text = StatusLabelText(value.Outcome);
            statusLabel.color = StatusColor(value.Outcome);

            // 행은 실제 시각 줄 높이만큼만 차지하며 라벨·점선 아래에서 위→아래로 쌓인다.
            float rowsAreaTop = LabelHeight + GapAboveLabel + DashHeight + RowGapAboveDash;
            float rowWidth = progressBlockRect.rect.width - 8f; // 좌우 4px 여백
            float runningTop = rowsAreaTop;
            for (int i = 0; i < progressRows.Length; i++)
            {
                if (i >= value.Lines.Count)
                {
                    SetRowActive(progressRows[i], false);
                    continue;
                }
                runningTop = BindRow(progressRows[i], value.Lines[i], value.Outcome, runningTop, rowWidth);
            }

            float blockHeight = runningTop + BlockBottom;
            ProgressBlockHeight = blockHeight;

            // 블록 아랫변을 콘텐츠 세이프 영역 밖(FooterBleed)까지 내려 그린다. 본문 여백도 같은 값으로 맞춘다.
            float contentHeight = contentRoot.rect.height;
            SetStretch(progressBlockRect, 0f, 0f, contentHeight - (blockHeight - FooterBleed), -FooterBleed);
            SetStretch(descriptionText.rectTransform, 4f, 4f, DescriptionTopMargin, blockHeight - FooterBleed);
        }

        private static string StatusLabelText(AugmentProgressOutcome outcome) => outcome switch
        {
            AugmentProgressOutcome.Succeeded => "퀘스트 성공",
            AugmentProgressOutcome.Failed => "퀘스트 실패",
            _ => "퀘스트 진행 중"
        };

        private static Color StatusColor(AugmentProgressOutcome outcome) => outcome switch
        {
            AugmentProgressOutcome.Succeeded => StatusSucceeded,
            AugmentProgressOutcome.Failed => StatusFailed,
            _ => StatusInProgress
        };

        private void SetRowActive(ProgressRow row, bool active)
        {
            row.Rect.gameObject.SetActive(active);
        }

        /// <summary>
        /// 행을 채우고 실제 시각 줄 높이만큼 위치·크기를 잡는다. <paramref name="topOffset"/>는 블록
        /// 위쪽 변에서 이 행의 윗변까지 아래로 잰 거리(≥0)다. 반환값은 다음 행이 이어받을 topOffset이다.
        /// 취소선은 별도 막대가 아니라 리치 텍스트 `&lt;s&gt;` 태그로 긋는다.
        /// </summary>
        private float BindRow(ProgressRow row, AugmentProgressLine line, AugmentProgressOutcome outcome, float topOffset, float rowWidth)
        {
            row.Rect.gameObject.SetActive(true);
            bool strike = line.Done || outcome == AugmentProgressOutcome.Failed;
            string content = line.IsTargetNote
                ? $"└ 현재 타겟: <color=#D4AF37>{line.Text}</color>"
                : $"<b>퀘스트</b>: {line.Text}";
            row.Text.text = strike ? $"<s>{content}</s>" : content;
            row.Text.color = Ink;

            float rowHeight = Mathf.Max(row.Text.GetPreferredValues(row.Text.text, rowWidth, 0f).y, 1f);
            SetTopStretch(row.Rect, 4f, 4f, topOffset, rowHeight);

            row.Group.alpha = strike ? (line.Done ? 0.7f : 0.6f) : 1f;

            return topOffset + rowHeight;
        }

        public void SetState(AugmentCardDisplayState state)
        {
            DisplayState = state;
            Color accent = state switch
            {
                AugmentCardDisplayState.Available => AntiqueGold,
                AugmentCardDisplayState.Selected => new Color(1f, 0.62f, 0.23f, 1f),
                AugmentCardDisplayState.Owned => new Color(0.72f, 0.48f, 0.20f, 1f),
                AugmentCardDisplayState.Conflict => Crimson,
                AugmentCardDisplayState.Used => Indigo,
                _ => new Color(0.35f, 0.33f, 0.31f, 1f)
            };
            Color cardColor = state switch
            {
                AugmentCardDisplayState.Selected => new Color(1f, .97f, .88f, 1f),
                AugmentCardDisplayState.Owned => new Color(.98f, .96f, .92f, 1f),
                AugmentCardDisplayState.Conflict => new Color(.82f, .62f, .58f, 1f),
                AugmentCardDisplayState.Used => new Color(.72f, .75f, .78f, 1f),
                AugmentCardDisplayState.Disabled => new Color(.56f, .55f, .53f, 1f),
                _ => Color.white
            };

            if (overlayContentOnly) cardColor.a = 0f;

            stateAccent.color = accent;
            header.color = Color.Lerp(Crimson, accent, state == AugmentCardDisplayState.Available ? 0f : 0.28f);
            outline.effectColor = accent;
            outline.effectDistance = state is AugmentCardDisplayState.Selected or AugmentCardDisplayState.Conflict
                ? new Vector2(2f, -2f)
                : new Vector2(1f, -1f);
            button.interactable = state == AugmentCardDisplayState.Available;
            background.color = cardColor;

            // Button의 기본 비활성 회색 틴트가 상태별 색상을 덮지 않도록 카드 자체 색상을 유지한다.
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;
        }

        public void SetParchmentPreset(AugmentParchmentPreset preset, bool overlayContentOnly = false)
        {
            ParchmentPreset = AugmentParchmentVisuals.Normalize((int)preset);
            this.overlayContentOnly = overlayContentOnly;
            background.sprite = AugmentParchmentVisuals.GetSprite(ParchmentPreset, overlayContentOnly);
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
            if (outline != null) outline.enabled = !overlayContentOnly;
            if (overlayContentOnly)
            {
                Color transparent = background.color;
                transparent.a = 0f;
                background.color = transparent;
            }
        }

        public void SetRaycastTargets(bool enabled)
        {
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++) graphics[i].raycastTarget = enabled && graphics[i] == background;
        }

        /// <summary>발동 버튼을 켜고 끈다. 비활성이면 흐리게 표시하고 호버 연출도 되돌린다.</summary>
        public void SetUseAction(bool visible, bool enabled)
        {
            if (useActionButtonObject == null) return;
            useActionButtonObject.SetActive(visible);
            UseActionEnabled = enabled;
            useActionGroup.alpha = enabled ? 1f : UseActionDisabledAlpha;
            if (!enabled) SetUseActionHoverAmount(0f);
        }

        /// <summary>테두리·후광만 잉크 → 네온 파랑으로 보간한다. fill과 라벨 색은 그대로 둔다.</summary>
        public void SetUseActionHoverAmount(float amount)
        {
            if (useActionBorderImage == null) return;
            float t = Mathf.Clamp01(amount);
            useActionBorderImage.color = Color.Lerp(UseActionFrame, NeonNear, t);
            Color glow = NeonOuter;
            glow.a = Mathf.Lerp(0f, UseActionGlowMaxAlpha, t);
            useActionGlowImage.color = glow;
        }

        private void Build(UnityAction onClick, Vector2 size)
        {
            float width = Mathf.Max(240f, size.x);
            float height = Mathf.Max(135f, size.y);
            background = GetComponent<Image>();
            background.color = Parchment;
            SetParchmentPreset(AugmentParchmentPreset.GentleWave);

            outline = GetComponent<Outline>();
            outline.effectColor = AntiqueGold;
            outline.effectDistance = new Vector2(1f, -1f);

            button = GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.88f, 0.58f, 1f);
            colors.pressedColor = new Color(0.78f, 0.54f, 0.30f, 1f);
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);

            GameObject contentObject = new("Content Root", typeof(RectTransform));
            contentObject.transform.SetParent(transform, false);
            contentRoot = contentObject.GetComponent<RectTransform>();
            Rect safeRect = AugmentParchmentVisuals.ContentSafeRect;
            contentRoot.anchorMin = new Vector2(safeRect.xMin, safeRect.yMin);
            contentRoot.anchorMax = new Vector2(safeRect.xMax, safeRect.yMax);
            contentRoot.offsetMin = contentRoot.offsetMax = Vector2.zero;

            float contentWidth = width * safeRect.width;
            float contentHeight = height * safeRect.height;

            // 위에서 아래로 헤더 · 강조선 · 본문이 한 줄씩 쌓이는 배치다.
            // 각 행은 콘텐츠 사각형의 위아래 여백만으로 위치를 정하므로 카드 크기가 달라져도 비율이 유지된다.
            // footerHeight는 그리는 행이 아니라 본문 아래에 남기는 여백이다.
            header = CreateImage(contentRoot, "Crimson Header", Vector2.zero, Vector2.zero, Crimson);
            SetStretch(header.rectTransform, 0f, 0f, 0f, contentHeight - HeaderHeight);
            header.raycastTarget = false;

            // 잉크 아이콘을 양피지 위에 직접 얹으므로 받침판은 배치 기준으로만 남기고 그리지 않는다.
            iconBacking = CreateImage(contentRoot, "Pixel Icon Backing", Vector2.zero, Vector2.zero, Color.clear);
            SetStretch(iconBacking.rectTransform, 4f, contentWidth - 38f, 6f, contentHeight - HeaderHeight + 6f);
            iconBacking.raycastTarget = false;
            icon = CreateImage(iconBacking.transform, "Pixel Icon", Vector2.zero, new Vector2(-6f, -6f), AntiqueGold, true);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // 이름은 크림슨 헤더 위에 얹히므로 잉크색이 아니라 양피지색으로 뽑는다.
            nameText = CreateText(contentRoot, "Name", "증강", Vector2.zero, Vector2.zero, 22, TextAnchor.MiddleLeft, Parchment);
            SetStretch(nameText.rectTransform, 44f, 84f, 0f, contentHeight - HeaderHeight);
            kindText = CreateText(contentRoot, "Kind Badge", "종류", Vector2.zero, Vector2.zero, 17, TextAnchor.MiddleRight, AntiqueGold);
            SetStretch(kindText.rectTransform, contentWidth - 76f, 4f, 0f, contentHeight - HeaderHeight);

            // 상태 강조선이 헤더와 본문을 가르는 구분선을 겸한다.
            stateAccent = CreateImage(contentRoot, "State Accent", Vector2.zero, Vector2.zero, AntiqueGold);
            SetStretch(stateAccent.rectTransform, 2f, 2f, HeaderHeight, contentHeight - HeaderHeight - 2f);
            stateAccent.raycastTarget = false;

            descriptionText = CreateText(contentRoot, "Effect Body", "효과", Vector2.zero, Vector2.zero, 18, TextAnchor.UpperLeft, Ink);
            SetStretch(descriptionText.rectTransform, 4f, 4f, DescriptionTopMargin, DescriptionBottomDefault);
            descriptionText.resizeTextForBestFit = true;
            descriptionText.resizeTextMinSize = 14;
            descriptionText.resizeTextMaxSize = 19;
            descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descriptionText.verticalOverflow = VerticalWrapMode.Truncate;

            // footerHeight 여백 안에 대상 족보를 둔다. 본문 아래에 남겨 둔 자리라 본문 크기를 다시 계산할 필요가 없다.
            // 변형 카드에서만 채워지고, 퀘스트 카드는 진행 블록이 이 자리를 대신 쓴다(SetProgressBlock).
            targetText = CreateText(contentRoot, "Target Badge", "", Vector2.zero, Vector2.zero, 14, TextAnchor.LowerLeft, AntiqueGold);
            SetStretch(targetText.rectTransform, 4f, 4f, contentHeight - FooterHeight - 2f, 2f);

            BuildProgressBlock(contentWidth, contentHeight);
            BuildUseAction(contentWidth, contentHeight, width, height);
        }

        /// <summary>
        /// 퀘스트 진행 블록을 미리 만들어 둔다. Bind마다 켜고 끄고 다시 채우기만 하며, 자식을
        /// 파괴하거나 새로 만들지 않는다. 상태 라벨 → 점선 → 하위 목표 줄 최대 3개 순서다.
        /// 라벨·점선은 블록 위쪽에서부터의 거리가 고정이라 여기서 한 번만 배치한다. 블록 자체의
        /// 위치·크기와 행(row)들의 배치는 실제 줄 수에 따라 달라지므로 SetProgressBlock이 매번 다시 잡는다.
        /// </summary>
        private void BuildProgressBlock(float contentWidth, float contentHeight)
        {
            GameObject blockObject = new("Progress Block", typeof(RectTransform));
            blockObject.transform.SetParent(contentRoot, false);
            progressBlockRect = blockObject.GetComponent<RectTransform>();
            progressBlockRoot = blockObject;

            statusLabel = CreateText(progressBlockRect, "Status Label", "", Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleLeft, StatusInProgress);
            statusLabel.fontStyle = FontStyle.Bold;
            SetTopStretch(statusLabel.rectTransform, 4f, 4f, 0f, LabelHeight);

            float dashTop = LabelHeight + GapAboveLabel;
            int dashCount = Mathf.Max(1, Mathf.FloorToInt((contentWidth - DashSpanX) / (DashSpanX + DashGapX)));
            dashes = new Image[dashCount];
            for (int i = 0; i < dashCount; i++)
            {
                Image dash = CreateImage(progressBlockRect, $"Dash {i + 1}", Vector2.zero, Vector2.zero, AntiqueGold);
                float left = i * (DashSpanX + DashGapX);
                SetTopStretch(dash.rectTransform, left, contentWidth - (left + DashSpanX), dashTop, DashHeight);
                dash.raycastTarget = false;
                dashes[i] = dash;
            }

            for (int i = 0; i < RowSlotCount; i++)
            {
                progressRows[i] = CreateProgressRow(progressBlockRect, i);
            }
        }

        /// <summary>
        /// 버튼 테두리·fill·후광이 함께 쓰는 둥근 사각형 스프라이트. 카드 캔버스는 TesseraLayers.CrispUI라
        /// 픽셀 필터 격자를 타지 않으므로 모서리가 계단 없이 부드럽게 나온다. 빌트인 UI 스킨 스프라이트는
        /// AssetDatabase 전용 경로라 런타임에서 쓸 수 없어, AugmentStickerTexture와 같은 절차 생성
        /// 방식으로 대체했다. 3장이 공유하므로 RoundedRectSprite가 정적으로 한 번만 구워 캐시해 둔 것을
        /// 그대로 참조한다.
        /// </summary>
        private static Sprite UseActionRoundedSprite => RoundedRectSprite.Shared;

        /// <summary>
        /// 수동 발동 버튼을 푸터 오른쪽 끝에 미리 만들어 둔다. 윗변은 Target Badge(targetText, L387 근처)와
        /// 같은 값을 쓰지만, 아랫변은 사용자 요청으로 버튼을 키우면서 푸터 아래 양피지 여백(FooterBleed
        /// 안쪽)으로 4px 더 내렸다 — 두 rect가 더 이상 완전히 겹치지 않는다. 이 버튼을 쓰는 5종 수동
        /// 증강은 전부 Kind == Enhance라 AugmentStickerCatalog.HasSticker가 Modification만 통과시키는
        /// targetText는 항상 빈 문자열이므로, 자리가 겹쳐도 시각 충돌이 없다. 기본은 숨김 — 드래프트
        /// 카드에는 나타나지 않는다.
        /// </summary>
        private void BuildUseAction(float contentWidth, float contentHeight, float cardWidth, float cardHeight)
        {
            float left = contentWidth - (UseActionWidth + UseActionMargin);
            float right = UseActionMargin;
            float top = contentHeight - FooterHeight - 2f;
            float bottom = -2f;

            // contentRoot는 ContentSafeRect 비율 그대로라, 그 왼쪽/아래쪽 변의 카드 로컬 좌표를 구하면
            // 버튼의 contentRoot 기준 여백을 카드 중심 기준 좌표로 그대로 옮길 수 있다.
            Rect safeRect = AugmentParchmentVisuals.ContentSafeRect;
            float contentLeftEdge = (safeRect.xMin - 0.5f) * cardWidth;
            float contentBottomEdge = (safeRect.yMin - 0.5f) * cardHeight;
            UseActionCardRect = Rect.MinMaxRect(
                contentLeftEdge + left,
                contentBottomEdge + bottom,
                contentLeftEdge + (contentWidth - right),
                contentBottomEdge + (contentHeight - top));

            // 후광은 버튼 뒤에 6px 크게 깐 사각형 한 장뿐이다. 뒤에 있으므로 바깥 6px만 보인다.
            useActionGlowImage = CreateImage(contentRoot, "Use Action Glow", Vector2.zero, Vector2.zero, NeonOuter);
            SetStretch(useActionGlowImage.rectTransform,
                left - UseActionGlowSpread, right - UseActionGlowSpread,
                top - UseActionGlowSpread, bottom - UseActionGlowSpread);
            useActionGlowImage.raycastTarget = false;
            useActionGlowImage.sprite = UseActionRoundedSprite;
            useActionGlowImage.type = Image.Type.Sliced;
            Color glowColor = NeonOuter;
            glowColor.a = 0f;
            useActionGlowImage.color = glowColor;
            // 버튼이 커지면서 후광 윗변이 본문(Effect Body) 아랫변을 살짝 침범한다. 형제 순서를
            // contentRoot의 맨 앞(=렌더 순서상 가장 아래)으로 내려, 본문 텍스트가 후광 위에 그려지게 한다.
            useActionGlowImage.transform.SetAsFirstSibling();

            // 테두리는 스트립 4장이 아니라 바깥 사각형(이 Image) + 2px 인셋 fill 두 장으로 만든다.
            useActionButtonObject = new GameObject("Use Action Button", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            useActionButtonObject.transform.SetParent(contentRoot, false);
            useActionRect = useActionButtonObject.GetComponent<RectTransform>();
            SetStretch(useActionRect, left, right, top, bottom);
            useActionGroup = useActionButtonObject.GetComponent<CanvasGroup>();
            useActionBorderImage = useActionButtonObject.GetComponent<Image>();
            useActionBorderImage.color = UseActionFrame;
            useActionBorderImage.raycastTarget = false;
            useActionBorderImage.sprite = UseActionRoundedSprite;
            useActionBorderImage.type = Image.Type.Sliced;

            useActionFillImage = CreateImage(useActionRect, "Use Action Fill", Vector2.zero, Vector2.zero, UseActionFill);
            SetStretch(useActionFillImage.rectTransform,
                UseActionBorderThickness, UseActionBorderThickness, UseActionBorderThickness, UseActionBorderThickness);
            useActionFillImage.raycastTarget = false;
            useActionFillImage.sprite = UseActionRoundedSprite;
            useActionFillImage.type = Image.Type.Sliced;

            useActionLabelText = CreateText(useActionRect, "Use Action Label", "사용", Vector2.zero, Vector2.zero, 17, TextAnchor.MiddleCenter, Ink);
            SetStretch(useActionLabelText.rectTransform, 0f, 0f, 0f, 0f);
            useActionLabelText.raycastTarget = false;

            useActionButtonObject.SetActive(false);
        }

        /// <summary>
        /// 행 하나를 만든다. 위아래 위치는 실제 내용에 따라 <see cref="BindRow"/>가 매번 다시 잡으므로,
        /// 여기서는 자리표시자 위치로만 둔다(항상 Bind 전에 쓰이지 않는다).
        /// </summary>
        private ProgressRow CreateProgressRow(Transform parent, int index)
        {
            GameObject rowObject = new($"Progress Row {index + 1}", typeof(RectTransform), typeof(CanvasGroup));
            rowObject.transform.SetParent(parent, false);
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            SetTopStretch(rowRect, 4f, 4f, 0f, 0f);

            TextMeshProUGUI rowText = CreateProgressText(rowRect);

            return new ProgressRow
            {
                Rect = rowRect,
                Text = rowText,
                Group = rowObject.GetComponent<CanvasGroup>()
            };
        }

        /// <summary>진행 행 전용 TMP 텍스트를 만든다. 취소선을 리치 텍스트 태그로 긋기 위해 richText를 켠다.</summary>
        private static TextMeshProUGUI CreateProgressText(Transform parent)
        {
            GameObject textObject = new("Row Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetStretch(rect, 0f, 0f, 0f, 0f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset font = LoadProgressFont();
            if (font != null) text.font = font;
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = Ink;
            text.richText = true;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }

        private static TMP_FontAsset progressFont;

        private static TMP_FontAsset LoadProgressFont()
        {
            if (progressFont != null) return progressFont;
            Font source = LoadFont();
            // 별도 폰트 에셋 파일을 두지 않고 레거시 Font로부터 런타임 동적 아틀라스를 생성한다.
            progressFont = TMP_FontAsset.CreateFontAsset(source);
            if (progressFont != null)
            {
                progressFont.hideFlags = HideFlags.DontSave;

                // 픽셀 폰트에는 OS/2 취소선 메트릭이 없어 strikethroughOffset이 0으로 잡혀 기준선(밑줄처럼)에
                // 그려진다. 한글 글리프 하나("가")를 기준으로 세로 중앙에 오도록 직접 계산해 둔다.
                progressFont.TryAddCharacters("가");
                if (progressFont.characterLookupTable.TryGetValue('가', out TMP_Character ch) && ch.glyph != null)
                {
                    GlyphMetrics metrics = ch.glyph.metrics;
                    FaceInfo face = progressFont.faceInfo;
                    face.strikethroughOffset = metrics.horizontalBearingY - metrics.height / 2f;
                    progressFont.faceInfo = face;
                }
            }
            return progressFont;
        }

        /// <summary>부모 사각형에 네 변 여백만으로 붙인다. 행 단위 배치를 좌표 계산 없이 표현하기 위한 것이다.</summary>
        private static void SetStretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>
        /// 부모의 위쪽 변에 고정하고 좌우로만 늘린다. 높이가 매번 달라지는 진행 블록·행처럼
        /// "위에서 topOffset만큼 내려가 height만큼 차지"를 표현할 때 쓴다.
        /// </summary>
        private static void SetTopStretch(RectTransform rect, float left, float right, float topOffset, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2((left - right) / 2f, -topOffset);
            rect.sizeDelta = new Vector2(-(left + right), height);
        }

        private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color, bool stretch = false)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            if (stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(-size.x * 0.5f, -size.y * 0.5f);
                rect.offsetMax = new Vector2(size.x * 0.5f, size.y * 0.5f);
            }
            else
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = size.x < 0f || size.y < 0f
                    ? new Vector2(Mathf.Max(0f, -size.x), Mathf.Max(0f, -size.y))
                    : size;
                if (size.x < 0f)
                {
                    rect.anchorMin = new Vector2(0f, 0.5f);
                    rect.anchorMax = new Vector2(1f, 0.5f);
                    rect.offsetMin = new Vector2(-size.x * 0.5f, position.y - Mathf.Abs(size.y) * 0.5f);
                    rect.offsetMax = new Vector2(size.x * 0.5f, position.y + Mathf.Abs(size.y) * 0.5f);
                }
            }
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            if (size.x < 0f)
            {
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.offsetMin = new Vector2(-size.x * 0.5f, position.y - size.y * 0.5f);
                rect.offsetMax = new Vector2(size.x * 0.5f, position.y + size.y * 0.5f);
            }
            else
            {
                rect.sizeDelta = size;
            }

            Text text = textObject.GetComponent<Text>();
            text.font = LoadFont();
            if (text.font != null && text.font.material != null && text.font.material.mainTexture != null)
            {
                text.font.material.mainTexture.filterMode = FilterMode.Point;
            }
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Font LoadFont()
        {
            Font font = null;
#if UNITY_EDITOR
            font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/ThirdParty/Fonts/Mulmaru.ttf")
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/ThirdParty/Fonts/alagard.ttf")
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/ThirdParty/Fonts/m6x11.ttf");
#endif
            return font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static string Compact(string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return "효과 설명 없음";
            string compact = description.Replace('\n', ' ').Replace('\r', ' ').Trim();
            while (compact.Contains("  ", StringComparison.Ordinal)) compact = compact.Replace("  ", " ");
            return compact;
        }

        private static string KindLabel(YachtAugmentKind kind) => kind switch
        {
            YachtAugmentKind.Modification => "변형",
            YachtAugmentKind.Quest => "퀘스트",
            _ => "강화"
        };

        private static Color IconColor(YachtAugmentKind kind) => kind switch
        {
            YachtAugmentKind.Modification => new Color(0.66f, 0.42f, 0.83f, 1f),
            YachtAugmentKind.Quest => new Color(0.40f, 0.56f, 0.78f, 1f),
            _ => AntiqueGold
        };
    }

    public static class AugmentPixelIconFactory
    {
        private const int Size = 64;
        private static readonly Dictionary<YachtAugmentKind, Sprite> Cache = new();

        public static Sprite Get(YachtAugmentKind kind)
        {
            if (Cache.TryGetValue(kind, out Sprite cached) && cached != null) return cached;

            Color32[] pixels = new Color32[Size * Size];
            DrawGlyph(pixels, kind);
            Texture2D texture = new(Size, Size, TextureFormat.RGBA32, false)
            {
                name = $"Augment_Pixel_{kind}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
            sprite.name = texture.name;
            Cache[kind] = sprite;
            return sprite;
        }

        private static void DrawGlyph(Color32[] pixels, YachtAugmentKind kind)
        {
            Color32 white = new(255, 255, 255, 255);
            switch (kind)
            {
                case YachtAugmentKind.Modification:
                    FillDiamond(pixels, 32, 32, 24, white);
                    ClearDiamond(pixels, 32, 32, 12);
                    FillRect(pixels, 29, 18, 35, 46, white);
                    break;
                case YachtAugmentKind.Quest:
                    FillDiamond(pixels, 32, 30, 25, white);
                    ClearDiamond(pixels, 32, 29, 15);
                    FillRect(pixels, 29, 26, 35, 43, white);
                    FillRect(pixels, 25, 37, 39, 43, white);
                    break;
                default:
                    FillDiamond(pixels, 32, 32, 25, white);
                    ClearDiamond(pixels, 32, 32, 14);
                    FillRect(pixels, 28, 12, 35, 51, white);
                    FillRect(pixels, 12, 28, 51, 35, white);
                    break;
            }
        }

        private static void FillRect(Color32[] pixels, int minX, int minY, int maxX, int maxY, Color32 color)
        {
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++) Set(pixels, x, y, color);
        }

        private static void FillDiamond(Color32[] pixels, int centerX, int centerY, int radius, Color32 color)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                int span = radius - Mathf.Abs(y - centerY);
                for (int x = centerX - span; x <= centerX + span; x++) Set(pixels, x, y, color);
            }
        }

        private static void ClearDiamond(Color32[] pixels, int centerX, int centerY, int radius)
        {
            FillDiamond(pixels, centerX, centerY, radius, default);
        }

        private static void Set(Color32[] pixels, int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= Size || y >= Size) return;
            pixels[y * Size + x] = color;
        }
    }
}
