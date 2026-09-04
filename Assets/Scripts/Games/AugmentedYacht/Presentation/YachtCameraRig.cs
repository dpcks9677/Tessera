using Tessera.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 저해상도 렌더와 픽셀 업스케일, 그리고 필터를 거치지 않는 Crisp UI 합성을 담당한다(M10-T2).
    ///
    /// 카메라 오브젝트 자체는 씬이 소유한다. 이 클래스는 그 카메라들이 무엇을 어디에 그리는지,
    /// 즉 렌더 타깃과 업스케일 재질, 화면 크기 대응만 관리한다.
    /// </summary>
    public sealed class YachtCameraRig : MonoBehaviour
    {
        private const string CrispUiCameraName = "Crisp UI Camera";
        private const string CrispUiOverlayName = "Crisp UI Overlay";

        private static readonly Vector2Int OutputResolution = new(1920, 1080);
        private static readonly Color DarkCharcoalBackground = new(0.06f, 0.05f, 0.07f);

        private Camera worldCamera;
        private Camera presentationCamera;
        private RawImage gameImage;
        private RectTransform gameImageRect;
        private Transform layoutRoot;
        private Shader upscaleShader;

        private Material upscaleMaterial;
        private RenderTexture lowResolutionTarget;

        private Camera crispUiCamera;
        private RenderTexture crispUiTarget;
        private RawImage crispUiImage;
        private Vector2Int crispUiScreenSize;

        private Vector2Int internalResolution = new(640, 360);

        public Camera WorldCamera => worldCamera;
        public Camera PresentationCamera => presentationCamera;
        public Camera CrispUiCamera => crispUiCamera;
        public Vector2Int InternalResolution => internalResolution;

        /// <summary>Crisp UI 카메라가 준비되면 알린다. 월드 스페이스 캔버스의 이벤트 카메라로 쓴다.</summary>
        public event System.Action<Camera> CrispUiCameraReady;
        public Material UpscaleMaterial => upscaleMaterial;
        public RawImage GameImage => gameImage;

        /// <summary>컨트롤러가 씬에서 찾은 구성 요소를 넘겨준다.</summary>
        public void Bind(Camera world, Camera presentation, RawImage image, RectTransform imageRect, Transform layout, Shader shader)
        {
            worldCamera = world;
            presentationCamera = presentation;
            gameImage = image;
            gameImageRect = imageRect;
            layoutRoot = layout;
            if (shader != null) upscaleShader = shader;
        }

        public void SetGameImage(RawImage image, RectTransform imageRect)
        {
            gameImage = image;
            gameImageRect = imageRect;
        }

        /// <summary>픽셀아트 내부 해상도를 바꾼다. 실제 렌더 타깃 크기는 그대로다.</summary>
        public void SetInternalResolution(Vector2Int resolution)
        {
            internalResolution = resolution;
            ApplyRenderSettings();
        }

        public void EnsureUpscaleMaterial()
        {
            if (upscaleMaterial != null || gameImage == null) return;

            upscaleMaterial = new Material(upscaleShader != null ? upscaleShader : Shader.Find("UI/Default"));
            gameImage.material = upscaleMaterial;
        }

        /// <summary>렌더 타깃과 업스케일 재질을 정리한다. 컨트롤러의 OnDestroy에서 부른다.</summary>
        public void Dispose()
        {
            if (worldCamera != null) worldCamera.targetTexture = null;
            ReleaseTarget(ref lowResolutionTarget);

            if (crispUiCamera != null) crispUiCamera.targetTexture = null;
            ReleaseTarget(ref crispUiTarget);

            if (upscaleMaterial != null)
            {
                if (Application.isPlaying) Destroy(upscaleMaterial);
                else DestroyImmediate(upscaleMaterial);
                upscaleMaterial = null;
            }
        }

        private void ReleaseTarget(ref RenderTexture target)
        {
            if (target == null) return;

            target.Release();
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
            target = null;
        }

        public void CreatePresentationCamera()
        {
            GameObject cameraObject = new("Display 1 Camera", typeof(Camera));
            cameraObject.transform.SetParent(layoutRoot, false);
            presentationCamera = cameraObject.GetComponent<Camera>();
            presentationCamera.targetDisplay = 0;
            presentationCamera.clearFlags = CameraClearFlags.SolidColor;
            presentationCamera.backgroundColor = DarkCharcoalBackground;
            presentationCamera.cullingMask = 0;
            presentationCamera.depth = -100f;
            presentationCamera.nearClipPlane = 0.01f;
            presentationCamera.farClipPlane = 1f;
            presentationCamera.allowHDR = false;
            presentationCamera.allowMSAA = false;
        }

        public void CreateRenderTarget()
        {
            if (worldCamera != null) worldCamera.targetTexture = null;
            if (lowResolutionTarget != null)
            {
                lowResolutionTarget.Release();
                Destroy(lowResolutionTarget);
            }

            lowResolutionTarget = new RenderTexture(OutputResolution.x, OutputResolution.y, 24, RenderTextureFormat.ARGB32)
            {
                name = $"Dice PoC Full Field {OutputResolution.x}x{OutputResolution.y}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            lowResolutionTarget.Create();
            worldCamera.targetTexture = lowResolutionTarget;
            if (gameImage != null)
            {
                gameImage.gameObject.SetActive(true);
                gameImage.texture = lowResolutionTarget;
            }
            FitFullScreen();
            EnsureCrispUiPipeline();
        }

        /// <summary>
        /// 픽셀 필터를 거치지 않는 UI 경로를 구성한다(M9.5).
        ///
        /// 월드 카메라는 <see cref="TesseraLayers.CrispUI"/> 레이어를 찍지 않고, 같은 투영을 쓰는
        /// 전용 카메라가 그 레이어만 화면 해상도 렌더 타깃에 그린다. 결과를 픽셀 이미지 위에 겹치면
        /// 배경은 픽셀아트로, 글자는 원본 해상도로 남는다.
        ///
        /// 두 카메라의 투영이 같으므로 월드 스페이스 UI는 별도 정렬 계산 없이 정확히 겹친다.
        /// </summary>
        public void EnsureCrispUiPipeline()
        {
            if (!SetupCrispUiSceneObjects()) return;

            EnsureCrispUiTarget();
            EnsureCrispUiOverlay();

            // 월드 스페이스 UI가 이 카메라보다 먼저 만들어지므로, 준비되면 알려 붙이게 한다.
            CrispUiCameraReady?.Invoke(crispUiCamera);
        }

        /// <summary>
        /// 씬이 소유해야 하는 부분만 구성한다. 컬링 마스크와 카메라·오버레이 오브젝트가 여기 해당한다.
        ///
        /// 렌더 타깃은 화면 해상도에 묶여 있어 에셋으로 저장할 수 없으므로 제외한다.
        /// 에디터 메뉴로 한 번 실행해 씬에 굽고, 이후에는 씬에 저장된 상태를 그대로 쓴다.
        /// </summary>
        [ContextMenu("Setup Crisp UI Pipeline")]
        public bool SetupCrispUiSceneObjects()
        {
            if (worldCamera == null)
            {
                GameObject found = GameObject.Find("Full Field World Camera") ?? GameObject.Find("Low Resolution World Camera");
                worldCamera = found != null ? found.GetComponent<Camera>() : null;
            }
            if (worldCamera == null) return false;

            worldCamera.cullingMask &= ~TesseraLayers.Mask(TesseraLayers.CrispUI);

            EnsureCrispUiCamera();

            if (gameImageRect == null)
            {
                GameObject upscale = GameObject.Find("Point Upscale");
                gameImageRect = upscale != null ? upscale.GetComponent<RectTransform>() : null;
            }
            EnsureCrispUiOverlay();
            return true;
        }

        private void EnsureCrispUiCamera()
        {
            if (crispUiCamera == null)
            {
                Transform existing = worldCamera.transform.Find(CrispUiCameraName);
                crispUiCamera = existing != null ? existing.GetComponent<Camera>() : null;
            }

            if (crispUiCamera == null)
            {
                GameObject cameraObject = new(CrispUiCameraName, typeof(Camera));
                cameraObject.transform.SetParent(worldCamera.transform, false);
                crispUiCamera = cameraObject.GetComponent<Camera>();
            }

            // 투영을 월드 카메라와 일치시킨 뒤 이 카메라만의 설정을 덮어쓴다.
            crispUiCamera.CopyFrom(worldCamera);
            crispUiCamera.transform.localPosition = Vector3.zero;
            crispUiCamera.transform.localRotation = Quaternion.identity;
            crispUiCamera.transform.localScale = Vector3.one;
            crispUiCamera.cullingMask = TesseraLayers.Mask(TesseraLayers.CrispUI);
            crispUiCamera.clearFlags = CameraClearFlags.SolidColor;
            crispUiCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            crispUiCamera.depth = worldCamera.depth + 1f;

            // CopyFrom은 월드 카메라의 렌더 타깃까지 복사한다. 그대로 두면 두 카메라가 같은
            // 타깃을 공유해, 나중에 그리는 이쪽이 투명색으로 지우며 월드 화면을 통째로 날린다.
            // 이 카메라는 언제나 자기 전용 타깃만 쓴다.
            crispUiCamera.targetTexture = crispUiTarget;

            // 전용 타깃이 없으면 끈다. 타깃 없이 켜 두면 화면에 직접 그리며 색을 지우고,
            // URP Base 카메라는 Depth only 클리어를 지원하지 않아 켜 둔 채로는 피할 수 없다.
            crispUiCamera.enabled = crispUiTarget != null;
            crispUiCamera.allowHDR = false;
            crispUiCamera.allowMSAA = false;

            AudioListener listener = crispUiCamera.GetComponent<AudioListener>();
            if (listener != null)
            {
                if (Application.isPlaying) Destroy(listener);
                else DestroyImmediate(listener);
            }
        }

        /// <summary>
        /// Crisp UI 렌더 타깃을 화면 해상도로 맞춘다.
        ///
        /// 월드 스페이스 캔버스의 클릭 판정은 카메라의 <c>pixelRect</c>를 기준으로 하는데,
        /// 렌더 타깃이 지정되면 그 크기가 곧 <c>pixelRect</c>가 된다. 따라서 화면과 1:1이 아니면
        /// 클릭 위치가 어긋난다. 화면 크기가 바뀔 때마다 다시 만들어야 한다.
        /// </summary>
        private void EnsureCrispUiTarget()
        {
            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            if (crispUiTarget != null && crispUiTarget.width == width && crispUiTarget.height == height)
            {
                crispUiCamera.targetTexture = crispUiTarget;
                crispUiCamera.enabled = true;
                return;
            }

            if (crispUiCamera != null) crispUiCamera.targetTexture = null;
            if (crispUiTarget != null)
            {
                crispUiTarget.Release();
                if (Application.isPlaying) Destroy(crispUiTarget);
                else DestroyImmediate(crispUiTarget);
            }

            crispUiTarget = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = $"Crisp UI {width}x{height}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            crispUiTarget.Create();
            crispUiCamera.targetTexture = crispUiTarget;
            crispUiCamera.enabled = true;
            crispUiScreenSize = new Vector2Int(width, height);
            if (crispUiImage != null) crispUiImage.texture = crispUiTarget;
        }

        private void EnsureCrispUiOverlay()
        {
            if (gameImageRect == null) return;

            if (crispUiImage == null)
            {
                Transform parent = gameImageRect.parent;
                Transform existing = parent != null ? parent.Find(CrispUiOverlayName) : null;
                if (existing != null)
                {
                    crispUiImage = existing.GetComponent<RawImage>();
                }
                else
                {
                    GameObject overlay = new(CrispUiOverlayName, typeof(RectTransform), typeof(RawImage));
                    overlay.transform.SetParent(parent, false);
                    crispUiImage = overlay.GetComponent<RawImage>();
                }
            }

            // 픽셀 이미지 바로 다음 형제여야 그 위에 그려진다.
            crispUiImage.transform.SetSiblingIndex(gameImageRect.GetSiblingIndex() + 1);

            RectTransform rect = crispUiImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            // 전체 화면 이미지가 이벤트를 가로채면 아래 월드 스페이스 캔버스가 클릭을 못 받는다.
            crispUiImage.raycastTarget = false;
            crispUiImage.material = null;
            crispUiImage.texture = crispUiTarget;
            crispUiImage.gameObject.SetActive(true);
            // 텍스처가 없는 RawImage는 흰색 불투명 사각형으로 그려져 화면 전체를 덮는다.
            // 렌더 타깃은 런타임에만 만들어지므로 에디터에서는 꺼 둔다.
            crispUiImage.enabled = crispUiTarget != null;
        }

        public void ApplyRenderSettings()
        {
            if (upscaleMaterial != null)
            {
                if (upscaleMaterial.HasProperty("_Quantize")) upscaleMaterial.SetFloat("_Quantize", 0f);
                upscaleMaterial.SetVector("_VirtualResolution", new Vector4(internalResolution.x, internalResolution.y, 0f, 0f));
            }
        }

        public void FitFullScreen()
        {
            if (gameImageRect == null) return;
            gameImageRect.anchorMin = Vector2.zero;
            gameImageRect.anchorMax = Vector2.one;
            gameImageRect.anchoredPosition = Vector2.zero;
            gameImageRect.sizeDelta = Vector2.zero;
        }

        /// <summary>
        /// 화면 크기가 바뀌면 Crisp UI 렌더 타깃을 다시 만든다.
        /// 타깃 해상도가 화면과 어긋나면 월드 스페이스 캔버스의 클릭 위치가 밀린다.
        /// </summary>
        public void SyncCrispUiTargetToScreen()
        {
            if (crispUiCamera == null) return;

            Vector2Int size = new(Screen.width, Screen.height);
            if (size == crispUiScreenSize) return;

            crispUiScreenSize = size;
            EnsureCrispUiTarget();
            EnsureCrispUiOverlay();
        }
    }
}
