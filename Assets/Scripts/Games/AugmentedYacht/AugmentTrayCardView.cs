using Tessera.Games.Yacht;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>3D 스톤 트레이 슬롯 위에 공통 증강 카드를 월드 공간으로 표시합니다.</summary>
    public sealed class AugmentTrayCardView : MonoBehaviour
    {
        private const int DecorationLayer = 11;
        private const float CardPixelWidth = 460f;
        private const float RestingHeight = 0.08f;
        private const float HoverHeight = 0.16f;

        private Canvas cardCanvas;
        private AugmentCardView card;
        private BoxCollider pointerCollider;
        private YachtAugmentDefinition definition;
        private Vector3 restingCanvasScale;
        private bool selected;

        public AugmentCardView Card => card;
        public BoxCollider PointerCollider => pointerCollider;
        public YachtAugmentDefinition Definition => definition;
        public string AugmentId => definition?.Id;
        public bool IsSelected => selected;
        public bool IsHovered { get; private set; }

        public static AugmentTrayCardView Create(
            Transform slotAnchor,
            Camera worldCamera,
            Vector2 slotLocalSize,
            int slotIndex)
        {
            GameObject root = new($"Owned Augment Card {slotIndex + 1}");
            root.layer = DecorationLayer;
            root.transform.SetParent(slotAnchor, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            AugmentTrayCardView view = root.AddComponent<AugmentTrayCardView>();
            view.pointerCollider = root.AddComponent<BoxCollider>();
            view.pointerCollider.center = new Vector3(0f, RestingHeight, 0f);
            view.pointerCollider.size = new Vector3(slotLocalSize.x * 0.94f, 0.12f, slotLocalSize.y * 0.90f);

            GameObject canvasObject = new("World Card Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.layer = DecorationLayer;
            canvasObject.transform.SetParent(root.transform, false);
            view.cardCanvas = canvasObject.GetComponent<Canvas>();
            view.cardCanvas.renderMode = RenderMode.WorldSpace;
            view.cardCanvas.worldCamera = worldCamera;
            view.cardCanvas.overrideSorting = true;
            view.cardCanvas.sortingOrder = 12 + slotIndex;

            float cardPixelHeight = CardPixelWidth / AugmentCardView.TrayCardAspectRatio;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(CardPixelWidth, cardPixelHeight);
            canvasRect.localPosition = new Vector3(0f, RestingHeight, 0f);
            canvasRect.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float worldScale = slotLocalSize.x * 0.94f / CardPixelWidth;
            view.restingCanvasScale = Vector3.one * worldScale;
            canvasRect.localScale = view.restingCanvasScale;

            view.card = AugmentCardView.Create(
                canvasObject.transform,
                "Card",
                Vector2.zero,
                new Vector2(CardPixelWidth, cardPixelHeight),
                new Vector2(0.5f, 0.5f),
                null);
            view.SetVisible(false);
            return view;
        }

        public void Bind(YachtAugmentDefinition value)
        {
            definition = value;
            selected = false;
            card.Bind(value, value == null ? AugmentCardDisplayState.Disabled : AugmentCardDisplayState.Owned);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (!visible)
            {
                IsHovered = false;
                selected = false;
            }
        }

        public void SetHovered(bool hovered)
        {
            if (IsHovered == hovered || cardCanvas == null) return;
            IsHovered = hovered;
            RectTransform rect = cardCanvas.transform as RectTransform;
            rect.localPosition = new Vector3(0f, hovered ? HoverHeight : RestingHeight, 0f);
            rect.localScale = restingCanvasScale * (hovered ? 1.035f : 1f);
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (definition != null)
                card.SetState(value ? AugmentCardDisplayState.Selected : AugmentCardDisplayState.Owned);
        }
    }
}
