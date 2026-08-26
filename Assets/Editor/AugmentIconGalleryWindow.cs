using System;
using System.Collections.Generic;
using Tessera.Games.AugmentedYacht;
using Tessera.Games.Yacht;
using UnityEditor;
using UnityEngine;

public sealed class AugmentIconGalleryWindow : EditorWindow
{
    private const string IconDirectory = "Assets/Resources/AugmentIcons";
    private const float CardWidth = 178f;
    private const float CardHeight = 202f;
    private const float CardGap = 8f;

    private static readonly string[] KindFilterLabels =
    {
        "전체",
        "점수 대체",
        "주사위",
        "강화",
        "퀘스트",
        "수동 행동",
        "무작위 교체"
    };

    private readonly List<YachtAugmentDefinition> definitions = new();
    private readonly Dictionary<string, Sprite> customIcons = new();

    private Vector2 scrollPosition;
    private string searchText = string.Empty;
    private int kindFilter;
    private bool customOnly;

    private GUIStyle cardStyle;
    private GUIStyle nameStyle;
    private GUIStyle detailStyle;
    private GUIStyle statusStyle;

    [MenuItem("Tessera/증강 아이콘 갤러리")]
    public static void Open()
    {
        AugmentIconGalleryWindow window = GetWindow<AugmentIconGalleryWindow>();
        window.titleContent = new GUIContent("증강 아이콘");
        window.minSize = new Vector2(420f, 360f);
        window.Show();
    }

