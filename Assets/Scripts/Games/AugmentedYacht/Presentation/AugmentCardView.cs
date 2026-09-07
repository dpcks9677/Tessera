using System;
using System.Collections.Generic;
using Tessera.Games.Yacht;
using UnityEngine;
using UnityEngine.Events;
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

    /// <summary>증강 이름·효과·종류·대상·상태를 같은 정보 계층으로 표시하는 공통 카드 뷰입니다.</summary>
    public sealed class AugmentCardView : MonoBehaviour
    {
        public const float TrayCardAspectRatio = 1.774f;

        private static readonly Color Parchment = new(0.97f, 0.95f, 0.91f, 1f);
        private static readonly Color Ink = new(0.16f, 0.10f, 0.07f, 1f);
        private static readonly Color Crimson = new(0.53f, 0.18f, 0.13f, 1f);
        private static readonly Color AntiqueGold = new(0.90f, 0.66f, 0.24f, 1f);
        private static readonly Color Indigo = new(0.21f, 0.29f, 0.43f, 1f);

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
        private Text stateText;
        private Button button;
        private bool overlayContentOnly;

        public Button Button => button;
        public Text NameText => nameText;
        public Text DescriptionText => descriptionText;
        public Text KindText => kindText;
        public Text TargetText => targetText;
        public Text StateText => stateText;
        public Image Icon => icon;
        public Image Background => background;
        public Image StateAccent => stateAccent;
        public RectTransform ContentRoot => contentRoot;
        public Outline CardOutline => outline;
        public AugmentParchmentPreset ParchmentPreset { get; private set; }
        public AugmentCardDisplayState DisplayState { get; private set; }

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

        public void Bind(YachtAugmentDefinition definition, AugmentCardDisplayState state, Sprite overrideIcon = null)
        {
            if (definition == null)
            {
                nameText.text = "알 수 없는 증강";
                descriptionText.text = "표시 데이터를 찾을 수 없습니다.";
                kindText.text = "미확인";
                targetText.text = "대상 · 없음";
                icon.sprite = overrideIcon != null ? overrideIcon : AugmentPixelIconFactory.Get(YachtAugmentKind.Enhance);
                icon.color = overrideIcon != null ? Color.white : IconColor(YachtAugmentKind.Enhance);
                SetState(AugmentCardDisplayState.Disabled);
                return;
            }

            nameText.text = definition.DisplayName;
            descriptionText.text = Compact(definition.Description);
            kindText.text = KindLabel(definition.Kind);
            targetText.text = $"대상 · {TargetLabel(definition.Target)}";
            Sprite augmentIcon = overrideIcon ?? Resources.Load<Sprite>($"AugmentIcons/{definition.Id}");
            icon.sprite = augmentIcon != null ? augmentIcon : AugmentPixelIconFactory.Get(definition.Kind);
            // 증강 고유 아이콘은 앤틱 잉크색이 구워져 있으므로 틴트하지 않는다.
            icon.color = augmentIcon != null ? Color.white : IconColor(definition.Kind);
            SetState(state);
        }

        public void SetState(AugmentCardDisplayState state)
        {
            DisplayState = state;
            stateText.text = state switch
            {
                AugmentCardDisplayState.Available => "[선택 가능]",
                AugmentCardDisplayState.Selected => "[선택됨]",
                AugmentCardDisplayState.Owned => "[보유 중]",
                AugmentCardDisplayState.Conflict => "[충돌]",
                AugmentCardDisplayState.Used => "[사용 완료]",
                _ => "[비활성]"
            };

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

            stateText.color = accent;
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

            // 위에서 아래로 헤더 · 강조선 · 본문 · 푸터가 한 줄씩 쌓이는 배치다.
            // 각 행은 콘텐츠 사각형의 위아래 여백만으로 위치를 정하므로 카드 크기가 달라져도 비율이 유지된다.
            float headerHeight = 46f;
            float accentTop = headerHeight;
            float footerHeight = 22f;

            header = CreateImage(contentRoot, "Crimson Header", Vector2.zero, Vector2.zero, Crimson);
            SetStretch(header.rectTransform, 0f, 0f, 0f, contentHeight - headerHeight);
            header.raycastTarget = false;

            // 잉크 아이콘을 양피지 위에 직접 얹으므로 받침판은 배치 기준으로만 남기고 그리지 않는다.
            iconBacking = CreateImage(contentRoot, "Pixel Icon Backing", Vector2.zero, Vector2.zero, Color.clear);
            SetStretch(iconBacking.rectTransform, 4f, contentWidth - 38f, 6f, contentHeight - headerHeight + 6f);
            iconBacking.raycastTarget = false;
            icon = CreateImage(iconBacking.transform, "Pixel Icon", Vector2.zero, new Vector2(-6f, -6f), AntiqueGold, true);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // 이름은 크림슨 헤더 위에 얹히므로 잉크색이 아니라 양피지색으로 뽑는다.
            nameText = CreateText(contentRoot, "Name", "증강", Vector2.zero, Vector2.zero, 22, TextAnchor.MiddleLeft, Parchment);
            SetStretch(nameText.rectTransform, 44f, 100f, 0f, contentHeight - headerHeight);
            stateText = CreateText(contentRoot, "State Badge", "[선택 가능]", Vector2.zero, Vector2.zero, 12, TextAnchor.MiddleRight, AntiqueGold);
            SetStretch(stateText.rectTransform, contentWidth - 96f, 4f, 0f, contentHeight - headerHeight);

            // 상태 강조선이 헤더와 본문을 가르는 구분선을 겸한다.
            stateAccent = CreateImage(contentRoot, "State Accent", Vector2.zero, Vector2.zero, AntiqueGold);
            SetStretch(stateAccent.rectTransform, 2f, 2f, accentTop, contentHeight - accentTop - 2f);
            stateAccent.raycastTarget = false;

            descriptionText = CreateText(contentRoot, "Effect Body", "효과", Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft, Ink);
            SetStretch(descriptionText.rectTransform, 4f, 4f, accentTop + 8f, footerHeight + 6f);
            descriptionText.resizeTextForBestFit = true;
            descriptionText.resizeTextMinSize = 12;
            descriptionText.resizeTextMaxSize = 16;
            descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descriptionText.verticalOverflow = VerticalWrapMode.Truncate;

            kindText = CreateText(contentRoot, "Kind Badge", "종류", Vector2.zero, Vector2.zero, 12, TextAnchor.MiddleLeft, AntiqueGold);
            SetStretch(kindText.rectTransform, 4f, contentWidth * 0.6f, contentHeight - footerHeight, 0f);
            targetText = CreateText(contentRoot, "Target Badge", "대상 · 없음", Vector2.zero, Vector2.zero, 12, TextAnchor.MiddleRight, new Color(0.25f, 0.19f, 0.15f, 1f));
            SetStretch(targetText.rectTransform, contentWidth * 0.4f, 4f, contentHeight - footerHeight, 0f);
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
            font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/alagard.ttf")
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/m6x11.ttf");
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

        private static string TargetLabel(string target) => target switch
        {
            "Aces" => "에이스",
            "Deuces" => "듀스",
            "Threes" => "쓰리스",
            "Fours" => "포스",
            "Fives" => "파이브스",
            "Sixes" => "식스스",
            "Choice" => "초이스",
            "FourOfAKind" => "포카인드",
            "FullHouse" => "풀하우스",
            "SmallStraight" => "스몰 스트레이트",
            "LargeStraight" => "라지 스트레이트",
            "Yacht" => "요트",
            "Quest" => "퀘스트 진행",
            null or "" => "플레이 상태",
            _ => target
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

        private static void ClearRect(Color32[] pixels, int minX, int minY, int maxX, int maxY)
        {
            FillRect(pixels, minX, minY, maxX, maxY, default);
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
