using Tessera.Core;
using Tessera.Games.Yacht;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 3D 양피지 본체와, 그 위에 눕힌 월드 스페이스 카드 UI를 함께 관리한다.
    ///
    /// 카드 UI는 양피지의 자식이므로 트레이를 옮기거나 카드가 호버로 떠오를 때 자동으로 따라온다.
    /// 픽셀 필터는 CrispUI 레이어를 월드 카메라에서 제외하는 방식으로 우회한다(M9.5).
    /// </summary>
    public sealed class AugmentTrayCardView : MonoBehaviour
    {
        public const string OwnedCardNamePrefix = "Owned Augment Card";

        private const int DecorationLayer = 11;
        private const float CardPixelWidth = 460f;
        private const float RestingHeight = 0.08f;
        private const float HoverHeight = 0.16f;
        private const float HoverScale = 1.06f;
        private const float HoverSmoothTime = 0.12f;

        /// <summary>월드 1단위당 캔버스 좌표 단위. 폰트 크기를 픽셀 감각 그대로 쓰기 위한 배율이다.</summary>
        private const float CanvasUnitsPerWorldUnit = 100f;

        /// <summary>양피지 표면과 캔버스 사이 z-fighting을 피하기 위한 최소 간격.</summary>
        private const float OverlayLift = 0.004f;

        /// <summary>
        /// 깊이 마스크가 놓이는 높이. 카드 글자 캔버스보다 낮아야 자기 글자를 잘라내지 않는다.
        /// </summary>
        private const float DepthMaskLift = 0.002f;

        private const string DepthMaskShaderName = "DicePoC/CrispUiDepthMask";

        /// <summary>모든 카드가 함께 쓰는 깊이 전용 재질. 색을 쓰지 않으므로 카드마다 나눌 이유가 없다.</summary>
        private static Material sharedDepthMaskMaterial;

        private Transform visualRoot;
        private AugmentScrollModel scrollModel;
        private RectTransform overlayRect;
        private Transform depthMask;
        private Canvas overlayCanvas;
        private AugmentCardView card;
        private BoxCollider pointerCollider;
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
        public Transform DepthMask => depthMask;
        public Transform VisualRoot => visualRoot;
        public AugmentScrollModel ScrollModel => scrollModel;
        public float CardAspectRatio => Mathf.Max(1f, cardAspectRatio);

        public static AugmentTrayCardView Create(
            Transform slotAnchor,
            Vector2 slotLocalSize,
            int slotIndex,
            string namePrefix = OwnedCardNamePrefix)
        {
            GameObject root = new($"{namePrefix} {slotIndex + 1}");
            root.layer = DecorationLayer;
            root.transform.SetParent(slotAnchor, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            AugmentTrayCardView view = root.AddComponent<AugmentTrayCardView>();
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

            // 카드 UI는 양피지 자식으로 눕힌 월드 스페이스 캔버스에 그린다.
            GameObject overlayObject = new(
                $"HighRes {namePrefix} {slotIndex + 1}",
                typeof(RectTransform), typeof(Canvas));
            overlayObject.transform.SetParent(visualObject.transform, false);
            view.overlayRect = overlayObject.GetComponent<RectTransform>();
            view.overlayRect.anchorMin = view.overlayRect.anchorMax = new Vector2(.5f, .5f);
            view.overlayRect.pivot = new Vector2(.5f, .5f);
            view.overlayRect.localRotation = Quaternion.Euler(90f, 0f, 0f);
            view.overlayRect.localScale = Vector3.one / CanvasUnitsPerWorldUnit;

            view.overlayCanvas = overlayObject.GetComponent<Canvas>();
            view.overlayCanvas.renderMode = RenderMode.WorldSpace;
            view.overlayCanvas.overrideSorting = true;

            // 선택 창과 같은 기준 크기·비율로 만든 뒤 균일 스케일만 적용해 정보 배치를 일치시킨다.
            float cardPixelHeight = CardPixelWidth / view.CardAspectRatio;
            view.card = AugmentCardView.Create(
                overlayObject.transform, "Card Content", Vector2.zero,
                new Vector2(CardPixelWidth, cardPixelHeight), new Vector2(.5f, .5f), null);
            view.card.SetRaycastTargets(false);
            view.card.SetParchmentPreset(AugmentParchmentPreset.GentleWave, true);

            SetLayerRecursively(overlayObject, TesseraLayers.CrispUI);
            MarkDontSaveRecursively(overlayObject);

            view.depthMask = CreateDepthMask(visualObject.transform);

            view.ApplyPreset(AugmentParchmentPreset.GentleWave);
            view.SetVisible(false);
            return view;
        }

        public void Bind(
            YachtAugmentDefinition value,
            int presetId,
            AugmentCardDisplayState state = AugmentCardDisplayState.Owned)
        {
            definition = value;
            selected = false;
            AugmentParchmentPreset preset = AugmentParchmentVisuals.Normalize(presetId);
            ApplyPreset(preset);
            if (card != null)
            {
                card.SetParchmentPreset(preset, true);
                card.Bind(value, value == null ? AugmentCardDisplayState.Disabled : state);
            }
            if (scrollModel != null && value != null) scrollModel.SetDisplayState(state);
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
            // 월드 스페이스에서는 형제 순서가 아니라 정렬 순서가 앞뒤를 정한다.
            if (overlayCanvas != null) overlayCanvas.sortingOrder = hovered ? 1 : 0;
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

        /// <summary>
        /// 카드 UI 사각형을 양피지 로컬 좌표에서 계산한다.
        ///
        /// 스크롤 모델의 앵커 4개는 글이 안전하게 들어갈 영역만 알려주므로,
        /// <see cref="AugmentParchmentVisuals.ContentSafeRect"/>로 카드 전체 사각형을 역산해
        /// 선택 창과 같은 좌표계를 쓴다. 화면 투영을 쓰지 않으므로 매 프레임 돌 필요가 없다.
        /// </summary>
        public void SyncOverlayTransform()
        {
            if (overlayRect == null || visualRoot == null || definition == null || !gameObject.activeInHierarchy)
            {
                if (overlayRect != null) overlayRect.gameObject.SetActive(false);
                if (depthMask != null) depthMask.gameObject.SetActive(false);
                return;
            }

            Vector3[] corners = new Vector3[4];
            if (scrollModel == null || !scrollModel.TryGetOverlayCorners(corners))
            {
                overlayRect.gameObject.SetActive(false);
                if (depthMask != null) depthMask.gameObject.SetActive(false);
                return;
            }

            // 앵커의 월드 좌표를 양피지 로컬 좌표로 되돌린다.
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = visualRoot.InverseTransformPoint(corners[i]);
                minX = Mathf.Min(minX, local.x);
                maxX = Mathf.Max(maxX, local.x);
                minZ = Mathf.Min(minZ, local.z);
                maxZ = Mathf.Max(maxZ, local.z);
            }

            Rect safe = AugmentParchmentVisuals.ContentSafeRect;
            float cardWidth = (maxX - minX) / Mathf.Max(.01f, safe.width);
            float cardHeight = (maxZ - minZ) / Mathf.Max(.01f, safe.height);

            overlayRect.gameObject.SetActive(true);
            overlayRect.localPosition = new Vector3(
                (minX + maxX) * .5f - (safe.center.x - .5f) * cardWidth,
                OverlayLift,
                (minZ + maxZ) * .5f - (safe.center.y - .5f) * cardHeight);
            overlayRect.sizeDelta = new Vector2(cardWidth, cardHeight) * CanvasUnitsPerWorldUnit;

            if (card != null)
            {
                float scale = cardWidth * CanvasUnitsPerWorldUnit / CardPixelWidth;
                card.transform.localScale = new Vector3(scale, scale, 1f);
            }

            if (depthMask != null)
            {
                depthMask.gameObject.SetActive(true);
                Vector3 overlayPosition = overlayRect.localPosition;
                depthMask.localPosition = new Vector3(overlayPosition.x, DepthMaskLift, overlayPosition.z);
                depthMask.localScale = new Vector3(cardWidth, cardHeight, 1f);
            }
        }

        /// <summary>
        /// Crisp UI 카메라에만 카드의 깊이를 알려 주는 판을 만든다.
        ///
        /// 족보 표와 다른 카드 글자는 픽셀 필터를 피해 CrispUI 레이어에 있고, 그 카메라는 월드 물체를
        /// 하나도 찍지 않아 깊이 버퍼가 비어 있다. 그래서 앞에 카드가 있어도 글자가 그대로 그려진다.
        /// 이 판은 색을 쓰지 않고 깊이만 남기므로, 뒤에 있는 글자만 이 카드 모양대로 잘려 나간다.
        /// </summary>
        private static Transform CreateDepthMask(Transform parent)
        {
            GameObject maskObject = new("Crisp Depth Mask", typeof(MeshFilter), typeof(MeshRenderer));
            maskObject.layer = TesseraLayers.CrispUI;
            maskObject.hideFlags = HideFlags.DontSave;
            maskObject.transform.SetParent(parent, false);
            maskObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            maskObject.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            MeshRenderer renderer = maskObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = EnsureDepthMaskMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return maskObject.transform;
        }

        private static Material EnsureDepthMaskMaterial()
        {
            if (sharedDepthMaskMaterial != null) return sharedDepthMaskMaterial;

            Shader shader = Shader.Find(DepthMaskShaderName);
            if (shader == null) return null;

            sharedDepthMaskMaterial = new Material(shader)
            {
                name = "Runtime Crisp Depth Mask",
                hideFlags = HideFlags.HideAndDontSave
            };
            return sharedDepthMaskMaterial;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            for (int i = 0; i < target.transform.childCount; i++)
            {
                SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
            }
        }

        /// <summary>카드 UI는 런타임에 다시 만들므로 씬과 프리팹에 직렬화하지 않는다.</summary>
        private static void MarkDontSaveRecursively(GameObject target)
        {
            target.hideFlags = HideFlags.DontSave;
            for (int i = 0; i < target.transform.childCount; i++)
            {
                MarkDontSaveRecursively(target.transform.GetChild(i).gameObject);
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
            if (depthMask != null) depthMask.gameObject.SetActive(false);
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
