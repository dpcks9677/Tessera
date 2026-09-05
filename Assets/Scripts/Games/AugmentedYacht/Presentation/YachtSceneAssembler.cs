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
            public Func<string> KeyLightPresetName;
            public Func<string> ResolutionPresetLabel;
            public Func<bool> PixelEdgeEnabled;
            public Func<string> QuantizeModeName;
        }

        /// <summary>디버그 버튼 참조. 라벨을 갱신하려면 들고 있어야 한다.</summary>
        public sealed class DebugButtons
        {
            public Button KeyLightToggle;
            public Button RuneFx;
            public Button RuneStone;
            public Button PixelEdgeToggle;
            public Button QuantizeToggle;
        }

        private const float CameraPitchAngle = 75.0f;

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

            Transform existingLight = refs.LayoutRoot != null ? refs.LayoutRoot.Find("Key Light") : null;
            if (existingLight != null) DestroyObject(existingLight.gameObject);

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
            key.intensity = 1.45f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.58f;
            key.shadowBias = 0.005f;
            key.shadowNormalBias = 0.03f;
            lightObject.transform.rotation = Quaternion.Euler(60f, -35f, 0f);
            lightObject.transform.SetParent(refs.LayoutRoot, true);
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
        public static DebugButtons BuildPresentation(SceneRefs refs, Transform owner, YachtCameraRig cameraRig, HudActions actions)
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

            YachtHudFactory.CreateButton(canvasObject.transform, "Debug", ResolutionLabel(actions), new Vector2(18f, -18f),
                new Vector2(130f, 38f), new Vector2(0f, 1f), () => actions.ToggleResolution());

            var buttons = new DebugButtons
            {
                RuneFx = YachtHudFactory.CreateButton(canvasObject.transform, "RuneFxDebug", "Runes: 0/12", new Vector2(333f, -18f),
                    new Vector2(140f, 38f), new Vector2(0f, 1f), () => actions.AdvanceRuneLighting()),
                RuneStone = YachtHudFactory.CreateButton(canvasObject.transform, "RuneStoneDebug", "Stones: 0/4", new Vector2(483f, -18f),
                    new Vector2(150f, 38f), new Vector2(0f, 1f), () => actions.CycleRuneStones()),
                KeyLightToggle = YachtHudFactory.CreateButton(canvasObject.transform, "KeyLightToggle", $"Light: {actions.KeyLightPresetName()}",
                    new Vector2(158f, -18f), new Vector2(165f, 38f), new Vector2(0f, 1f), () => actions.ToggleKeyLight()),
                PixelEdgeToggle = YachtHudFactory.CreateButton(canvasObject.transform, "PixelEdgeToggle", PixelEdgeLabel(actions),
                    new Vector2(643f, -18f), new Vector2(135f, 38f), new Vector2(0f, 1f), () => actions.TogglePixelEdge()),
                QuantizeToggle = YachtHudFactory.CreateButton(canvasObject.transform, "QuantizeToggle", QuantizeLabel(actions),
                    new Vector2(788f, -18f), new Vector2(155f, 38f), new Vector2(0f, 1f), () => actions.CycleQuantize())
            };

            refs.StatusText = YachtHudFactory.CreateText(canvasObject.transform, "Status", "", new Vector2(0f, -20f),
                new Vector2(600f, 30f), new Vector2(0.5f, 1f), 15, TextAnchor.MiddleCenter);
            Canvas.ForceUpdateCanvases();
            return buttons;
        }

        /// <summary>씬에 이미 있는 디버그 버튼을 찾아 동작을 다시 건다. 없으면 만든다.</summary>
        public static DebugButtons BindPresentationActions(HudActions actions)
        {
            var buttons = new DebugButtons
            {
                KeyLightToggle = GameObject.Find("KeyLightToggle")?.GetComponent<Button>(),
                RuneFx = GameObject.Find("RuneFxDebug")?.GetComponent<Button>(),
                RuneStone = GameObject.Find("RuneStoneDebug")?.GetComponent<Button>(),
                PixelEdgeToggle = GameObject.Find("PixelEdgeToggle")?.GetComponent<Button>(),
                QuantizeToggle = GameObject.Find("QuantizeToggle")?.GetComponent<Button>()
            };

            Button resolutionButton = GameObject.Find("Debug")?.GetComponent<Button>();
            if (resolutionButton != null)
            {
                resolutionButton.onClick.RemoveAllListeners();
                resolutionButton.onClick.AddListener(() => actions.ToggleResolution());

                // 씬에 구워진 문구는 옛 프리셋 값이다. 프리셋에서 파생한 문구로 덮어쓴다.
                Text resolutionLabel = resolutionButton.GetComponentInChildren<Text>();
                if (resolutionLabel != null) resolutionLabel.text = ResolutionLabel(actions);
            }

            GameObject canvasObject = GameObject.Find("Pixel Presentation");
            if (canvasObject != null)
            {
                if (buttons.KeyLightToggle == null)
                {
                    buttons.KeyLightToggle = YachtHudFactory.CreateButton(canvasObject.transform, "KeyLightToggle",
                        $"Light: {actions.KeyLightPresetName()}", new Vector2(158f, -18f), new Vector2(165f, 38f), new Vector2(0f, 1f),
                        () => actions.ToggleKeyLight());
                }
                if (buttons.RuneFx == null)
                {
                    buttons.RuneFx = YachtHudFactory.CreateButton(canvasObject.transform, "RuneFxDebug", "Runes: 0/12",
                        new Vector2(333f, -18f), new Vector2(140f, 38f), new Vector2(0f, 1f), () => actions.AdvanceRuneLighting());
                }
                if (buttons.RuneStone == null)
                {
                    buttons.RuneStone = YachtHudFactory.CreateButton(canvasObject.transform, "RuneStoneDebug", "Stones: 0/4",
                        new Vector2(483f, -18f), new Vector2(150f, 38f), new Vector2(0f, 1f), () => actions.CycleRuneStones());
                }
                if (buttons.PixelEdgeToggle == null)
                {
                    buttons.PixelEdgeToggle = YachtHudFactory.CreateButton(canvasObject.transform, "PixelEdgeToggle",
                        PixelEdgeLabel(actions), new Vector2(643f, -18f), new Vector2(135f, 38f), new Vector2(0f, 1f),
                        () => actions.TogglePixelEdge());
                }
                if (buttons.QuantizeToggle == null)
                {
                    buttons.QuantizeToggle = YachtHudFactory.CreateButton(canvasObject.transform, "QuantizeToggle",
                        QuantizeLabel(actions), new Vector2(788f, -18f), new Vector2(155f, 38f), new Vector2(0f, 1f),
                        () => actions.CycleQuantize());
                }
            }

            if (buttons.KeyLightToggle != null)
            {
                buttons.KeyLightToggle.onClick.RemoveAllListeners();
                buttons.KeyLightToggle.onClick.AddListener(() => actions.ToggleKeyLight());
                Text label = buttons.KeyLightToggle.GetComponentInChildren<Text>();
                if (label != null) label.text = $"Light: {actions.KeyLightPresetName()}";
            }
            if (buttons.RuneFx != null)
            {
                buttons.RuneFx.onClick.RemoveAllListeners();
                buttons.RuneFx.onClick.AddListener(() => actions.AdvanceRuneLighting());
            }
            if (buttons.RuneStone != null)
            {
                buttons.RuneStone.onClick.RemoveAllListeners();
                buttons.RuneStone.onClick.AddListener(() => actions.CycleRuneStones());
            }
            if (buttons.PixelEdgeToggle != null)
            {
                buttons.PixelEdgeToggle.onClick.RemoveAllListeners();
                buttons.PixelEdgeToggle.onClick.AddListener(() => actions.TogglePixelEdge());
                SetPixelEdgeLabel(buttons, actions.PixelEdgeEnabled != null && actions.PixelEdgeEnabled());
            }
            if (buttons.QuantizeToggle != null)
            {
                buttons.QuantizeToggle.onClick.RemoveAllListeners();
                buttons.QuantizeToggle.onClick.AddListener(() => actions.CycleQuantize());
                SetQuantizeLabel(buttons, actions.QuantizeModeName != null ? actions.QuantizeModeName() : "Off");
            }

            return buttons;
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

        /// <summary>디버그 버튼 라벨을 룬 상태에 맞춘다.</summary>
        public static void UpdateRuneDebugLabels(DebugButtons buttons, RunicSlateMatrix runicSlateMatrix)
        {
            if (buttons == null) return;

            int runeProgress = runicSlateMatrix != null ? runicSlateMatrix.OuterRuneProgress : 0;
            int stoneCount = runicSlateMatrix != null ? runicSlateMatrix.ExtraTurnCount : 0;
            int stoneCapacity = runicSlateMatrix != null ? runicSlateMatrix.MaxExtraTurns : 4;

            Text runeLabel = buttons.RuneFx != null ? buttons.RuneFx.GetComponentInChildren<Text>() : null;
            if (runeLabel != null) runeLabel.text = $"Runes: {runeProgress}/12";

            Text stoneLabel = buttons.RuneStone != null ? buttons.RuneStone.GetComponentInChildren<Text>() : null;
            if (stoneLabel != null) stoneLabel.text = $"Stones: {stoneCount}/{stoneCapacity}";
        }

        public static void SetKeyLightLabel(DebugButtons buttons, string presetName)
        {
            Text label = buttons?.KeyLightToggle != null ? buttons.KeyLightToggle.GetComponentInChildren<Text>() : null;
            if (label != null) label.text = $"Light: {presetName}";
        }

        /// <summary>픽셀 엣지 필터가 켜져 있는지 버튼 문구로 알린다. 기존 필터와 A/B로 비교할 때 쓴다.</summary>
        public static void SetPixelEdgeLabel(DebugButtons buttons, bool enabled)
        {
            Text label = buttons?.PixelEdgeToggle != null ? buttons.PixelEdgeToggle.GetComponentInChildren<Text>() : null;
            if (label != null) label.text = PixelEdgeLabel(enabled);
        }

        private static string PixelEdgeLabel(HudActions actions)
        {
            return PixelEdgeLabel(actions?.PixelEdgeEnabled != null && actions.PixelEdgeEnabled());
        }

        private static string PixelEdgeLabel(bool enabled)
        {
            return enabled ? "Edge: ON" : "Edge: OFF";
        }

        /// <summary>색 양자화 모드를 버튼 문구로 알린다. 세 모드를 눈으로 비교할 때 쓴다.</summary>
        public static void SetQuantizeLabel(DebugButtons buttons, string modeName)
        {
            Text label = buttons?.QuantizeToggle != null ? buttons.QuantizeToggle.GetComponentInChildren<Text>() : null;
            if (label != null) label.text = $"Quant: {modeName}";
        }

        private static string ResolutionLabel(HudActions actions)
        {
            return actions?.ResolutionPresetLabel != null ? actions.ResolutionPresetLabel() : "Resolution";
        }

        private static string QuantizeLabel(HudActions actions)
        {
            return $"Quant: {(actions?.QuantizeModeName != null ? actions.QuantizeModeName() : "Off")}";
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
