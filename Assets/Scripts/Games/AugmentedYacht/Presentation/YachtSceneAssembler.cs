using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Tessera.Tabletop;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 씬의 카메라·캔버스·HUD를 찾거나 세운다(M10-T8).
    ///
    /// 배치는 씬이 소유한다(M9). 여기서 만드는 것은 씬이 비어 있을 때의 최소 구성과,
    /// 프리팹으로 굽지 않는 화면 UI(상태 문구, 타이머, 시작·결과 오버레이, 디버그 버튼)뿐이다.
    /// 테이블 프롭은 만들지 않고 참조만 잇는다.
    /// </summary>
    public static class YachtSceneAssembler
    {
        /// <summary>씬에서 찾거나 새로 세운 화면 구성 요소.</summary>
        public sealed class SceneRefs
        {
            public Transform LayoutRoot;
            public Camera WorldCamera;
            public Camera PresentationCamera;
            public RawImage GameImage;
            public RectTransform GameImageRect;
            public RectTransform GameAreaRect;
            public Text StatusText;
        }

        /// <summary>턴 진행 중 갱신되는 화면 UI.</summary>
        public sealed class HudRefs
        {
            public Transform Canvas;
            public Text TimerText;
            public GameObject StartOverlay;
            public GameObject ResultOverlay;
            public Text ResultText;
        }

        /// <summary>HUD 버튼이 호출할 동작. 컨트롤러가 채운다.</summary>
        public sealed class HudActions
        {
            public Action ToggleResolution;
            public Action ToggleKeyLight;
            public Action AdvanceRuneLighting;
            public Action CycleRuneStones;
            public Action StartNormalGame;
            public Action StartAugmentedGame;
            public Action RestartGame;
            public Action TogglePixelEdge;
            public Action CycleQuantize;
            public Action ToggleRenderStyle;
            public Func<string> KeyLightPresetName;
            public Func<string> ResolutionPresetLabel;
            public Func<bool> PixelEdgeEnabled;
            public Func<string> QuantizeModeName;
            public Func<string> RenderStyleName;
            public Func<string> RuneProgressText;
            public Func<string> RuneStoneText;
        }

        /// <summary>씬에 구워졌거나 옛 코드가 만들던 가로 배치 디버그 버튼. 이제는 패널이 대신한다.</summary>
        private static readonly string[] LegacyDebugButtonNames =
        {
            "Debug", "KeyLightToggle", "RuneFxDebug", "RuneStoneDebug",
            "PixelEdgeToggle", "QuantizeToggle", "RenderStyleToggle"
        };

        private const float CameraPitchAngle = 75.0f;

        /// <summary>필라이트가 가져가는 밝기 비중. 런타임에는 YachtLightingRig가 프리셋 강도로 다시 나눈다.</summary>
        private const float FillLightShare = 0.25f;

        /// <summary>씬에 이미 배치된 레이아웃을 찾는다. 하나라도 없으면 false를 준다.</summary>
        public static bool ResolveExistingLayout(SceneRefs refs)
        {
            GameObject layoutObject = GameObject.Find("Graphics Layout");
            GameObject worldCameraObject = FindWorldCameraObject();
            GameObject displayCameraObject = GameObject.Find("Display 1 Camera");
            GameObject gameAreaObject = GameObject.Find("Game Area");
            GameObject imageObject = GameObject.Find("Point Upscale");
            GameObject statusObject = GameObject.Find("Status");

            if (layoutObject == null || worldCameraObject == null || displayCameraObject == null || gameAreaObject == null || imageObject == null)
            {
                return false;
            }

            refs.LayoutRoot = layoutObject.transform;
            refs.WorldCamera = worldCameraObject.GetComponent<Camera>();
            refs.PresentationCamera = displayCameraObject.GetComponent<Camera>();
            refs.GameAreaRect = gameAreaObject.GetComponent<RectTransform>();
            refs.GameImageRect = imageObject.GetComponent<RectTransform>();
            refs.GameImage = imageObject.GetComponent<RawImage>();
            refs.StatusText = statusObject != null ? statusObject.GetComponent<Text>() : null;
            imageObject.SetActive(true);

            return refs.WorldCamera != null && refs.PresentationCamera != null && refs.GameImage != null;
        }

        /// <summary>씬이 비어 있을 때의 월드 최소 구성. 구버전 카메라와 키라이트를 지우고 다시 만든다.</summary>
        public static void BuildWorld(SceneRefs refs, Transform owner, float centerX)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.44f, 0.40f);

            refs.LayoutRoot = EnsureLayoutRoot(refs.LayoutRoot, owner);

            DestroySceneObjectsNamed("Full Field World Camera", "Low Resolution World Camera");

            Transform existingKeyLight = refs.LayoutRoot != null ? refs.LayoutRoot.Find("Key Light") : null;
            if (existingKeyLight != null) DestroyObject(existingKeyLight.gameObject);

            Transform existingFillLight = refs.LayoutRoot != null ? refs.LayoutRoot.Find("Fill Light") : null;
            if (existingFillLight != null) DestroyObject(existingFillLight.gameObject);

            GameObject cameraObject = new("Full Field World Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(refs.LayoutRoot, false);
            refs.WorldCamera = cameraObject.GetComponent<Camera>();
            ApplyWorldCameraFraming(refs.WorldCamera, centerX);
            refs.WorldCamera.nearClipPlane = 0.1f;
            refs.WorldCamera.farClipPlane = 40f;
            refs.WorldCamera.clearFlags = CameraClearFlags.SolidColor;
            refs.WorldCamera.backgroundColor = new Color(0.06f, 0.045f, 0.04f);
            refs.WorldCamera.allowHDR = false;
            refs.WorldCamera.allowMSAA = false;

            GameObject lightObject = new("Key Light", typeof(Light));
            Light key = lightObject.GetComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.93f, 0.78f);
            key.intensity = 1.45f * (1f - FillLightShare);
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.58f;
            key.shadowBias = 0.005f;
            key.shadowNormalBias = 0.03f;
            lightObject.transform.rotation = Quaternion.Euler(60f, -35f, 0f);
            lightObject.transform.SetParent(refs.LayoutRoot, true);

            // 키라이트가 비추지 못하는 -X 방향 면(트레이 안쪽 오른쪽 벽 등)을 채운다.
            // yaw만 뒤집고 그림자는 끈다. 자세한 이유는 YachtLightingRig.ConfigureFillLight 참고.
            GameObject fillObject = new("Fill Light", typeof(Light));
            Light fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(1f, 0.93f, 0.78f);
            fill.intensity = 1.45f * FillLightShare;
            fill.shadows = LightShadows.None;
            fillObject.transform.rotation = Quaternion.Euler(60f, 35f, 0f);
            fillObject.transform.SetParent(refs.LayoutRoot, true);
        }

        /// <summary>월드 카메라의 위치·각도·직교 크기를 규정값으로 되돌린다.</summary>
        public static void ApplyWorldCameraFraming(Camera worldCamera, float centerX)
        {
            if (worldCamera == null) return;
            worldCamera.transform.position = new Vector3(centerX, 11.5f, -3.1f);
            worldCamera.transform.rotation = Quaternion.Euler(CameraPitchAngle, 0f, 0f);
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 8.2f;
        }

        /// <summary>씬이 비어 있을 때의 프레젠테이션 캔버스와 업스케일 경로를 세운다.</summary>
        public static void BuildPresentation(SceneRefs refs, Transform owner, YachtCameraRig cameraRig)
        {
            EnsureEventSystem();
            DestroySceneObjectsNamed("Pixel Presentation", "Display 1 Camera");

            GameObject canvasObject = new("Pixel Presentation", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(owner, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            cameraRig.CreatePresentationCamera();
            refs.PresentationCamera = cameraRig.PresentationCamera;

            GameObject gameArea = new("Game Area", typeof(RectTransform));
            gameArea.transform.SetParent(canvasObject.transform, false);
            refs.GameAreaRect = gameArea.GetComponent<RectTransform>();
            refs.GameAreaRect.anchorMin = Vector2.zero;
            refs.GameAreaRect.anchorMax = Vector2.one;
            refs.GameAreaRect.offsetMin = refs.GameAreaRect.offsetMax = Vector2.zero;

            GameObject imageObject = new("Point Upscale", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(gameArea.transform, false);
            refs.GameImageRect = imageObject.GetComponent<RectTransform>();
            refs.GameImageRect.anchorMin = refs.GameImageRect.anchorMax = new Vector2(0.5f, 0.5f);
            refs.GameImageRect.pivot = new Vector2(0.5f, 0.5f);
            refs.GameImage = imageObject.GetComponent<RawImage>();
            refs.GameImage.raycastTarget = false;

            cameraRig.SetGameImage(refs.GameImage, refs.GameImageRect);
            cameraRig.EnsureUpscaleMaterial();
            imageObject.SetActive(true);

            refs.StatusText = YachtHudFactory.CreateText(canvasObject.transform, "Status", "", new Vector2(0f, -20f),
                new Vector2(600f, 30f), new Vector2(0.5f, 1f), 15, TextAnchor.MiddleCenter);
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// 옛 가로 배치 디버그 버튼을 화면에서 치운다. 같은 동작을 <see cref="YachtDebugPanel"/>이 이어받았다.
        /// 씬에 구워진 오브젝트는 지우지 않고 끄기만 한다.
        /// </summary>
        public static void HideLegacyDebugButtons()
        {
            for (int i = 0; i < LegacyDebugButtonNames.Length; i++)
            {
                GameObject button = GameObject.Find(LegacyDebugButtonNames[i]);
                if (button != null) button.SetActive(false);
            }
        }

        /// <summary>타이머 문구와 시작·결과 오버레이를 다시 만든다. 이 셋은 프리팹으로 굽지 않는다.</summary>
        public static HudRefs BuildGameFlowUi(HudActions actions)
        {
            GameObject canvasObject = GameObject.Find("Pixel Presentation");
            if (canvasObject == null) return null;

            DestroyChild(canvasObject.transform, "Yacht Game Start Overlay");
            DestroyChild(canvasObject.transform, "Yacht Game Result Overlay");
            DestroyChild(canvasObject.transform, "Yacht Turn Timer Text");

            var hud = new HudRefs();
            hud.Canvas = canvasObject.transform;
            hud.TimerText = YachtHudFactory.CreateText(canvasObject.transform, "Yacht Turn Timer Text", "--", Vector2.zero,
                new Vector2(120f, 46f), new Vector2(0.5f, 0.5f), 30, TextAnchor.MiddleCenter);
            hud.TimerText.color = new Color32(255, 226, 151, 255);

            hud.StartOverlay = YachtHudFactory.CreateFullScreenOverlay(canvasObject.transform, "Yacht Game Start Overlay");
            Text title = YachtHudFactory.CreateText(hud.StartOverlay.transform, "Title", "요트 다이스", new Vector2(0f, 90f),
                new Vector2(620f, 90f), new Vector2(0.5f, 0.5f), 42, TextAnchor.MiddleCenter);
            title.color = new Color32(255, 222, 151, 255);
            YachtHudFactory.CreateButton(hud.StartOverlay.transform, "Start Normal Yacht Game", "일반 요트", new Vector2(0f, -5f),
                new Vector2(260f, 64f), new Vector2(0.5f, 0.5f), () => actions.StartNormalGame());
            YachtHudFactory.CreateButton(hud.StartOverlay.transform, "Start Augmented Yacht Game", "증강 요트", new Vector2(0f, -85f),
                new Vector2(260f, 64f), new Vector2(0.5f, 0.5f), () => actions.StartAugmentedGame());

            hud.ResultOverlay = YachtHudFactory.CreateFullScreenOverlay(canvasObject.transform, "Yacht Game Result Overlay");
            hud.ResultText = YachtHudFactory.CreateText(hud.ResultOverlay.transform, "Result", "", new Vector2(0f, 35f),
                new Vector2(720f, 150f), new Vector2(0.5f, 0.5f), 36, TextAnchor.MiddleCenter);
            hud.ResultText.color = new Color32(255, 222, 151, 255);
            YachtHudFactory.CreateButton(hud.ResultOverlay.transform, "Restart Yacht Game", "다시 시작", new Vector2(0f, -105f),
                new Vector2(240f, 64f), new Vector2(0.5f, 0.5f), () => actions.RestartGame());
            hud.ResultOverlay.SetActive(false);
            return hud;
        }

        public static void EnsureEventSystem()
        {
            if (!Application.isPlaying || UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;

            GameObject events = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            UnityEngine.Object.DontDestroyOnLoad(events);
        }

        /// <summary>오디오 리스너를 월드 카메라 하나로 줄인다. 중복 리스너는 경고를 낸다.</summary>
        public static Camera EnsureSingleAudioListener(Camera worldCamera)
        {
            if (worldCamera == null)
            {
                GameObject camObj = FindWorldCameraObject();
                if (camObj != null) worldCamera = camObj.GetComponent<Camera>();
            }

            AudioListener[] allListeners = Resources.FindObjectsOfTypeAll<AudioListener>();
            bool foundPrimary = false;
            foreach (AudioListener al in allListeners)
            {
                if (al == null) continue;
#if UNITY_EDITOR
                if (UnityEditor.EditorUtility.IsPersistent(al.gameObject)) continue;
#endif
                if (!foundPrimary && worldCamera != null && al.gameObject == worldCamera.gameObject)
                {
                    al.enabled = true;
                    foundPrimary = true;
                }
                else
                {
                    DestroyObject(al);
                }
            }

            if (!foundPrimary && worldCamera != null)
            {
                AudioListener al = worldCamera.GetComponent<AudioListener>();
                if (al == null) al = worldCamera.gameObject.AddComponent<AudioListener>();
                al.enabled = true;
            }
            return worldCamera;
        }

        public static Transform EnsureLayoutRoot(Transform current, Transform owner)
        {
            if (current != null) return current;

            GameObject existing = GameObject.Find("Graphics Layout");
            if (existing != null) return existing.transform;

            GameObject root = new("Graphics Layout");
            root.transform.SetParent(owner, false);
            return root.transform;
        }

        private static GameObject FindWorldCameraObject()
        {
            return GameObject.Find("Full Field World Camera") ?? GameObject.Find("Low Resolution World Camera");
        }

        private static void DestroyChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null) DestroyObject(child.gameObject);
        }

        private static void DestroySceneObjectsNamed(params string[] names)
        {
            GameObject[] allSceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject go in allSceneObjects)
            {
                if (go == null) continue;
#if UNITY_EDITOR
                if (UnityEditor.EditorUtility.IsPersistent(go)) continue;
#endif
                foreach (string name in names)
                {
                    if (go.name == name)
                    {
                        DestroyObject(go);
                        break;
                    }
                }
            }
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
