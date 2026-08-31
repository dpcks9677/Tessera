using Tessera.Games.Yacht;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>3D 양피지 본체와 픽셀 필터를 우회하는 화면 오버레이를 함께 관리합니다.</summary>
    public sealed class AugmentTrayCardView : MonoBehaviour
    {
        private const int DecorationLayer = 11;
        private const float CardPixelWidth = 460f;
        private const float RestingHeight = 0.08f;
        private const float HoverHeight = 0.16f;
        private const float HoverScale = 1.06f;
        private const float HoverSmoothTime = 0.12f;

        private Transform visualRoot;
        private AugmentScrollModel scrollModel;
        private RectTransform overlayRect;
        private AugmentCardView card;
        private BoxCollider pointerCollider;
        private Camera worldCamera;
        private YachtAugmentDefinition definition;
        private Vector2 cardWorldSize;
        private Vector3 positionVelocity;
        private Vector3 scaleVelocity;
        private float cardAspectRatio = AugmentCardView.TrayCardAspectRatio;
        private AugmentParchmentPreset currentPreset;
        private bool hasPreset;
        private bool selected;

        public AugmentCardView Card => card;
        public BoxCollider PointerCollider => pointerCollider;
        public YachtAugmentDefinition Definition => definition;
        public string AugmentId => definition?.Id;
        public bool IsSelected => selected;
        public bool IsHovered { get; private set; }
        public RectTransform OverlayRect => overlayRect;
        public Transform VisualRoot => visualRoot;
        public AugmentScrollModel ScrollModel => scrollModel;
        public float CardAspectRatio => Mathf.Max(1f, cardAspectRatio);

        public static AugmentTrayCardView Create(
            Transform slotAnchor,
            Camera worldCamera,
            Canvas overlayCanvas,
            Vector2 slotLocalSize,
            int slotIndex)
        {
            GameObject root = new($"Owned Augment Card {slotIndex + 1}");
            root.layer = DecorationLayer;
            root.transform.SetParent(slotAnchor, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            AugmentTrayCardView view = root.AddComponent<AugmentTrayCardView>();
            view.worldCamera = worldCamera;
            view.cardWorldSize = slotLocalSize;
            view.cardAspectRatio = slotLocalSize.y > 0f
                ? slotLocalSize.x / slotLocalSize.y
                : AugmentCardView.TrayCardAspectRatio;
            view.pointerCollider = root.AddComponent<BoxCollider>();
            view.pointerCollider.center = new Vector3(0f, RestingHeight + .04f, 0f);
            view.pointerCollider.size = new Vector3(view.cardWorldSize.x, .18f, view.cardWorldSize.y);

            GameObject visualObject = new("Parchment Visual Root");
            visualObject.layer = DecorationLayer;
            visualObject.transform.SetParent(root.transform, false);
            visualObject.transform.localPosition = new Vector3(0f, RestingHeight, 0f);
            view.visualRoot = visualObject.transform;

            overlayCanvas ??= GameObject.Find("Pixel Presentation")?.GetComponent<Canvas>()
                ?? Object.FindFirstObjectByType<Canvas>();
            if (overlayCanvas != null)
            {
                GameObject overlayObject = new($"HighRes Owned Augment Card {slotIndex + 1}", typeof(RectTransform));
                overlayObject.transform.SetParent(overlayCanvas.transform, false);
                view.overlayRect = overlayObject.GetComponent<RectTransform>();
                view.overlayRect.anchorMin = view.overlayRect.anchorMax = new Vector2(.5f, .5f);
                view.overlayRect.pivot = new Vector2(.5f, .5f);

                // 선택 창과 같은 기준 크기·비율로 만든 뒤 균일 스케일만 적용해 정보 배치를 일치시킨다.
                float cardPixelHeight = CardPixelWidth / view.CardAspectRatio;
                view.card = AugmentCardView.Create(
                    overlayObject.transform, "Card Content", Vector2.zero,
                    new Vector2(CardPixelWidth, cardPixelHeight), new Vector2(.5f, .5f), null);
                view.card.SetRaycastTargets(false);
                view.card.SetParchmentPreset(AugmentParchmentPreset.GentleWave, true);
            }

            view.ApplyPreset(AugmentParchmentPreset.GentleWave);
            view.SetVisible(false);
            return view;
        }

        public void Bind(YachtAugmentDefinition value, int presetId)
        {
            definition = value;
            selected = false;
            AugmentParchmentPreset preset = AugmentParchmentVisuals.Normalize(presetId);
            ApplyPreset(preset);
            if (card != null)
            {
                card.SetParchmentPreset(preset, true);
                card.Bind(value, value == null ? AugmentCardDisplayState.Disabled : AugmentCardDisplayState.Owned);
            }
            if (gameObject.activeInHierarchy) SyncOverlayTransform();
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                IsHovered = false;
                selected = false;
                ResetPose();
            }
            gameObject.SetActive(visible);
            if (overlayRect != null) overlayRect.gameObject.SetActive(visible && definition != null);
            if (visible) SyncOverlayTransform();
        }

        public void SetHovered(bool hovered)
        {
            IsHovered = hovered;
            if (hovered && overlayRect != null) overlayRect.SetAsLastSibling();
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (definition != null && card != null)
                card.SetState(value ? AugmentCardDisplayState.Selected : AugmentCardDisplayState.Owned);
            if (scrollModel != null)
                scrollModel.SetDisplayState(value ? AugmentCardDisplayState.Selected : AugmentCardDisplayState.Owned);
        }

        private void Update() => TickHover(Time.unscaledDeltaTime);
        private void LateUpdate() => SyncOverlayTransform();

        public void TickHover(float deltaTime)
        {
            if (visualRoot == null) return;
            Vector3 targetPosition = new(0f, IsHovered ? HoverHeight : RestingHeight, 0f);
            Vector3 targetScale = Vector3.one * (IsHovered ? HoverScale : 1f);
            visualRoot.localPosition = Vector3.SmoothDamp(
                visualRoot.localPosition, targetPosition, ref positionVelocity, HoverSmoothTime,
                Mathf.Infinity, Mathf.Max(0f, deltaTime));
            visualRoot.localScale = Vector3.SmoothDamp(
                visualRoot.localScale, targetScale, ref scaleVelocity, HoverSmoothTime,
                Mathf.Infinity, Mathf.Max(0f, deltaTime));
        }

        public void SyncOverlayTransform()
        {
            if (overlayRect == null || visualRoot == null || worldCamera == null || definition == null || !gameObject.activeInHierarchy)
            {
                if (overlayRect != null) overlayRect.gameObject.SetActive(false);
                return;
            }

            Vector3[] corners = new Vector3[4];
            if (scrollModel == null || !scrollModel.TryGetOverlayCorners(corners))
            {
                if (overlayRect != null) overlayRect.gameObject.SetActive(false);
                return;
            }
            Vector3 s0 = worldCamera.WorldToScreenPoint(corners[0]);
            Vector3 s1 = worldCamera.WorldToScreenPoint(corners[1]);
            Vector3 s2 = worldCamera.WorldToScreenPoint(corners[2]);
            Vector3 s3 = worldCamera.WorldToScreenPoint(corners[3]);
            if (s0.z <= 0f || s1.z <= 0f || s2.z <= 0f || s3.z <= 0f)
            {
                overlayRect.gameObject.SetActive(false);
                return;
            }

            overlayRect.gameObject.SetActive(true);
            float minX = Mathf.Min(s0.x, s1.x, s2.x, s3.x);
            float maxX = Mathf.Max(s0.x, s1.x, s2.x, s3.x);
            float minY = Mathf.Min(s0.y, s1.y, s2.y, s3.y);
            float maxY = Mathf.Max(s0.y, s1.y, s2.y, s3.y);
            // 3D 앵커는 안전 영역만 알려주므로 카드 전체 사각형을 역산해 선택 창과 같은 좌표계를 쓴다.
            Rect safe = AugmentParchmentVisuals.ContentSafeRect;
            float cardWidth = (maxX - minX) / Mathf.Max(.01f, safe.width);
            float cardHeight = (maxY - minY) / Mathf.Max(.01f, safe.height);
            overlayRect.position = new Vector3(
                (minX + maxX) * .5f - (safe.center.x - .5f) * cardWidth,
                (minY + maxY) * .5f - (safe.center.y - .5f) * cardHeight,
                0f);
            overlayRect.sizeDelta = new Vector2(cardWidth, cardHeight);
            if (card != null)
            {
                float scale = cardWidth / CardPixelWidth;
                card.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void ApplyPreset(AugmentParchmentPreset preset)
        {
            if (hasPreset && currentPreset == preset) return;
            if (scrollModel != null) DestroyRuntimeObject(scrollModel.gameObject);
            scrollModel = AugmentScrollModelFactory.Create(visualRoot, preset, cardWorldSize);
            currentPreset = preset;
            hasPreset = true;
            if (definition != null)
                scrollModel?.SetDisplayState(selected ? AugmentCardDisplayState.Selected : AugmentCardDisplayState.Owned);
        }

        private void ResetPose()
        {
            if (visualRoot == null) return;
            visualRoot.localPosition = new Vector3(0f, RestingHeight, 0f);
            visualRoot.localScale = Vector3.one;
            positionVelocity = scaleVelocity = Vector3.zero;
        }

        private void OnDisable()
        {
            if (overlayRect != null) overlayRect.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (overlayRect != null) DestroyRuntimeObject(overlayRect.gameObject);
        }

        private static void DestroyRuntimeObject(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
