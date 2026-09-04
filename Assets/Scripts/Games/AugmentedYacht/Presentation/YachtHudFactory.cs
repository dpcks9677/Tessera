using UnityEngine;
using UnityEngine.UI;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 화면 HUD를 이루는 기본 위젯을 만든다(M10-T7a).
    ///
    /// 버튼·텍스트·전체 화면 오버레이는 생김새가 정해져 있고 게임 상태를 보지 않는다.
    /// 컨트롤러에 두면 여기를 쓰려는 쪽이 모두 컨트롤러를 거쳐야 하므로 따로 둔다.
    /// </summary>
    public static class YachtHudFactory
    {
        public static GameObject CreateFullScreenOverlay(Transform parent, string name)
        {
            GameObject overlay = new(name, typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(parent, false);
            RectTransform rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = overlay.GetComponent<Image>();
            image.color = new Color(0.035f, 0.025f, 0.04f, 0.82f);
            image.raycastTarget = true;
            return overlay;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Vector2 anchor, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.06f, 0.4f, 0.46f, 0.94f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.82f, 0.3f, 1f);
            colors.pressedColor = new Color(0.72f, 0.13f, 0.18f, 1f);
            button.colors = colors;
            button.onClick.AddListener(action);

            CreateText(buttonObject.transform, "Label", label, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), 15, TextAnchor.MiddleCenter, true);
            return button;
        }

        public static Text CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size, Vector2 anchor, int fontSize, TextAnchor alignment, bool stretch = false)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            if (stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = rect.anchorMax = anchor;
                rect.pivot = anchor;
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }

            Text text = textObject.GetComponent<Text>();
            Font font = null;
#if UNITY_EDITOR
            font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/alagard.ttf")
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/m6x11.ttf");
#endif
            text.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font != null && text.font.material != null && text.font.material.mainTexture != null)
            {
                text.font.material.mainTexture.filterMode = FilterMode.Point;
            }
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Normal;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            Shadow shadow = textObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }
    }
}
