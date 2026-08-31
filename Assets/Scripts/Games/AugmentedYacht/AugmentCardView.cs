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
        private Vector2 cardDisplaySize;

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
                icon.sprite = overrideIcon ?? AugmentPixelIconFactory.Get(YachtAugmentKind.Enhancement);
                SetState(AugmentCardDisplayState.Disabled);
                return;
            }

            nameText.text = definition.DisplayName;
            descriptionText.text = Compact(definition.Description);
            kindText.text = KindLabel(definition.Kind);
            targetText.text = $"대상 · {TargetLabel(definition.Target)}";
            icon.sprite = overrideIcon
                ?? Resources.Load<Sprite>($"AugmentIcons/{definition.Id}")
                ?? AugmentPixelIconFactory.Get(definition.Kind);
            icon.color = IconColor(definition.Kind);
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
            iconBacking.color = Color.Lerp(new Color(0.12f, 0.08f, 0.07f, 0.94f), accent, 0.12f);
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
            background.sprite = overlayContentOnly
                ? AugmentParchmentVisuals.GetSprite(ParchmentPreset, true)
                : AugmentParchmentVisuals.GetPixelFilteredSprite(ParchmentPreset, cardDisplaySize);
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
            cardDisplaySize = size;
            float width = Mathf.Max(240f, size.x);
            float height = Mathf.Max(135f, size.y);
            background = GetComponent<Image>();
            background.color = Parchment;
            SetParchmentPreset(AugmentParchmentPreset.GentleWave);

            outline = GetComponent<Outline>();
            outline.effectColor = new Color(.37f, .86f, 1f, .42f);
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
            float headerY = contentHeight * 0.40f;
            stateAccent = CreateImage(contentRoot, "State Accent", Vector2.zero, new Vector2(8f, -12f), AntiqueGold);
            RectTransform accentRect = stateAccent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = new Vector2(7f, 0f);
            accentRect.sizeDelta = new Vector2(8f, -16f);
            stateAccent.raycastTarget = false;

            header = CreateImage(contentRoot, "Crimson Header", new Vector2(0f, headerY), new Vector2(-18f, contentHeight * 0.18f), Crimson);
            header.raycastTarget = false;

            kindText = CreateText(contentRoot, "Kind Badge", "종류", new Vector2(-contentWidth * 0.29f, headerY), new Vector2(contentWidth * 0.36f, 28f), 14, TextAnchor.MiddleLeft, AntiqueGold);
            stateText = CreateText(contentRoot, "State Badge", "[선택 가능]", new Vector2(contentWidth * 0.29f, headerY), new Vector2(contentWidth * 0.36f, 28f), 13, TextAnchor.MiddleRight, AntiqueGold);

            iconBacking = CreateImage(contentRoot, "Pixel Icon Backing", new Vector2(0f, contentHeight * 0.19f), new Vector2(56f, 56f), new Color(0.12f, 0.08f, 0.07f, 0.94f));
            iconBacking.raycastTarget = false;
            icon = CreateImage(iconBacking.transform, "Pixel Icon", Vector2.zero, new Vector2(-10f, -10f), AntiqueGold, true);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            nameText = CreateText(contentRoot, "Name", "증강", new Vector2(0f, contentHeight * 0.015f), new Vector2(-30f, 34f), 21, TextAnchor.MiddleCenter, Ink);
            Image divider = CreateImage(contentRoot, "Description Divider", new Vector2(0f, -contentHeight * 0.085f), new Vector2(-28f, 2f), new Color(0.37f, 0.20f, 0.10f, 0.46f));
            divider.raycastTarget = false;

            descriptionText = CreateText(contentRoot, "One Line Effect", "효과", new Vector2(0f, -contentHeight * 0.19f), new Vector2(-34f, 30f), 15, TextAnchor.MiddleCenter, Ink);
            descriptionText.resizeTextForBestFit = true;
            descriptionText.resizeTextMinSize = 12;
            descriptionText.resizeTextMaxSize = 15;
            descriptionText.horizontalOverflow = HorizontalWrapMode.Overflow;
            descriptionText.verticalOverflow = VerticalWrapMode.Truncate;

            targetText = CreateText(contentRoot, "Target Badge", "대상 · 없음", new Vector2(0f, -contentHeight * 0.39f), new Vector2(-34f, 24f), 12, TextAnchor.MiddleCenter, new Color(0.25f, 0.19f, 0.15f, 1f));
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
            return compact.Length > 20 ? $"{compact[..19]}…" : compact;
        }

        private static string KindLabel(YachtAugmentKind kind) => kind switch
        {
            YachtAugmentKind.ScoreReplacement => "족보 교체",
            YachtAugmentKind.Dice => "특수 주사위",
            YachtAugmentKind.Quest => "퀘스트",
            YachtAugmentKind.ManualAction => "수동 행동",
            YachtAugmentKind.RandomReplacement => "무작위 교체",
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
            YachtAugmentKind.Quest => new Color(0.40f, 0.56f, 0.78f, 1f),
            YachtAugmentKind.Dice => new Color(0.95f, 0.70f, 0.27f, 1f),
            YachtAugmentKind.ManualAction => new Color(0.84f, 0.29f, 0.20f, 1f),
            YachtAugmentKind.RandomReplacement => new Color(0.66f, 0.42f, 0.83f, 1f),
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
                case YachtAugmentKind.ScoreReplacement:
                    FillDiamond(pixels, 32, 32, 24, white);
                    ClearDiamond(pixels, 32, 32, 12);
                    FillRect(pixels, 29, 18, 35, 46, white);
                    break;
                case YachtAugmentKind.Dice:
                    FillRect(pixels, 13, 13, 50, 50, white);
                    ClearRect(pixels, 17, 17, 46, 46);
                    FillRect(pixels, 19, 19, 25, 25, white);
                    FillRect(pixels, 38, 19, 44, 25, white);
                    FillRect(pixels, 29, 29, 35, 35, white);
                    FillRect(pixels, 19, 38, 25, 44, white);
                    FillRect(pixels, 38, 38, 44, 44, white);
                    break;
                case YachtAugmentKind.Quest:
                    FillDiamond(pixels, 32, 30, 25, white);
                    ClearDiamond(pixels, 32, 29, 15);
                    FillRect(pixels, 29, 26, 35, 43, white);
                    FillRect(pixels, 25, 37, 39, 43, white);
                    break;
                case YachtAugmentKind.ManualAction:
                    FillRect(pixels, 10, 27, 40, 37, white);
                    FillDiamond(pixels, 43, 32, 16, white);
                    break;
                case YachtAugmentKind.RandomReplacement:
                    FillRect(pixels, 12, 22, 51, 49, white);
                    ClearRect(pixels, 18, 28, 45, 43);
                    FillRect(pixels, 18, 15, 45, 22, white);
                    FillDiamond(pixels, 32, 14, 11, white);
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