    private void OnEnable()
    {
        ReloadDefinitions();
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawToolbar();

        List<YachtAugmentDefinition> visible = GetVisibleDefinitions();
        int customCount = CountCustomIcons();
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(
            $"표시 {visible.Count} / 전체 {definitions.Count}  ·  개별 에셋 {customCount}  ·  임시 공통 {definitions.Count - customCount}",
            EditorStyles.miniLabel);
        EditorGUILayout.Space(4f);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawGrid(visible);
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            string nextSearch = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f));
            if (!string.Equals(nextSearch, searchText, StringComparison.Ordinal))
            {
                searchText = nextSearch;
                scrollPosition = Vector2.zero;
            }

            int nextFilter = EditorGUILayout.Popup(kindFilter, KindFilterLabels, EditorStyles.toolbarPopup, GUILayout.Width(108f));
            if (nextFilter != kindFilter)
            {
                kindFilter = nextFilter;
                scrollPosition = Vector2.zero;
            }

            bool nextCustomOnly = GUILayout.Toggle(customOnly, "개별만", EditorStyles.toolbarButton, GUILayout.Width(52f));
            if (nextCustomOnly != customOnly)
            {
                customOnly = nextCustomOnly;
                scrollPosition = Vector2.zero;
            }

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(62f)))
                ReloadDefinitions();
        }
    }

    private void DrawGrid(IReadOnlyList<YachtAugmentDefinition> visible)
    {
        float availableWidth = Mathf.Max(CardWidth, position.width - 24f);
        int columnCount = Mathf.Max(1, Mathf.FloorToInt((availableWidth + CardGap) / (CardWidth + CardGap)));

        for (int index = 0; index < visible.Count; index += columnCount)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int column = 0; column < columnCount; column++)
                {
                    int definitionIndex = index + column;
                    if (definitionIndex < visible.Count)
                        DrawCard(visible[definitionIndex]);
                    else
                        GUILayout.Space(CardWidth);

                    if (column < columnCount - 1) GUILayout.Space(CardGap);
                }
            }

            GUILayout.Space(CardGap);
        }

        if (visible.Count == 0)
        {
            GUILayout.Space(24f);
            EditorGUILayout.LabelField("조건에 맞는 증강 아이콘이 없습니다.", EditorStyles.centeredGreyMiniLabel);
        }
    }

    private void DrawCard(YachtAugmentDefinition definition)
    {
        Rect cardRect = GUILayoutUtility.GetRect(CardWidth, CardHeight, GUILayout.Width(CardWidth), GUILayout.Height(CardHeight));
        GUI.Box(cardRect, GUIContent.none, cardStyle);

        bool hasCustomIcon = customIcons.TryGetValue(definition.Id, out Sprite sprite) && sprite != null;
        if (!hasCustomIcon) sprite = AugmentPixelIconFactory.Get(definition.Kind);

        Rect iconBacking = new(cardRect.x + 41f, cardRect.y + 10f, 96f, 96f);
        EditorGUI.DrawRect(iconBacking, new Color(0.12f, 0.08f, 0.07f, 0.95f));
        DrawSprite(new Rect(iconBacking.x + 12f, iconBacking.y + 12f, 72f, 72f), sprite, IconColor(definition.Kind));

        GUI.Label(new Rect(cardRect.x + 8f, cardRect.y + 112f, cardRect.width - 16f, 35f), definition.DisplayName, nameStyle);
        GUI.Label(new Rect(cardRect.x + 8f, cardRect.y + 150f, cardRect.width - 16f, 18f), KindLabel(definition.Kind), detailStyle);
        GUI.Label(new Rect(cardRect.x + 8f, cardRect.y + 169f, cardRect.width - 16f, 17f), definition.Id, detailStyle);

        Color previousColor = GUI.color;
        GUI.color = hasCustomIcon ? new Color(0.72f, 0.92f, 0.65f) : new Color(0.93f, 0.72f, 0.40f);
        GUI.Label(
            new Rect(cardRect.x + 8f, cardRect.y + 186f, cardRect.width - 16f, 14f),
            hasCustomIcon ? "개별 에셋" : "임시 공통",
            statusStyle);
        GUI.color = previousColor;

        EditorGUIUtility.AddCursorRect(cardRect, MouseCursor.Link);
        Event current = Event.current;
        if (current.type == EventType.MouseDown && current.button == 0 && cardRect.Contains(current.mousePosition))
        {
            if (hasCustomIcon)
            {
                Selection.activeObject = sprite;
                EditorGUIUtility.PingObject(sprite);
            }
            else
            {
                ShowNotification(new GUIContent($"{definition.DisplayName}: 아직 개별 아이콘 에셋이 없습니다."));
            }

            current.Use();
        }
    }

    private void ReloadDefinitions()
    {
        definitions.Clear();
        customIcons.Clear();

        IReadOnlyList<YachtAugmentDefinition> source = new YachtAugmentRuntime().GetDefinitions();
        for (int i = 0; i < source.Count; i++)
        {
            YachtAugmentDefinition definition = source[i];
            definitions.Add(definition);

            string iconPath = $"{IconDirectory}/{definition.Id}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite != null) customIcons[definition.Id] = sprite;
        }

        definitions.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.CurrentCulture));
        Repaint();
    }

    private List<YachtAugmentDefinition> GetVisibleDefinitions()
    {
        var result = new List<YachtAugmentDefinition>();
        for (int i = 0; i < definitions.Count; i++)
        {
            YachtAugmentDefinition definition = definitions[i];
            if (kindFilter > 0 && (int)definition.Kind != kindFilter - 1) continue;
            if (customOnly && !customIcons.ContainsKey(definition.Id)) continue;
            if (!MatchesSearch(definition)) continue;
            result.Add(definition);
        }

        return result;
    }

    private bool MatchesSearch(YachtAugmentDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return true;
        return definition.DisplayName.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0
            || definition.Id.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
            || KindLabel(definition.Kind).IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }

    private int CountCustomIcons()
    {
        int count = 0;
        for (int i = 0; i < definitions.Count; i++)
            if (customIcons.ContainsKey(definitions[i].Id)) count++;
        return count;
    }

    private static void DrawSprite(Rect rect, Sprite sprite, Color tint)
    {
        if (sprite == null || sprite.texture == null) return;

        Rect textureRect = sprite.textureRect;
        Texture2D texture = sprite.texture;
        Rect uv = new(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height);

        Color previousColor = GUI.color;
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
        GUI.color = previousColor;
    }

    private void EnsureStyles()
    {
        if (cardStyle != null) return;

        cardStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(0, 0, 0, 0)
        };
        nameStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            fontSize = 13
        };
        detailStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip
        };
        statusStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };
    }

    private static string KindLabel(YachtAugmentKind kind) => kind switch
    {
        YachtAugmentKind.ScoreReplacement => "점수 대체",
        YachtAugmentKind.Dice => "주사위",
        YachtAugmentKind.Enhancement => "강화",
        YachtAugmentKind.Quest => "퀘스트",
        YachtAugmentKind.ManualAction => "수동 행동",
        YachtAugmentKind.RandomReplacement => "무작위 교체",
        _ => kind.ToString()
    };

    private static Color IconColor(YachtAugmentKind kind) => kind switch
    {
        YachtAugmentKind.Quest => new Color(0.40f, 0.56f, 0.78f),
        YachtAugmentKind.Dice => new Color(0.95f, 0.70f, 0.27f),
        YachtAugmentKind.ManualAction => new Color(0.84f, 0.29f, 0.20f),
        YachtAugmentKind.RandomReplacement => new Color(0.66f, 0.42f, 0.83f),
        _ => new Color(0.90f, 0.66f, 0.24f)
    };
}
