using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Tessera.Core;
using Tessera.Dice;
using Tessera.Games.Yacht;
using Tessera.Tabletop;

namespace Tessera.Games.AugmentedYacht
{
    public sealed class AugmentedYachtController : MonoBehaviour
    {
        [Header("Source assets")]
        [SerializeField] private GameObject diceModel;
        [SerializeField] private Mesh yachtTrayMesh;
        [SerializeField] private Texture2D playmatTexture;

        [Header("Rendering")]
        [SerializeField] private Shader upscaleShader;

        [Header("Game Settings")]
        [SerializeField, Min(1)] private int diceCount = 5;
        [SerializeField] private DieType selectedDieType = DieType.Normal;
        [SerializeField] private YachtGameMode launchMode = YachtGameMode.Normal;

        private readonly List<GameObject> activeDice = new();
        private readonly List<bool> keptDice = new();
        private readonly List<int> diceValues = new();
        private readonly List<int> keptSlotIndices = new();


        private DicePresetCatalog presetCatalog;
        private BakedDiceController bakedDiceController;
        private YachtAudioService audioService;

        private Camera worldCamera;
        private Camera presentationCamera;
        private RawImage gameImage;

        // 픽셀 필터를 거치지 않는 UI 경로(M9.5). 월드 카메라와 같은 투영으로 CrispUI 레이어만 그린다.
        private YachtInputRouter inputRouter;
        private YachtCameraRig cameraRig;
        private DiceVisualPool dicePool;
        private Text statusText;
        private RectTransform gameAreaRect;
        private RectTransform gameImageRect;
        private Transform layoutRoot;
        // 테이블 프롭은 Assets/Prefabs/Tabletop 의 프리팹 인스턴스이며 씬이 배치를 소유한다(M9).
        // 컨트롤러는 참조만 들고, 생성도 배치도 하지 않는다. 참조가 비면 이름으로 한 번 찾아 붙인다.
        [Header("Tabletop Props")]
        [SerializeField] private ParchmentScoreSheet parchmentScoreSheet;
        [SerializeField] private AugmentCardTray augmentCardTray;
        [SerializeField] private RollOrb rollOrb;
        [SerializeField] private RollCosmicCube rollCosmicCube;
        [SerializeField] private RerollCounterBar rerollCounterBar;
        [SerializeField] private HourglassTimer hourglassTimer;
        [SerializeField] private CozyCandleStand candleStand;
        [SerializeField] private RunicSlateMatrix runicSlateMatrix;
        [SerializeField] private TabletopTrinketCluster trinketCluster;
        [SerializeField] private TurnBalanceIndicator turnBalanceIndicator;
        private YachtGameSession gameSession;
        private GameObject startGameOverlay;
        private GameObject gameResultOverlay;
        private GameObject augmentDraftOverlay;
        private Text timerText;
        private Text resultText;
        private Text augmentDraftTitle;
        private Text augmentEffectText;
        private Text augmentHoverDetailText;
        private readonly Button[] augmentDraftButtons = new Button[YachtAugmentRuntime.DraftOptionCount];
        private readonly AugmentCardView[] augmentDraftCards = new AugmentCardView[YachtAugmentRuntime.DraftOptionCount];
        private readonly AugmentTrayCardView[] augmentOwnedCards = new AugmentTrayCardView[3];
        private int displayedAugmentPlayer = -1;
        private int hoveredAugmentSlot = -1;
        private int selectedAugmentSlot = -1;
        private static readonly string[] ManualAugmentIds =
        {
            YachtAugmentRuntime.TableFlipId,
            YachtAugmentRuntime.EquivalentExchangeId,
            YachtAugmentRuntime.GambitId,
            YachtAugmentRuntime.DoubleDownId,
            YachtAugmentRuntime.DiceAlchemyId
        };
        private static readonly string[] ManualAugmentLabels =
        {
            "판 뒤집기", "등가교환", "갬빗", "더블 다운", "주사위 연금술"
        };
        private readonly Button[] augmentActionButtons = new Button[ManualAugmentIds.Length];
        private Button tableFlipButton;
        private readonly YachtAugmentRuntime augmentViewCatalog = new();
        private bool turnTransitionInProgress;
        private string pendingTurnTransitionMessage;
        private YachtGameCommandResult pendingRollResult;

        public ParchmentScoreSheet ScoreSheet => parchmentScoreSheet;
        public AugmentCardTray CardTray => augmentCardTray;
        public RollOrb RollOrb => rollOrb;
        public RollCosmicCube RollCosmicCube => rollCosmicCube;
        public RerollCounterBar RerollCounter => rerollCounterBar;
        public HourglassTimer Hourglass => hourglassTimer;
        public CozyCandleStand CandleStand => candleStand;
        public RunicSlateMatrix RunicMatrix => runicSlateMatrix;
        public TabletopTrinketCluster TrinketCluster => trinketCluster;
        public TurnBalanceIndicator TurnBalance => turnBalanceIndicator;
        public YachtGameSession GameSession => gameSession;
        public YachtGameMode GameMode => gameSession?.Mode ?? launchMode;

        private Coroutine rollRoutine;
        private Coroutine keepRoutine;
        private Button keyLightToggleButton;
        private Button runeFxButton;
        private Button runeStoneButton;
        private int rollIndex;
        private bool hasCompletedRoll;
        private bool isArranging;
        private int hoveredDieIndex = -1;

        private YachtLightingRig lightingRig;

        private static readonly Vector2Int ResolutionA = new(960, 540);
        private static readonly Vector2Int ResolutionB = new(640, 360);

        private const float TableWidth = 15.6f;
        private const float LeftSectionWidth = TableWidth * 0.25f;
        private const float CenterSectionWidth = TableWidth * 0.45f;
        private const float RightSectionWidth = TableWidth * 0.3f;
        private const float CenterSectionX = -TableWidth * 0.5f + LeftSectionWidth + CenterSectionWidth * 0.5f;
        private const float TrayScale = 0.05f;
        private const float RollSurfaceY = 0.2f;
        private const float TrayVisualY = RollSurfaceY + 10.283531f * TrayScale;
        private const int DecorationLayer = 11;
        private const float TurnDurationSeconds = YachtGameOptions.DefaultTurnDurationSeconds;

        public bool IsSettled => hasCompletedRoll && !isArranging && rollRoutine == null;
        public int KeptDieCount => keptDice.FindAll(kept => kept).Count;

        public int GetDieValue(int index)
        {
            return index >= 0 && index < diceValues.Count ? diceValues[index] : 0;
        }

        private void Awake()
        {
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;

            EnsureDicePool();

            if (diceModel == null)
            {
#if UNITY_EDITOR
                diceModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Reference/normal_dice.fbx");
#endif
            }

            // 배치는 씬이 소유한다(M9). 여기서는 바인딩만 하고, 씬이 비어 있을 때만 최소 월드를 세운다.
            if (!ResolveEditableLayout())
            {
                Debug.LogWarning(
                    "[AugmentedYachtController] 씬에서 레이아웃을 찾지 못해 카메라와 프레젠테이션만 생성합니다. " +
                    "테이블 프롭은 Assets/Prefabs/Tabletop 의 프리팹을 씬에 배치해야 합니다.");
                BuildWorld();
                BuildPresentation();
            }
            else
            {
                EnsureEventSystem();
                BindPresentationActions();
                EnsureCameraRig();
            cameraRig.CreateRenderTarget();
            }

            cameraRig?.ApplyRenderSettings();
            ConfigureLighting();
            EnsureSingleAudioListener();
            EnsureAudioService();
            InitializePresetCatalog();
            InitializeBakedController();
            EnsureDicePool();
            EnsureDiceState();
            EnsureInputRouter();

            BindTabletopProps();

            WarmUpRollAssets();
            ResolveRunicMatrix();
            InitializeYachtGame();
        }



        private void OnValidate()
        {
            if (Application.isPlaying && activeDice.Count > 0)
            {
                SetDieType(selectedDieType);
            }
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                cameraRig?.ApplyRenderSettings();
            }
#endif
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                cameraRig?.ApplyRenderSettings();
            }
        }
#endif

        // 자동 호출하지 않는다. 트레이 메시의 UV와 펠트 텍스처는 M9에서 에셋으로 구웠고,
        // 이 메서드는 런타임 생성 텍스처를 머티리얼에 덮어써 구워둔 참조를 지운다.
        // 형상을 다시 만들어야 할 때만 수동으로 실행한 뒤 프리팹을 다시 굽는다.
        [ContextMenu("Regenerate Tray Visual Material (bake 전용)")]
        public void SyncTrayVisualMat()
        {
            GameObject trayObj = GameObject.Find("Yacht Tray Visual");
            if (trayObj == null) return;
            MeshFilter mf = trayObj.GetComponent<MeshFilter>();
            MeshRenderer mr = trayObj.GetComponent<MeshRenderer>();
            if (mf == null || mr == null) return;

            Mesh mesh = mf.sharedMesh;
            if (mesh != null && (mesh.uv == null || mesh.uv.Length == 0))
            {
                Mesh copy = Instantiate(mesh);
                Vector3[] verts = copy.vertices;
                Vector3[] norms = copy.normals;
                Vector2[] uvs = new Vector2[verts.Length];
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 v = verts[i];
                    Vector3 n = (norms != null && i < norms.Length) ? norms[i] : Vector3.up;
                    if (Mathf.Abs(n.y) >= 0.7f)
                        uvs[i] = new Vector2(v.x * (1f / 50f), v.z * (1f / 50f));
                    else
                        uvs[i] = new Vector2((Mathf.Abs(n.x) > Mathf.Abs(n.z) ? v.z : v.x) * (1f / 50f), v.y * (1f / 50f));
                }
                copy.uv = uvs;
                mf.sharedMesh = copy;
            }

            Texture2D corduroyTex = CreateBurgundyCorduroyTexture();
            Material[] mats = mr.sharedMaterials;
            if (mats != null && mats.Length >= 2)
            {
                Material felt = mats[1];
                if (felt != null)
                {
                    felt.mainTexture = corduroyTex;
                    if (felt.HasProperty("_BaseMap")) felt.SetTexture("_BaseMap", corduroyTex);
                    if (felt.HasProperty("_MainTex")) felt.SetTexture("_MainTex", corduroyTex);
                    if (felt.HasProperty("_BaseColor")) felt.SetColor("_BaseColor", Color.white);
                    if (felt.HasProperty("_Color")) felt.SetColor("_Color", Color.white);
                    felt.mainTextureScale = new Vector2(1f, 1f);
                    felt.color = Color.white;
                    felt.SetFloat("_Smoothness", 0.12f);
                }
            }
        }

        private void Start()
        {
            StartCoroutine(audioService.LoadClipsAsync());
        }

        private void InitializeYachtGame()
        {
            if (!Application.isPlaying || parchmentScoreSheet == null) return;

            parchmentScoreSheet.EnsureStructure();
            parchmentScoreSheet.ScoreSelected -= OnScoreSelected;
            parchmentScoreSheet.ScoreSelected += OnScoreSelected;

            gameSession = CreateGameSession(launchMode);
            parchmentScoreSheet.SetActivePlayer(-1, false);
            turnBalanceIndicator?.SetActiveSide(TurnSide.None, false);

            if (hourglassTimer != null)
            {
                hourglassTimer.OnTimerStarted -= OnTurnTimerStarted;
                hourglassTimer.OnTimerTick -= OnTurnTimerTick;
                hourglassTimer.OnTimerExpired -= OnTurnTimerExpired;
                hourglassTimer.OnTimerStarted += OnTurnTimerStarted;
                hourglassTimer.OnTimerTick += OnTurnTimerTick;
                hourglassTimer.OnTimerExpired += OnTurnTimerExpired;
                hourglassTimer.SetIdleState(TurnDurationSeconds);
            }

            runicSlateMatrix?.SetRoundProgress(0);
            rerollCounterBar?.SetRollsRemaining(YachtGameSession.MaxRolls, YachtGameSession.MaxRolls);
            EnsureGameFlowUI();
            EnsureOwnedCardViews();
            ResetDiceForTurn();
            SetTimerTextIdle();
            SetRollInteraction(false);
            UpdateStatusText("게임 시작 버튼을 눌러 주세요.");
        }

        private void EnsureGameFlowUI()
        {
            GameObject canvasObject = GameObject.Find("Pixel Presentation");
            if (canvasObject == null) return;

            Transform existingStart = canvasObject.transform.Find("Yacht Game Start Overlay");
            if (existingStart != null)
            {
                if (Application.isPlaying) Destroy(existingStart.gameObject);
                else DestroyImmediate(existingStart.gameObject);
            }
            Transform existingResult = canvasObject.transform.Find("Yacht Game Result Overlay");
            if (existingResult != null)
            {
                if (Application.isPlaying) Destroy(existingResult.gameObject);
                else DestroyImmediate(existingResult.gameObject);
            }
            Transform existingDraft = canvasObject.transform.Find("Yacht Augment Draft Overlay");
            if (existingDraft != null)
            {
                if (Application.isPlaying) Destroy(existingDraft.gameObject);
                else DestroyImmediate(existingDraft.gameObject);
            }
            Transform existingTimer = canvasObject.transform.Find("Yacht Turn Timer Text");
            if (existingTimer != null)
            {
                if (Application.isPlaying) Destroy(existingTimer.gameObject);
                else DestroyImmediate(existingTimer.gameObject);
            }
            string[] augmentPresentationNames =
            {
                "Yacht Augment Owned Text",
                "Yacht Augment Effect Text",
                "Yacht Augment Hover Detail Text",
                "Use Table Flip",
            };
            foreach (string presentationName in augmentPresentationNames)
            {
                Transform existingPresentation = canvasObject.transform.Find(presentationName);
                if (existingPresentation == null) continue;
                if (Application.isPlaying) Destroy(existingPresentation.gameObject);
                else DestroyImmediate(existingPresentation.gameObject);
            }

            timerText = CreateText(canvasObject.transform, "Yacht Turn Timer Text", "--", Vector2.zero,
                new Vector2(120f, 46f), new Vector2(0.5f, 0.5f), 30, TextAnchor.MiddleCenter);
            timerText.color = new Color32(255, 226, 151, 255);

            startGameOverlay = CreateFullScreenOverlay(canvasObject.transform, "Yacht Game Start Overlay");
            Text title = CreateText(startGameOverlay.transform, "Title", "요트 다이스", new Vector2(0f, 90f),
                new Vector2(620f, 90f), new Vector2(0.5f, 0.5f), 42, TextAnchor.MiddleCenter);
            title.color = new Color32(255, 222, 151, 255);
            CreateButton(startGameOverlay.transform, "Start Normal Yacht Game", "일반 요트", new Vector2(0f, -5f),
                new Vector2(260f, 64f), new Vector2(0.5f, 0.5f), () => StartNewGame(YachtGameMode.Normal));
            CreateButton(startGameOverlay.transform, "Start Augmented Yacht Game", "증강 요트", new Vector2(0f, -85f),
                new Vector2(260f, 64f), new Vector2(0.5f, 0.5f), () => StartNewGame(YachtGameMode.Augmented));

            gameResultOverlay = CreateFullScreenOverlay(canvasObject.transform, "Yacht Game Result Overlay");
            resultText = CreateText(gameResultOverlay.transform, "Result", "", new Vector2(0f, 35f),
                new Vector2(720f, 150f), new Vector2(0.5f, 0.5f), 36, TextAnchor.MiddleCenter);
            resultText.color = new Color32(255, 222, 151, 255);
            CreateButton(gameResultOverlay.transform, "Restart Yacht Game", "다시 시작", new Vector2(0f, -105f),
                new Vector2(240f, 64f), new Vector2(0.5f, 0.5f), StartNewGame);
            gameResultOverlay.SetActive(false);

            augmentDraftOverlay = CreateFullScreenOverlay(canvasObject.transform, "Yacht Augment Draft Overlay");
            EnsureCameraRig();
            AugmentParchmentVisuals.PixelFilterResolution = cameraRig.InternalResolution;
            float draftCardWidth = 460f;
            float draftCardAspect = augmentCardTray != null
                ? augmentCardTray.CardSlotAspectRatio
                : AugmentCardView.TrayCardAspectRatio;
            float draftCardHeight = draftCardWidth / Mathf.Max(1f, draftCardAspect);
            float draftCardSpacing = draftCardWidth + 24f;
            augmentDraftTitle = CreateText(augmentDraftOverlay.transform, "Draft Title", "증강 선택", new Vector2(0f, draftCardHeight * 0.5f + 72f),
                new Vector2(760f, 60f), new Vector2(0.5f, 0.5f), 34, TextAnchor.MiddleCenter);
            augmentDraftTitle.color = new Color32(255, 222, 151, 255);
            for (int i = 0; i < augmentDraftButtons.Length; i++)
            {
                int optionIndex = i;
                augmentDraftCards[i] = AugmentCardView.Create(
                    augmentDraftOverlay.transform,
                    $"Draft Option {i + 1}",
                    new Vector2((i - 1) * draftCardSpacing, -8f),
                    new Vector2(draftCardWidth, draftCardHeight),
                    new Vector2(0.5f, 0.5f),
                    () => SelectDraftOption(optionIndex));
                augmentDraftCards[i].SetParchmentPreset((AugmentParchmentPreset)i);
                augmentDraftButtons[i] = augmentDraftCards[i].Button;
            }
            augmentDraftOverlay.SetActive(false);

            augmentEffectText = CreateText(canvasObject.transform, "Yacht Augment Effect Text", "", new Vector2(0f, 58f),
                new Vector2(760f, 44f), new Vector2(0.5f, 0f), 18, TextAnchor.MiddleCenter);
            augmentEffectText.color = new Color32(255, 205, 95, 255);
            augmentHoverDetailText = CreateText(canvasObject.transform, "Yacht Augment Hover Detail Text", "", new Vector2(0f, 126f),
                new Vector2(820f, 64f), new Vector2(0.5f, 0f), 16, TextAnchor.MiddleCenter);
            augmentHoverDetailText.color = new Color32(255, 226, 151, 255);
            augmentHoverDetailText.gameObject.SetActive(false);
            for (int i = 0; i < augmentActionButtons.Length; i++)
            {
                string augmentId = ManualAugmentIds[i];
                augmentActionButtons[i] = CreateButton(
                    canvasObject.transform,
                    $"Use {augmentId}",
                    ManualAugmentLabels[i],
                    new Vector2((i - 2) * 152f, 18f),
                    new Vector2(142f, 48f),
                    new Vector2(0.5f, 0f),
                    () => UseAugmentAction(augmentId));
                augmentActionButtons[i].gameObject.SetActive(false);
            }
            tableFlipButton = augmentActionButtons[0];
        }

        private static GameObject CreateFullScreenOverlay(Transform parent, string name)
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

        private void StartNewGame()
        {
            StartNewGame(launchMode);
        }

        private void StartNewGame(YachtGameMode mode)
        {
            if (parchmentScoreSheet == null) return;

            if (rollRoutine != null)
            {
                StopCoroutine(rollRoutine);
                rollRoutine = null;
            }
            if (keepRoutine != null)
            {
                StopCoroutine(keepRoutine);
                keepRoutine = null;
            }

            launchMode = mode;
            gameSession = CreateGameSession(mode);
            hourglassTimer?.SetIdleState(TurnDurationSeconds);
            gameSession.StartNewGame();
            ApplyModePresentation();
            turnBalanceIndicator?.SetActiveSide(TurnSide.Left, false);
            parchmentScoreSheet.RefreshAllScores();
            parchmentScoreSheet.SetActivePlayer(gameSession.CurrentPlayerIndex, false);
            startGameOverlay?.SetActive(false);
            gameResultOverlay?.SetActive(false);
            runicSlateMatrix?.SetRoundProgress(gameSession.CurrentRound);
            ResetDiceForTurn();
            string augmentMessage = GetAugmentEventMessage(gameSession.LastCommandResult);
            RefreshAugmentPresentation(augmentMessage);
            if (gameSession.IsDrafting)
            {
                turnTransitionInProgress = false;
                SetTimerTextIdle();
                SetRollInteraction(false);
                UpdateStatusText(augmentMessage);
            }
            else
            {
                BeginTurnTimer();
                UpdateStatusText(augmentMessage);
            }
        }

        private YachtGameSession CreateGameSession(YachtGameMode mode)
        {
            int presetClipCount = presetCatalog != null ? presetCatalog.NormalFiveDiceClipCount : 20;
            var options = new YachtGameOptions
            {
                Mode = mode,
                DiceCount = diceCount,
                PresetClipCount = Mathf.Max(1, presetClipCount),
                TurnDurationSeconds = TurnDurationSeconds
            };
            return new YachtGameSession(parchmentScoreSheet.Player1, parchmentScoreSheet.Player2, options);
        }

        private void ApplyModePresentation()
        {
            // 일반 모드에서도 왼쪽 증강 트레이 그래픽은 사용자의 현재 아트 기준에 따라 임시 유지한다.
            // 드래프트, 특수 주사위, 증강 명령은 Normal 규칙 세트에서 생성되지 않는다.
            if (augmentCardTray != null) augmentCardTray.gameObject.SetActive(true);
            if (launchMode == YachtGameMode.Normal) SetDieType(DieType.Normal);
        }

        private void SelectDraftOption(int optionIndex)
        {
            if (gameSession == null || !gameSession.IsDrafting) return;
            string[] options = gameSession.State.Draft.Options;
            if (optionIndex < 0 || optionIndex >= options.Length) return;

            if (!gameSession.TrySelectAugment(options[optionIndex], out YachtGameCommandResult result))
            {
                RefreshAugmentPresentation(result.ErrorMessage);
                UpdateStatusText(result.ErrorMessage);
                return;
            }

            string message = GetAugmentEventMessage(result);
            RefreshAugmentPresentation(message);
            if (gameSession.IsDrafting)
            {
                parchmentScoreSheet?.SetActivePlayer(gameSession.State.Draft.PlayerIndex, false);
                UpdateStatusText(message);
                return;
            }

            ResetDiceForTurn();
            parchmentScoreSheet?.SetActivePlayer(gameSession.CurrentPlayerIndex, false);
            BeginTurnTimer();
            UpdateStatusText(message);
        }

        private void RefreshAugmentPresentation(string message = null)
        {
            bool augmented = gameSession != null && gameSession.Mode == YachtGameMode.Augmented;
            bool gameInProgress = augmented
                && gameSession.Phase != YachtGamePhase.WaitingToStart
                && gameSession.Phase != YachtGamePhase.GameOver;
            if (augmentDraftOverlay != null)
            {
                bool showDraft = gameInProgress && gameSession.IsDrafting;
                augmentDraftOverlay.SetActive(showDraft);
                if (showDraft)
                    augmentDraftOverlay.transform.SetAsLastSibling();
            }
            if (augmentEffectText != null)
            {
                augmentEffectText.gameObject.SetActive(augmented && !string.IsNullOrEmpty(message));
                if (!string.IsNullOrEmpty(message)) augmentEffectText.text = message;
            }
            if (tableFlipButton != null)
            {
                tableFlipButton.interactable = gameInProgress && gameSession.CanUseTableFlip
                    && !turnTransitionInProgress && rollRoutine == null && !isArranging;
            }
            for (int i = 0; i < augmentActionButtons.Length; i++)
            {
                Button button = augmentActionButtons[i];
                if (button == null) continue;
                bool owned = gameInProgress && !gameSession.IsDrafting
                    && IsOwnedAugment(gameSession.CurrentPlayerIndex, ManualAugmentIds[i]);
                button.gameObject.SetActive(owned);
                if (i > 0) button.interactable = owned && !turnTransitionInProgress && rollRoutine == null && !isArranging;
            }
            RefreshOwnedCardTray(augmented, gameInProgress);
            if (!augmented) return;

            if (!gameSession.IsDrafting) return;

            int playerIndex = gameSession.State.Draft.PlayerIndex;
            if (augmentDraftTitle != null)
                augmentDraftTitle.text = $"P{playerIndex + 1} 증강 선택 · {gameSession.CurrentRound}라운드";
            string[] options = gameSession.State.Draft.Options;
            for (int i = 0; i < augmentDraftButtons.Length; i++)
            {
                Button button = augmentDraftButtons[i];
                if (button == null) continue;
                bool active = i < options.Length;
                button.gameObject.SetActive(active);
                if (!active) continue;
                YachtAugmentDefinition definition = augmentViewCatalog.FindDefinition(options[i]);
                int presetId = i < (gameSession.State.Draft.OptionCardPresetIds?.Length ?? 0)
                    ? gameSession.State.Draft.OptionCardPresetIds[i]
                    : 0;
                augmentDraftCards[i]?.SetParchmentPreset(AugmentParchmentVisuals.Normalize(presetId));
                augmentDraftCards[i]?.Bind(definition, AugmentCardDisplayState.Available);
            }
        }

        /// <summary>픽셀 격자가 바뀌면 선택 창 카드 본체를 새 격자로 다시 굽는다.</summary>
        private void RefreshDraftCardParchment()
        {
            for (int i = 0; i < augmentDraftCards.Length; i++)
                augmentDraftCards[i]?.SetParchmentPreset(augmentDraftCards[i].ParchmentPreset);
        }

        private void EnsureOwnedCardViews()
        {
            if (augmentCardTray == null || worldCamera == null) return;
            Vector2 slotSize = augmentCardTray.CardSlotLocalSize;
            int count = Mathf.Min(augmentOwnedCards.Length, augmentCardTray.SlotCount);
            for (int i = 0; i < count; i++)
            {
                if (augmentOwnedCards[i] != null) continue;
                Transform anchor = augmentCardTray.GetSlotAnchor(i);
                if (anchor == null) continue;
                Transform existing = anchor.Find($"Owned Augment Card {i + 1}");
                augmentOwnedCards[i] = existing != null
                    ? existing.GetComponent<AugmentTrayCardView>()
                    : AugmentTrayCardView.Create(anchor, slotSize, i);
            }
        }

        private void RefreshOwnedCardTray(bool augmented, bool gameInProgress)
        {
            if (!augmented || !gameInProgress)
            {
                for (int i = 0; i < augmentOwnedCards.Length; i++) augmentOwnedCards[i]?.SetVisible(false);
                return;
            }
            EnsureOwnedCardViews();
            int playerIndex = gameSession != null && gameSession.IsDrafting
                ? gameSession.State.Draft.PlayerIndex
                : gameSession?.CurrentPlayerIndex ?? -1;
            if (displayedAugmentPlayer != playerIndex)
            {
                displayedAugmentPlayer = playerIndex;
                selectedAugmentSlot = -1;
                SetHoveredAugmentSlot(-1);
            }

            string[] owned = augmented && gameInProgress && playerIndex >= 0
                ? gameSession.State.AugmentPlayers[playerIndex].OwnedIds
                : Array.Empty<string>();
            int[] presets = playerIndex >= 0
                ? gameSession.State.AugmentPlayers[playerIndex].OwnedCardPresetIds
                : Array.Empty<int>();
            if (selectedAugmentSlot >= owned.Length) selectedAugmentSlot = -1;

            for (int i = 0; i < augmentOwnedCards.Length; i++)
            {
                AugmentTrayCardView view = augmentOwnedCards[i];
                if (view == null) continue;
                bool visible = i < owned.Length;
                view.SetVisible(visible);
                if (!visible) continue;
                int presetId = i < (presets?.Length ?? 0) ? presets[i] : 0;
                view.Bind(augmentViewCatalog.FindDefinition(owned[i]), presetId);
                view.SetSelected(i == selectedAugmentSlot);
            }
        }

        private void SetHoveredAugmentSlot(int slotIndex)
        {
            if (hoveredAugmentSlot == slotIndex) return;
            if (hoveredAugmentSlot >= 0 && hoveredAugmentSlot < augmentOwnedCards.Length)
                augmentOwnedCards[hoveredAugmentSlot]?.SetHovered(false);

            hoveredAugmentSlot = slotIndex;
            YachtAugmentDefinition definition = null;
            if (slotIndex >= 0 && slotIndex < augmentOwnedCards.Length)
            {
                AugmentTrayCardView view = augmentOwnedCards[slotIndex];
                view?.SetHovered(true);
                definition = view?.Definition;
            }

            if (augmentHoverDetailText == null) return;
            bool show = definition != null;
            augmentHoverDetailText.gameObject.SetActive(show);
            if (show)
                augmentHoverDetailText.text = $"{definition.DisplayName}\n{definition.Description}";
        }

        private bool IsOwnedAugment(int playerIndex, string augmentId)
        {
            if (gameSession?.State?.AugmentPlayers == null
                || playerIndex < 0 || playerIndex >= gameSession.State.AugmentPlayers.Length) return false;
            string[] owned = gameSession.State.AugmentPlayers[playerIndex].OwnedIds;
            return Array.IndexOf(owned, augmentId) >= 0;
        }

        private static string GetAugmentEventMessage(YachtGameCommandResult result)
        {
            if (result?.Events == null) return null;
            for (int i = result.Events.Length - 1; i >= 0; i--)
                if (!string.IsNullOrEmpty(result.Events[i].Message)) return result.Events[i].Message;
            return null;
        }

        private void BeginTurnTimer()
        {
            if (gameSession == null || gameSession.IsDrafting)
            {
                turnTransitionInProgress = false;
                hourglassTimer?.SetIdleState(TurnDurationSeconds);
                SetTimerTextIdle();
                SetRollInteraction(false);
                RefreshAugmentPresentation();
                return;
            }
            turnTransitionInProgress = true;
            parchmentScoreSheet?.SetActivePlayer(gameSession.CurrentPlayerIndex, false);
            SetRollInteraction(false);
            float turnDuration = gameSession.CurrentTurnDurationSeconds;
            SetTimerText(turnDuration);

            if (hourglassTimer != null)
            {
                hourglassTimer.StartTimer(turnDuration, true);
            }
            else
            {
                OnTurnTimerStarted();
            }
        }

        private void OnTurnTimerStarted()
        {
            string transitionMessage = null;
            bool advancedTurn = false;
            if (gameSession != null && gameSession.Phase == YachtGamePhase.TurnTransition)
            {
                if (!gameSession.AdvanceTurnAfterAnimation()) return;
                advancedTurn = true;

                if (gameSession.Phase == YachtGamePhase.GameOver)
                {
                    FinishGame();
                    return;
                }

                transitionMessage = pendingTurnTransitionMessage;
                pendingTurnTransitionMessage = null;
                string augmentMessage = GetAugmentEventMessage(gameSession.LastCommandResult);
                if (!string.IsNullOrEmpty(augmentMessage)) transitionMessage = augmentMessage;
                if (gameSession.IsDrafting)
                {
                    turnTransitionInProgress = false;
                    hourglassTimer?.StopTimer(false);
                    SetTimerTextIdle();
                    SetRollInteraction(false);
                    parchmentScoreSheet?.SetActivePlayer(gameSession.State.Draft.PlayerIndex, false);
                    RefreshAugmentPresentation(transitionMessage);
                    UpdateStatusText(transitionMessage);
                    return;
                }
                ResetDiceForTurn();
                rerollCounterBar?.SetRollsRemaining(YachtGameSession.MaxRolls, YachtGameSession.MaxRolls);
                runicSlateMatrix?.SetRoundProgress(gameSession.CurrentRound);
                parchmentScoreSheet?.SetActivePlayer(gameSession.CurrentPlayerIndex, false);
                turnBalanceIndicator?.SetActiveSide(MapPlayerToTurnSide(gameSession.CurrentPlayerIndex), true);
            }

            if (advancedTurn && hourglassTimer != null)
            {
                float turnDuration = gameSession.CurrentTurnDurationSeconds;
                hourglassTimer.ResetTimer(turnDuration);
                hourglassTimer.ResumeTimer();
                SetTimerText(turnDuration);
            }

            turnTransitionInProgress = false;
            RefreshGameInteraction();
            RefreshAugmentPresentation(transitionMessage);
            UpdateStatusText(transitionMessage);
        }

        private void OnTurnTimerTick(float remaining, float total)
        {
            SetTimerText(remaining);
        }

        private void OnTurnTimerExpired()
        {
            if (gameSession == null || !gameSession.ResolveTimeout(out YachtTurnResult result)) return;
            parchmentScoreSheet.RefreshAllScores();
            string augmentMessage = GetAugmentEventMessage(gameSession.LastCommandResult);
            HandleTurnCompleted(result, augmentMessage ?? "시간 초과로 점수가 자동 확정되었습니다.");
        }

        private void OnScoreSelected(int playerIndex, ScoreCategory category)
        {
            if (gameSession == null || playerIndex != gameSession.CurrentPlayerIndex) return;
            if (!gameSession.TryCommitScore(category, out YachtTurnResult result)) return;

            parchmentScoreSheet.RefreshAllScores();
            string augmentMessage = GetAugmentEventMessage(gameSession.LastCommandResult);
            string scoreMessage = $"P{result.ScoredPlayerIndex + 1} 점수 {result.Score}점 확정";
            HandleTurnCompleted(result, string.IsNullOrEmpty(augmentMessage) ? scoreMessage : $"{scoreMessage} · {augmentMessage}");
        }

        private void HandleTurnCompleted(YachtTurnResult result, string message)
        {
            hourglassTimer?.StopTimer(false);
            parchmentScoreSheet.ClearCandidateScores();
            SetRollInteraction(false);

            if (result.GameEnded)
            {
                FinishGame();
                return;
            }

            pendingTurnTransitionMessage = message;
            RefreshAugmentPresentation(message);
            UpdateStatusText(message);
            BeginTurnHandoffAnimation();
        }

        private void BeginTurnHandoffAnimation()
        {
            turnTransitionInProgress = true;
            SetRollInteraction(false);
            SetTimerText(TurnDurationSeconds);

            if (hourglassTimer != null)
            {
                hourglassTimer.StartTimer(TurnDurationSeconds, true);
            }
            else
            {
                OnTurnTimerStarted();
            }
        }

        private void FinishGame()
        {
            turnTransitionInProgress = false;
            hourglassTimer?.StopTimer();
            rerollCounterBar?.SetRollsRemaining(0, YachtGameSession.MaxRolls);
            parchmentScoreSheet?.SetActivePlayer(-1, false);
            turnBalanceIndicator?.SetActiveSide(TurnSide.None, true);
            SetRollInteraction(false);
            SetTimerTextIdle();

            int p1 = gameSession.GetPlayer(0).totalScore;
            int p2 = gameSession.GetPlayer(1).totalScore;
            string winner = p1 == p2 ? "무승부" : (p1 > p2 ? "P1 승리" : "P2 승리");
            if (resultText != null) resultText.text = $"{winner}\nP1  {p1}점   ·   P2  {p2}점";
            gameResultOverlay?.SetActive(true);
            RefreshAugmentPresentation();
            UpdateStatusText("게임이 종료되었습니다.");
        }

        private void ResetDiceForTurn()
        {
            SyncDiceStateFromAuthority();
            for (int i = 0; i < keptSlotIndices.Count; i++) keptSlotIndices[i] = -1;
            hasCompletedRoll = false;
            isArranging = false;
            hoveredDieIndex = -1;
            dicePool.ArrangeInitialPositions(activeDice, diceValues);
        }

        private void SyncDiceStateFromAuthority()
        {
            if (gameSession?.State?.Dice == null) return;
            YachtDieState[] stateDice = gameSession.State.Dice;
            int count = Mathf.Min(stateDice.Length, Mathf.Min(keptDice.Count, diceValues.Count));
            for (int i = 0; i < count; i++)
            {
                keptDice[i] = stateDice[i].IsKept;
                diceValues[i] = stateDice[i].Value;
            }
        }

        private void RefreshGameInteraction()
        {
            if (gameSession == null)
            {
                SetRollInteraction(false);
                return;
            }

            bool allKept = keptDice.Count > 0 && keptDice.TrueForAll(kept => kept);
            bool canRoll = gameSession.CanRoll && !turnTransitionInProgress
                && rollRoutine == null && !isArranging && !allKept;
            SetRollInteraction(canRoll);

            if (gameSession.Phase == YachtGamePhase.ScoreSelection && !turnTransitionInProgress)
            {
                parchmentScoreSheet?.ShowCandidateScores(gameSession.CurrentPlayerIndex, gameSession.CurrentCandidates);
            }
            else if (gameSession.Phase == YachtGamePhase.Draft)
            {
                parchmentScoreSheet?.SetActivePlayer(gameSession.State.Draft.PlayerIndex, false);
            }
            else if (gameSession.Phase != YachtGamePhase.GameOver)
            {
                parchmentScoreSheet?.SetActivePlayer(gameSession.CurrentPlayerIndex, false);
            }
            RefreshAugmentPresentation();
        }

        private bool CanInitiateRoll()
        {
            if (gameSession == null || !gameSession.CanRoll || turnTransitionInProgress) return false;
            if (rollRoutine != null || isArranging) return false;
            return keptDice.Count == 0 || !keptDice.TrueForAll(kept => kept);
        }

        private void SetTimerText(float remaining)
        {
            if (timerText == null) return;
            float duration = gameSession?.CurrentTurnDurationSeconds ?? TurnDurationSeconds;
            int seconds = Mathf.Clamp(Mathf.CeilToInt(remaining), 0, Mathf.CeilToInt(duration));
            timerText.text = $"{seconds}s";
            timerText.color = seconds <= 10
                ? new Color32(255, 100, 65, 255)
                : new Color32(255, 226, 151, 255);
        }

        private void SetTimerTextIdle()
        {
            if (timerText == null) return;
            timerText.text = "--";
            timerText.color = new Color32(160, 140, 120, 230);
        }

        private void UpdateTimerTextPosition()
        {
            if (timerText == null || hourglassTimer == null || worldCamera == null) return;
            Vector3 worldPosition = hourglassTimer.transform.position + Vector3.up * 2.8f;
            Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z > 0f) timerText.rectTransform.position = screenPosition;
        }

        private void InitializePresetCatalog()
        {
            // 일반·혼합·판 뒤집기 프리셋의 인덱스를 먼저 읽고 실제 파일은 최초 사용 시 적재한다.
            presetCatalog = DicePresetCatalog.LoadAll();
            if (!presetCatalog.IsLoaded) presetCatalog = DicePresetCatalog.LoadNormalFiveDice();
            Debug.Log($"Preset Catalog loaded: {presetCatalog.NormalFiveDiceClipCount} clips available.");
        }

        private static void WarmUpRollAssets()
        {
            // 저장된 씬의 기존 롤 오브젝트는 지오메트리를 재생성하지 않아 별자리 캐시가 비어 있을 수 있다.
            // 입력 처리 전에 베이킹을 끝내, 첫 RollDice 호출이 프레임을 점유하지 않도록 한다.
            ZodiacConstellationData.GetAllZodiacTextures();
        }

        private void InitializeBakedController()
        {
            bakedDiceController = GetComponent<BakedDiceController>();
            if (bakedDiceController == null)
            {
                bakedDiceController = gameObject.AddComponent<BakedDiceController>();
            }
        }


        private void EnsureDiceState()
        {
            if (activeDice.Count == diceCount && keptDice.Count == diceCount && diceValues.Count == diceCount)
            {
                return;
            }

            foreach (GameObject die in activeDice)
            {
                if (die != null) Destroy(die);
            }
            activeDice.Clear();
            keptDice.Clear();
            diceValues.Clear();
            keptSlotIndices.Clear();

            EnsureDicePool();
            for (int index = 0; index < diceCount; index++)
            {
                GameObject die = dicePool.CreateVisualDie(index + 1);
                activeDice.Add(die);
                keptDice.Add(false);
                diceValues.Add(index + 1); // 기본 1~5 눈 설정
                keptSlotIndices.Add(-1);
            }

            hasCompletedRoll = false;
            dicePool.ArrangeInitialPositions(activeDice, diceValues);
        }








        public void RollDice()
        {
            if (!CanInitiateRoll())
            {
                if (gameSession != null && gameSession.RollsRemaining <= 0)
                    UpdateStatusText("이번 턴의 굴림 횟수를 모두 사용했습니다.");
                else if (keptDice.Count > 0 && keptDice.TrueForAll(kept => kept))
                    UpdateStatusText("모든 주사위가 킵되어 있습니다.");
                return;
            }

            if (!gameSession.TryRoll(out pendingRollResult)) return;
            SyncDiceStateFromAuthority();
            hourglassTimer?.PauseTimer();
            parchmentScoreSheet?.ClearCandidateScores();
            rerollCounterBar?.SetRollsRemaining(gameSession.RollsRemaining, YachtGameSession.MaxRolls);
            SetRollInteraction(false);
            rollRoutine = StartCoroutine(PerformBakedRollSequence());
        }

        public void UseTableFlip()
        {
            UseAugmentAction(YachtAugmentRuntime.TableFlipId);
        }

        public void UseAugmentAction(string augmentId)
        {
            if (gameSession == null || turnTransitionInProgress || rollRoutine != null || isArranging) return;
            if (!gameSession.TryUseAugmentAction(augmentId, out pendingRollResult))
            {
                UpdateStatusText(pendingRollResult?.ErrorMessage);
                RefreshAugmentPresentation(pendingRollResult?.ErrorMessage);
                return;
            }

            if (pendingRollResult.RollPresentation == null)
            {
                if (activeDice.Count != gameSession.State.Dice.Length) ResetDiceForTurn();
                else SyncDiceStateFromAuthority();
                parchmentScoreSheet?.ClearCandidateScores();
                if (gameSession.Phase == YachtGamePhase.ScoreSelection)
                    parchmentScoreSheet?.ShowCandidateScores(gameSession.CurrentPlayerIndex, gameSession.CurrentCandidates);
                string message = GetAugmentEventMessage(pendingRollResult);
                RefreshAugmentPresentation(message);
                UpdateStatusText(message);
                return;
            }

            SyncDiceStateFromAuthority();
            hourglassTimer?.PauseTimer();
            parchmentScoreSheet?.ClearCandidateScores();
            SetRollInteraction(false);
            RefreshAugmentPresentation(GetAugmentEventMessage(pendingRollResult));
            rollRoutine = StartCoroutine(PerformBakedRollSequence());
        }

        public void ResetAndRollDice()
        {
            if (rollRoutine != null || isArranging) return;

            for (int i = 0; i < keptDice.Count; i++)
            {
                if (keptDice[i]) gameSession?.TrySetDieKept(i, false);
                keptDice[i] = false;
            }
            for (int i = 0; i < keptSlotIndices.Count; i++) keptSlotIndices[i] = -1;
            RollDice();
        }

        private IEnumerator PerformBakedRollSequence()
        {
            isArranging = false;
            hasCompletedRoll = false;
            SetRollInteraction(false);
            hoveredDieIndex = -1;
            rollIndex++;

            // 코스믹 큐브 / 수정구 황도 12궁 다음 별자리로 순차 전환 (부드러운 크로스페이드)
            if (rollCosmicCube != null)
            {
                rollCosmicCube.AdvanceZodiac();
            }
            if (rollOrb != null)
            {
                rollOrb.AdvanceZodiac();
            }

            // 주사위 값과 프리셋은 권위 명령 결과에서 이미 함께 확정되었다.
            RollPresentation presentation = pendingRollResult?.RollPresentation;
            if (presentation == null)
            {
                rollRoutine = null;
                hourglassTimer?.ResumeTimer();
                RefreshGameInteraction();
                yield break;
            }
            int clipIndex = presentation.PresetIndex;
            presetCatalog.TryGetClip(presentation.PresetFile, clipIndex, out WebPresetClip clip);
            bool isMirrored = presentation.IsMirrored;

            List<int> rolledValues = new();
            List<int> keptValues = new();
            for (int i = 0; i < diceCount; i++)
            {
                if (keptDice[i]) keptValues.Add(diceValues[i]);
                else rolledValues.Add(diceValues[i]);
            }
            Debug.Log($"<color=#2EA3FF>[주사위 굴림 #{rollIndex}]</color> Preset #{clipIndex + 1} (미러링: {isMirrored}) | 굴린 눈: [{string.Join(", ", rolledValues)}], 킵된 눈: [{string.Join(", ", keptValues)}], 전체 결과: [{string.Join(", ", diceValues)}]");

            UpdateStatusText($"주사위 굴리는 중... (Preset #{clipIndex + 1})");

            // 3. Transform 리스트 수집 및 스케일 보장
            var diceTransforms = new Transform[activeDice.Count];
            for (int i = 0; i < activeDice.Count; i++)
            {
                if (activeDice[i] != null)
                {
                    activeDice[i].transform.localScale = Vector3.one * DiceBoardMetrics.DieSize;
                    diceTransforms[i] = activeDice[i].transform;
                }
            }

            // 4. 프리셋 궤적 재생 (사운드 싱크 포함)
            yield return bakedDiceController.Play(
                diceTransforms,
                clipIndex,
                clip,
                keptDice,
                diceValues,
                isMirrored);

            // 5. 굴림 완료 후 보드 중앙 정렬 (작은 눈 -> 큰 눈 오름차순)
            yield return AnimateDiceLayout(0.45f);

            hasCompletedRoll = true;
            rollRoutine = null;
            pendingRollResult = null;
            parchmentScoreSheet?.ShowCandidateScores(gameSession.CurrentPlayerIndex, gameSession.CurrentCandidates);
            hourglassTimer?.ResumeTimer();
            RefreshGameInteraction();
            RefreshAugmentPresentation(GetAugmentEventMessage(gameSession.LastCommandResult));
            UpdateStatusText();
        }

        public bool SetDieKept(int index, bool kept)
        {
            if (gameSession == null || !gameSession.CanKeepDice || turnTransitionInProgress) return false;
            if (!hasCompletedRoll || isArranging || rollRoutine != null) return false;
            if (index < 0 || index >= keptDice.Count || activeDice[index] == null) return false;
            if (keptDice[index] == kept) return true;

            if (!gameSession.TrySetDieKept(index, kept)) return false;
            keptDice[index] = kept;
            if (kept)
            {
                // 왼쪽부터 비어있는 가장 빠른 슬롯 탐색 (기존 킵 주사위를 밀어내지 않음)
                bool[] occupied = new bool[diceCount];
                for (int i = 0; i < diceCount; i++)
                {
                    if (keptDice[i] && i != index && keptSlotIndices.Count > i && keptSlotIndices[i] >= 0 && keptSlotIndices[i] < diceCount)
                    {
                        occupied[keptSlotIndices[i]] = true;
                    }
                }
                int targetSlot = 0;
                for (int s = 0; s < diceCount; s++)
                {
                    if (!occupied[s])
                    {
                        targetSlot = s;
                        break;
                    }
                }
                while (keptSlotIndices.Count <= index) keptSlotIndices.Add(-1);
                keptSlotIndices[index] = targetSlot;
            }
            else
            {
                if (keptSlotIndices.Count > index) keptSlotIndices[index] = -1;
            }

            if (keepRoutine != null) StopCoroutine(keepRoutine);
            keepRoutine = StartCoroutine(AnimateKeepToggleRoutine());
            return true;
        }

        public void ToggleKeep(int dieIndex)
        {
            if (dieIndex < 0 || dieIndex >= keptDice.Count) return;
            SetDieKept(dieIndex, !keptDice[dieIndex]);
        }

        private IEnumerator AnimateKeepToggleRoutine()
        {
            yield return AnimateDiceLayout(0.32f);
            keepRoutine = null;
            RefreshGameInteraction();
        }

        private IEnumerator AnimateDiceLayout(float duration)
        {
            isArranging = true;

            var diceTransforms = new Transform[activeDice.Count];
            var targetPositions = new Vector3[activeDice.Count];
            var targetRotations = new Quaternion[activeDice.Count];
            var targetScales = new Vector3[activeDice.Count];

            var unkeptIndices = new List<int>();

            // 1. 킵된 주사위와 활성(킵되지 않은) 주사위 분류 및 카메라 정면 틸트 정렬 목표 회전 계산
            for (int i = 0; i < activeDice.Count; i++)
            {
                diceTransforms[i] = activeDice[i] != null ? activeDice[i].transform : null;
                float normalScale = DiceBoardMetrics.DieSize;

                // 현재 주사위 루트의 착지 회전으로부터 윗면(Top)을 유지한 채 카메라 렌즈를 정면으로 바라보도록 회전 계산
                Quaternion currentRot = activeDice[i] != null ? activeDice[i].transform.localRotation : Quaternion.identity;
                Quaternion cameraFacingRot = DiceFaceOrientation.GetCameraFacingUprightRotation(currentRot, 75.0f);

                if (keptDice[i])
                {
                    int slot = (keptSlotIndices.Count > i && keptSlotIndices[i] >= 0) ? keptSlotIndices[i] : 0;
                    targetPositions[i] = DiceBoardMetrics.GetKeepPosition(slot);
                    targetScales[i] = Vector3.one * (normalScale * DiceBoardMetrics.KeepDieScale);
                    targetRotations[i] = cameraFacingRot;
                }
                else
                {
                    unkeptIndices.Add(i);
                    targetRotations[i] = cameraFacingRot;
                }
            }

            // 2. 킵되지 않은 활성 주사위들을 왼쪽부터 오른쪽으로 작은 눈 -> 큰 눈 오름차순 정렬
            unkeptIndices.Sort((a, b) =>
            {
                int cmp = diceValues[a].CompareTo(diceValues[b]);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            for (int slot = 0; slot < unkeptIndices.Count; slot++)
            {
                int dieIndex = unkeptIndices[slot];
                targetPositions[dieIndex] = DiceBoardMetrics.GetActivePosition(slot, unkeptIndices.Count);
                targetScales[dieIndex] = Vector3.one * DiceBoardMetrics.ActiveDieSize;
            }

            // 3. 머티리얼 방식 렌더러 fallback
            for (int i = 0; i < activeDice.Count; i++)
            {
                if (activeDice[i] == null) continue;
                Transform visual = activeDice[i].transform.Find("Visual");
                if (visual == null)
                {
                    DiceMaterialFactory.ApplyPredictedTopValue(activeDice[i].transform, targetRotations[i], diceValues[i]);
                }
            }

            // 4. 부드러운 위치/회전/스케일 보간 애니메이션 수행 (순수 Yaw 수평 슬라이딩)
            yield return bakedDiceController.AnimateKeptDice(
                diceTransforms,
                keptDice,
                diceValues,
                targetPositions,
                targetRotations,
                targetScales,
                duration);

            isArranging = false;
            UpdateStatusText();
        }

        /// <summary>
        /// 입력 라우터를 붙이고 사건을 기존 처리에 연결한다(M10-T1).
        /// 라우터는 무엇을 가리키고 눌렀는지만 알리고, 무엇을 할지는 여기서 정한다.
        /// </summary>
        private void EnsureInputRouter()
        {
            if (inputRouter != null) return;

            inputRouter = GetComponent<YachtInputRouter>() ?? gameObject.AddComponent<YachtInputRouter>();
            inputRouter.WorldCamera = worldCamera;
            inputRouter.DicePointerEnabled = () => activeDice.Count > 0;
            inputRouter.AugmentPointerEnabled = () => gameSession != null
                && gameSession.Mode == YachtGameMode.Augmented
                && !gameSession.IsDrafting;

            inputRouter.RollRequested += RollDice;
            inputRouter.ResolutionPresetRequested += OnResolutionPresetRequested;
            inputRouter.DieTypeRequested += SetDieType;
            inputRouter.DieHoverChanged += OnDieHoverChanged;
            inputRouter.DieClicked += ToggleKeep;
            inputRouter.RollTriggerHoverChanged += OnRollTriggerHoverChanged;
            inputRouter.RollTriggerClicked += OnRollOrbClicked;
            inputRouter.AugmentCardHoverChanged += OnAugmentCardHoverChanged;
            inputRouter.AugmentCardClicked += OnAugmentCardClicked;
        }

        /// <summary>주사위 비주얼 풀을 붙인다(M10-T5).</summary>
        private void EnsureDicePool()
        {
            if (dicePool == null)
            {
                dicePool = GetComponent<DiceVisualPool>() ?? gameObject.AddComponent<DiceVisualPool>();
            }

            dicePool.Bind(diceModel, layoutRoot, CenterSectionX, selectedDieType);
        }

        /// <summary>주사위 색상 팔레트를 바꾼다. 입력 라우터와 모드 전환이 호출한다.</summary>
        public void SetDieType(DieType type)
        {
            selectedDieType = type;
            EnsureDicePool();
            dicePool.SetDieType(type, activeDice);
        }

        /// <summary>렌더 파이프라인 리그를 붙이고 씬 구성 요소를 넘긴다(M10-T2).</summary>
        private void EnsureCameraRig()
        {
            if (cameraRig == null)
            {
                cameraRig = GetComponent<YachtCameraRig>() ?? gameObject.AddComponent<YachtCameraRig>();
                cameraRig.CrispUiCameraReady += OnCrispUiCameraReady;
            }

            cameraRig.Bind(worldCamera, presentationCamera, gameImage, gameImageRect, layoutRoot, upscaleShader);
        }

        private void OnCrispUiCameraReady(Camera eventCamera)
        {
            if (parchmentScoreSheet == null) parchmentScoreSheet = FindFirstObjectByType<ParchmentScoreSheet>();
            if (parchmentScoreSheet != null) parchmentScoreSheet.BindEventCamera(eventCamera);
        }

        /// <summary>에디터 메뉴가 호출한다. Crisp UI 경로를 씬에 굽는다.</summary>
        public bool SetupCrispUiSceneObjects()
        {
            EnsureCameraRig();
            return cameraRig.SetupCrispUiSceneObjects();
        }

        /// <summary>조명·오디오 서비스를 붙인다(M10-T3, M10-T4).</summary>
        private void EnsureLightingRig()
        {
            if (lightingRig != null) return;

            lightingRig = GetComponent<YachtLightingRig>() ?? gameObject.AddComponent<YachtLightingRig>();
            lightingRig.PresetChanged += OnKeyLightPresetChanged;
        }

        private void EnsureAudioService()
        {
            if (audioService != null) return;

            audioService = GetComponent<YachtAudioService>() ?? gameObject.AddComponent<YachtAudioService>();
            audioService.EnsureSource();
            audioService.ClipsReady += OnAudioClipsReady;
        }

        private void OnAudioClipsReady(AudioSource source, AudioClip[] rollClips, AudioClip[] impactClips)
        {
            if (bakedDiceController != null) bakedDiceController.SetAudioSource(source, rollClips, impactClips);
        }

        private void OnKeyLightPresetChanged(string presetName)
        {
            if (keyLightToggleButton == null) return;

            Text label = keyLightToggleButton.GetComponentInChildren<Text>();
            if (label != null) label.text = $"Light: {presetName}";
        }

        /// <summary>버튼 라벨용 현재 조명 프리셋 이름.</summary>
        private string KeyLightPresetName
        {
            get
            {
                EnsureLightingRig();
                return lightingRig.CurrentPresetName;
            }
        }

        /// <summary>조명 프리셋 전환 버튼이 호출한다.</summary>
        public void ToggleKeyLightPreset()
        {
            EnsureLightingRig();
            lightingRig.TogglePreset();
        }

        private void ConfigureLighting()
        {
            EnsureLightingRig();
            lightingRig.Configure();
        }

        private void OnResolutionPresetRequested(int presetIndex)
        {
            SetResolution(presetIndex == 0 ? ResolutionA : ResolutionB);
        }

        private void OnDieHoverChanged(int dieIndex)
        {
            hoveredDieIndex = dieIndex;
            UpdateStatusText();
        }

        private void OnRollTriggerHoverChanged(bool hovered)
        {
            if (rollCosmicCube != null) rollCosmicCube.SetHovered(hovered);
            if (rollOrb != null) rollOrb.SetHovered(hovered);
        }

        private void OnAugmentCardHoverChanged(AugmentTrayCardView card)
        {
            SetHoveredAugmentSlot(card == null ? -1 : Array.IndexOf(augmentOwnedCards, card));
        }

        private void OnAugmentCardClicked(AugmentTrayCardView card)
        {
            int slot = Array.IndexOf(augmentOwnedCards, card);
            if (slot < 0) return;

            selectedAugmentSlot = selectedAugmentSlot == slot ? -1 : slot;
            for (int i = 0; i < augmentOwnedCards.Length; i++)
            {
                if (augmentOwnedCards[i] != null && augmentOwnedCards[i].gameObject.activeSelf)
                {
                    augmentOwnedCards[i].SetSelected(i == selectedAugmentSlot);
                }
            }
        }

        private void Update()
        {
            // 입력은 YachtInputRouter가 읽어 사건으로 알린다(M10-T1).
            UpdateTimerTextPosition();
            cameraRig?.FitFullScreen();
            cameraRig?.SyncCrispUiTargetToScreen();
        }


        private void OnRollOrbClicked()
        {
            if (!CanInitiateRoll()) return;

            if (rollCosmicCube != null)
            {
                rollCosmicCube.TriggerClickFeedback();
            }
            if (rollOrb != null)
            {
                rollOrb.TriggerClickFeedback();
            }

            RollDice();
        }

        private void SetRollInteraction(bool interactable)
        {
            if (rollCosmicCube != null)
            {
                rollCosmicCube.SetInteractable(interactable);
            }
            if (rollOrb != null)
            {
                rollOrb.SetInteractable(interactable);
            }
        }

        private void UpdateStatusText(string message = null)
        {
            if (statusText == null) return;

            if (gameSession == null || gameSession.Phase == YachtGamePhase.WaitingToStart)
            {
                statusText.text = message ?? "게임 시작 버튼을 눌러 주세요.";
                return;
            }
            if (gameSession.Phase == YachtGamePhase.GameOver)
            {
                statusText.text = message ?? "게임이 종료되었습니다.";
                return;
            }
            if (gameSession.Phase == YachtGamePhase.Draft)
            {
                int draftPlayer = gameSession.State.Draft.PlayerIndex;
                statusText.text = message ?? $"증강 드래프트  |  P{draftPlayer + 1}이(가) 카드를 선택합니다.";
                return;
            }

            int keptCount = keptDice.FindAll(kept => kept).Count;
            string interaction = hoveredDieIndex >= 0 && hasCompletedRoll && !isArranging
                ? (keptDice[hoveredDieIndex] ? "CLICK: UNKEEP" : "CLICK: KEEP")
                : $"KEEP {keptCount}/{diceCount}";

            string valuesSummary = hasCompletedRoll ? $" [ {string.Join(", ", diceValues)} ]" : "";
            string currentZodiac = rollCosmicCube != null ? rollCosmicCube.CurrentZodiacName : (rollOrb != null ? rollOrb.CurrentZodiacName : "");
            string zodiacInfo = !string.IsNullOrEmpty(currentZodiac) ? $"  |  ★ {currentZodiac}" : "";
            string modeText = gameSession.Mode == YachtGameMode.Augmented ? "증강" : "일반";
            string turnInfo = $"{modeText}  |  P{gameSession.CurrentPlayerIndex + 1}  |  {gameSession.CurrentRound}/12 라운드  |  굴림 {gameSession.RollsRemaining}회";

            statusText.text = string.IsNullOrEmpty(message)
                ? $"{turnInfo}  |  {interaction}{valuesSummary}{zodiacInfo}"
                : $"{message}  |  {turnInfo}  |  {interaction}{valuesSummary}{zodiacInfo}";
        }

        public void ToggleResolution()
        {
            EnsureCameraRig();
            SetResolution(cameraRig.InternalResolution == ResolutionA ? ResolutionB : ResolutionA);
        }

        private void SetResolution(Vector2Int resolution)
        {
            AugmentParchmentVisuals.PixelFilterResolution = resolution;
            RefreshDraftCardParchment();
            EnsureCameraRig();
            cameraRig.SetInternalResolution(resolution);
        }

        private bool ResolveEditableLayout()
        {
            GameObject layoutObject = GameObject.Find("Graphics Layout");
            GameObject worldCameraObject = GameObject.Find("Full Field World Camera") ?? GameObject.Find("Low Resolution World Camera");
            GameObject displayCameraObject = GameObject.Find("Display 1 Camera");
            GameObject gameAreaObject = GameObject.Find("Game Area");
            GameObject imageObject = GameObject.Find("Point Upscale");
            GameObject statusObject = GameObject.Find("Status");

            if (layoutObject == null || worldCameraObject == null || displayCameraObject == null || gameAreaObject == null || imageObject == null)
            {
                return false;
            }

            layoutRoot = layoutObject.transform;
            worldCamera = worldCameraObject.GetComponent<Camera>();
            presentationCamera = displayCameraObject.GetComponent<Camera>();
            gameAreaRect = gameAreaObject.GetComponent<RectTransform>();
            gameImageRect = imageObject.GetComponent<RectTransform>();
            gameImage = imageObject.GetComponent<RawImage>();
            statusText = statusObject != null ? statusObject.GetComponent<Text>() : null;

            if (imageObject != null)
            {
                imageObject.SetActive(true);
            }

            EnsureSingleAudioListener();

            BindTabletopProps();

            ApplyTopDownCamera();
            EnsureCameraRig();
            cameraRig.CreateRenderTarget();
            cameraRig?.ApplyRenderSettings();
            return worldCamera != null && presentationCamera != null && gameImage != null;
        }

        private void EnsureSingleAudioListener()
        {
            if (worldCamera == null)
            {
                GameObject camObj = GameObject.Find("Full Field World Camera") ?? GameObject.Find("Low Resolution World Camera");
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
                    if (Application.isPlaying) Destroy(al);
                    else DestroyImmediate(al);
                }
            }

            if (!foundPrimary && worldCamera != null)
            {
                AudioListener al = worldCamera.GetComponent<AudioListener>();
                if (al == null) al = worldCamera.gameObject.AddComponent<AudioListener>();
                al.enabled = true;
            }
        }

        private void EnsureEventSystem()
        {
            if (!Application.isPlaying || FindFirstObjectByType<EventSystem>() != null) return;

            GameObject events = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            DontDestroyOnLoad(events);
        }

        private void BindPresentationActions()
        {
            Button resolutionButton = GameObject.Find("Debug")?.GetComponent<Button>();
            keyLightToggleButton = GameObject.Find("KeyLightToggle")?.GetComponent<Button>();
            runeFxButton = GameObject.Find("RuneFxDebug")?.GetComponent<Button>();
            runeStoneButton = GameObject.Find("RuneStoneDebug")?.GetComponent<Button>();
            GameObject quantizeObject = GameObject.Find("Quantize");
            if (resolutionButton != null)
            {
                resolutionButton.onClick.RemoveAllListeners();
                resolutionButton.onClick.AddListener(ToggleResolution);
            }
            if (keyLightToggleButton == null)
            {
                GameObject canvasObj = GameObject.Find("Pixel Presentation");
                if (canvasObj != null)
                {
                    keyLightToggleButton = CreateButton(canvasObj.transform, "KeyLightToggle", $"Light: {KeyLightPresetName}", new Vector2(158f, -18f), new Vector2(165f, 38f), new Vector2(0f, 1f), ToggleKeyLightPreset);
                }
            }
            if (keyLightToggleButton != null)
            {
                keyLightToggleButton.onClick.RemoveAllListeners();
                keyLightToggleButton.onClick.AddListener(ToggleKeyLightPreset);
                Text label = keyLightToggleButton.GetComponentInChildren<Text>();
                if (label != null) label.text = $"Light: {KeyLightPresetName}";
            }
            
            GameObject canvasObject = GameObject.Find("Pixel Presentation");
            if (canvasObject != null)
            {
                if (runeFxButton == null)
                {
                    runeFxButton = CreateButton(canvasObject.transform, "RuneFxDebug", "Runes: 0/12", new Vector2(333f, -18f), new Vector2(140f, 38f), new Vector2(0f, 1f), DebugAdvanceRuneLighting);
                }
                if (runeStoneButton == null)
                {
                    runeStoneButton = CreateButton(canvasObject.transform, "RuneStoneDebug", "Stones: 0/4", new Vector2(483f, -18f), new Vector2(150f, 38f), new Vector2(0f, 1f), DebugCycleRuneStones);
                }
            }
            if (runeFxButton != null)
            {
                runeFxButton.onClick.RemoveAllListeners();
                runeFxButton.onClick.AddListener(DebugAdvanceRuneLighting);
            }
            if (runeStoneButton != null)
            {
                runeStoneButton.onClick.RemoveAllListeners();
                runeStoneButton.onClick.AddListener(DebugCycleRuneStones);
            }
            UpdateRunicDebugButtonLabels();
if (quantizeObject != null) quantizeObject.SetActive(false);
        }

        private void BuildWorld()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.44f, 0.40f);

            EnsureLayoutRoot();

            // 씬 전체의 모든 구버전 카메라 전수 검색 및 삭제 (중복 생성 방지)
            GameObject[] allSceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject go in allSceneObjects)
            {
                if (go == null) continue;
#if UNITY_EDITOR
                if (UnityEditor.EditorUtility.IsPersistent(go)) continue;
#endif
                if (go.name == "Full Field World Camera" || go.name == "Low Resolution World Camera")
                {
                    if (Application.isPlaying) Destroy(go);
                    else DestroyImmediate(go);
                }
            }

            Transform existingLight = layoutRoot != null ? layoutRoot.Find("Key Light") : null;
            if (existingLight != null)
            {
                if (Application.isPlaying) Destroy(existingLight.gameObject);
                else DestroyImmediate(existingLight.gameObject);
            }

            GameObject cameraObject = new("Full Field World Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(layoutRoot, false);
            worldCamera = cameraObject.GetComponent<Camera>();
            ApplyTopDownCamera();
            worldCamera.nearClipPlane = 0.1f;
            worldCamera.farClipPlane = 40f;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = new Color(0.06f, 0.045f, 0.04f);
            worldCamera.allowHDR = false;
            worldCamera.allowMSAA = false;

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
            lightObject.transform.SetParent(layoutRoot, true);

            ConfigureLighting();
        }

        private void ApplyTopDownCamera()
        {
            ApplyAngledWorldCamera(75.0f);
        }

        private void ApplyAngledWorldCamera(float pitchAngle = 75.0f)
        {
            if (worldCamera == null) return;
            worldCamera.transform.position = new Vector3(CenterSectionX, 11.5f, -3.1f);
            worldCamera.transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 8.2f;
        }

        /// <summary>
        /// 씬에 배치된 테이블 프롭을 컨트롤러 참조에 연결한다.
        /// 생성하지 않는다. 누락된 프롭은 경고로만 알리고, 프리팹을 씬에 배치해 해결한다.
        /// </summary>
        private void BindTabletopProps()
        {
            if (parchmentScoreSheet == null) parchmentScoreSheet = FindFirstObjectByType<ParchmentScoreSheet>();
            if (augmentCardTray == null) augmentCardTray = FindFirstObjectByType<AugmentCardTray>();
            if (rollOrb == null) rollOrb = FindFirstObjectByType<RollOrb>();
            if (rollCosmicCube == null) rollCosmicCube = FindFirstObjectByType<RollCosmicCube>();
            if (rerollCounterBar == null) rerollCounterBar = FindFirstObjectByType<RerollCounterBar>();
            if (hourglassTimer == null) hourglassTimer = FindFirstObjectByType<HourglassTimer>();
            if (candleStand == null) candleStand = FindFirstObjectByType<CozyCandleStand>();
            if (runicSlateMatrix == null) runicSlateMatrix = FindFirstObjectByType<RunicSlateMatrix>();
            if (trinketCluster == null) trinketCluster = FindFirstObjectByType<TabletopTrinketCluster>();
            if (turnBalanceIndicator == null) turnBalanceIndicator = FindFirstObjectByType<TurnBalanceIndicator>();

            WarnIfMissing(parchmentScoreSheet, nameof(ParchmentScoreSheet));
            WarnIfMissing(augmentCardTray, nameof(AugmentCardTray));
            WarnIfMissing(rollCosmicCube, nameof(RollCosmicCube));
            WarnIfMissing(rerollCounterBar, nameof(RerollCounterBar));
            WarnIfMissing(hourglassTimer, nameof(HourglassTimer));
            WarnIfMissing(candleStand, nameof(CozyCandleStand));
            WarnIfMissing(runicSlateMatrix, nameof(RunicSlateMatrix));
            WarnIfMissing(trinketCluster, nameof(TabletopTrinketCluster));
            WarnIfMissing(turnBalanceIndicator, nameof(TurnBalanceIndicator));

            if (parchmentScoreSheet != null)
            {
                parchmentScoreSheet.EnsureStructure();
                parchmentScoreSheet.RefreshAllScores();
            }
            if (rerollCounterBar != null) rerollCounterBar.SetRollsRemaining(3, 3);
        }

        private static void WarnIfMissing(UnityEngine.Object prop, string typeName)
        {
            if (prop == null)
            {
                Debug.LogWarning($"[AugmentedYachtController] 씬에 {typeName} 프롭이 없습니다. Assets/Prefabs/Tabletop 에서 배치하십시오.");
            }
        }

        private static TurnSide MapPlayerToTurnSide(int playerIndex)
        {
            return playerIndex == 0 ? TurnSide.Left : TurnSide.Right;
        }

        /// <summary>
        /// 테이블·러너·트레이를 절차적으로 다시 만든다. 자동 호출하지 않는다.
        ///
        /// 이 셋은 독립 컴포넌트가 없어 생성 코드가 여기에만 있다. 평소에는 프리팹이 형상을 소유하고
        /// 배치는 씬이 소유하지만, 형상 자체를 바꿔야 할 때는 이걸 실행해 다시 만든 뒤
        /// Tessera/Tabletop/Bake Tabletop Prefabs 로 프리팹을 다시 굽고 씬에 다시 배치한다.
        /// </summary>
        [ContextMenu("Regenerate Table Surfaces (bake 전용)")]
        public void RegenerateTableSurfaces()
        {
            if (Application.isPlaying) return;

            EnsureLayoutRoot();
            DestroyLayoutChild("3D Wood Planks Table");
            DestroyLayoutChild("3D Fabric Runner");
            DestroyLayoutChild("Yacht Tray Visual");

            Create3DWoodPlanksTable();
            Create3DFabricRunner();
            CreateYachtTrayVisual();
            SyncTrayVisualMat();
        }

        private void DestroyLayoutChild(string childName)
        {
            Transform child = layoutRoot != null ? layoutRoot.Find(childName) : null;
            if (child == null) return;

            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }

        private void EnsureLayoutRoot()
        {
            if (layoutRoot != null) return;
            GameObject existing = GameObject.Find("Graphics Layout");
            if (existing != null)
            {
                layoutRoot = existing.transform;
                return;
            }

            GameObject root = new("Graphics Layout");
            root.transform.SetParent(transform, false);
            layoutRoot = root.transform;
        }

        private void Create3DWoodPlanksTable()
        {
            GameObject tableRoot = new("3D Wood Planks Table");
            tableRoot.layer = DecorationLayer;
            tableRoot.transform.SetParent(layoutRoot, false);
            tableRoot.transform.position = Vector3.zero;

            int plankCount = 4;
            float totalHeight = 20.0f;
            float plankHeight = 4.90f;
            float gap = 0.10f;
            float plankWidth = 38.0f;
            float plankThickness = 0.60f;
            float baseY = -0.72f;

            // 0. 판자 틈새 그림자 역할의 언더레이어 밑판 (틈새로 배경이 비치지 않고 자연스러운 음영 연출)
            GameObject underlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            underlay.name = "Table Shadow Underlay";
            underlay.layer = DecorationLayer;
            underlay.transform.SetParent(tableRoot.transform, false);
            underlay.transform.position = new Vector3(CenterSectionX, baseY - 0.20f, 0f);
            underlay.transform.localScale = new Vector3(plankWidth, 0.20f, totalHeight + 1.0f);

            Collider underlayCol = underlay.GetComponent<Collider>();
            if (underlayCol != null)
            {
                if (Application.isPlaying) Destroy(underlayCol);
                else DestroyImmediate(underlayCol);
            }

            Material underlayMat = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                name = "Runtime Table Shadow Underlay Material",
                color = new Color32(20, 15, 12, 255)
            };
            if (underlayMat.HasProperty("_BaseColor")) underlayMat.SetColor("_BaseColor", new Color32(20, 15, 12, 255));
            if (underlayMat.HasProperty("_Color")) underlayMat.SetColor("_Color", new Color32(20, 15, 12, 255));
            underlayMat.SetFloat("_Smoothness", 0.05f);
            underlayMat.SetFloat("_Metallic", 0f);

            MeshRenderer underlayMr = underlay.GetComponent<MeshRenderer>();
            underlayMr.material = underlayMat;
            underlayMr.shadowCastingMode = ShadowCastingMode.TwoSided;
            underlayMr.receiveShadows = true;

            Color[] plankColors = new Color[]
            {
                new Color32(110, 67, 42, 255), // Plank 1: #6e432a (Warm Honey Brown)
                new Color32(120, 73, 46, 255), // Plank 2: #78492e (Amber Toast Brown)
                new Color32(99, 60, 37, 255),  // Plank 3: #633c25 (Deep Toffee Walnut)
                new Color32(115, 69, 43, 255)  // Plank 4: #73452b (Warm Walnut Brown)
            };

            // 판자마다 서로 다른 옹이(Knot)와 결 위치를 위한 UV Offset & Scale
            Vector2[] uvOffsets = new Vector2[]
            {
                new(0.00f, 0.00f),
                new(0.40f, 0.20f),
                new(0.80f, 0.60f),
                new(0.20f, 0.40f)
            };

            Texture2D woodTexture = null;
#if UNITY_EDITOR
            woodTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Wood/wood_grain_knots.png");
#endif

            float startZ = -totalHeight * 0.5f + plankHeight * 0.5f;

            for (int i = 0; i < plankCount; i++)
            {
                float z = startZ + i * (plankHeight + gap);
                float yOffset = ((i % 2 == 0) ? 0.008f : -0.008f); // 판자 간 자연스러운 3D 높낮이 단차
                Vector3 pos = new(CenterSectionX, baseY + yOffset, z);
                Vector3 size = new(plankWidth, plankThickness, plankHeight);

                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = $"Heavy Wood Plank {i + 1}";
                plank.layer = DecorationLayer;
                plank.transform.SetParent(tableRoot.transform, false);
                plank.transform.position = pos;
                plank.transform.localScale = size;

                Collider col = plank.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }

                Material mat = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                {
                    name = $"Runtime Heavy Wood Plank {i + 1} Material",
                    color = plankColors[i % plankColors.Length]
                };

                if (woodTexture != null)
                {
                    mat.mainTexture = woodTexture;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", woodTexture);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", woodTexture);

                    Vector2 tiling = new(1.5f, 1.0f);
                    Vector2 offset = uvOffsets[i % uvOffsets.Length];
                    mat.mainTextureScale = tiling;
                    mat.mainTextureOffset = offset;
                    if (mat.HasProperty("_BaseMap"))
                    {
                        mat.SetTextureScale("_BaseMap", tiling);
                        mat.SetTextureOffset("_BaseMap", offset);
                    }
                    if (mat.HasProperty("_MainTex"))
                    {
                        mat.SetTextureScale("_MainTex", tiling);
                        mat.SetTextureOffset("_MainTex", offset);
                    }
                }

                mat.SetFloat("_Smoothness", 0.20f);
                mat.SetFloat("_Metallic", 0f);

                MeshRenderer mr = plank.GetComponent<MeshRenderer>();
                mr.material = mat;
                mr.shadowCastingMode = ShadowCastingMode.TwoSided;
                mr.receiveShadows = true;
            }
        }

        private static Texture2D CreateBurgundyCorduroyTexture()
        {
            int width = 512;
            int height = 512;
            Texture2D tex = new(width, height, TextureFormat.RGBA32, true)
            {
                name = "Runtime Burgundy Corduroy Texture",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[width * height];
            int numRibs = 20; // 20개의 선명하고 굵은 가로 코듀로이 골

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                float phase = v * numRibs * 2f * Mathf.PI;
                float sinVal = Mathf.Sin(phase);
                float ridgeProfile = Mathf.Sign(sinVal) * Mathf.Pow(Mathf.Abs(sinVal), 0.55f);
                float tRidge = (ridgeProfile + 1f) * 0.5f;

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    float s1 = Mathf.Sin(u * 3.7f * 2f * Mathf.PI + v * 2.1f * 2f * Mathf.PI) * Mathf.Cos(u * 1.9f * 2f * Mathf.PI - v * 3.4f * 2f * Mathf.PI);
                    float s2 = Mathf.Sin(u * 8.3f * 2f * Mathf.PI - v * 6.5f * 2f * Mathf.PI) * 0.5f;
                    float organicWave = (s1 + s2) / 1.5f;
                    float toneBlend = Mathf.Clamp01(0.5f + 0.5f * organicWave);
                    float microWeave = ((Mathf.Sin(x * 0.85f) + Mathf.Cos(y * 0.85f)) * 0.5f) * 0.04f;

                    float r = Mathf.Clamp01((35f + 110f * tRidge + 35f * toneBlend + microWeave * 40f) / 255f);
                    float g = Mathf.Clamp01((4f + 26f * tRidge + 18f * toneBlend + microWeave * 25f) / 255f);
                    float b = Mathf.Clamp01((10f + 48f * tRidge + 24f * toneBlend + microWeave * 25f) / 255f);

                    pixels[y * width + x] = new Color(r, g, b, 1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true);
            return tex;
        }

        private void Create3DFabricRunner()
        {
            GameObject runnerRoot = new("3D Fabric Runner");
            runnerRoot.layer = DecorationLayer;
            runnerRoot.transform.SetParent(layoutRoot, false);
            runnerRoot.transform.position = new Vector3(CenterSectionX, -0.40f, 0.4f);
            runnerRoot.transform.rotation = Quaternion.Euler(0f, 4.5f, 0f);

            // 1. 딥 크림슨 펠트 본체 (로우폴리 스타일라이즈드 솔리드 메쉬)
            GameObject feltBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            feltBody.name = "Crimson Felt Body";
            feltBody.layer = DecorationLayer;
            feltBody.transform.SetParent(runnerRoot.transform, false);
            feltBody.transform.localPosition = Vector3.zero;
            feltBody.transform.localScale = new Vector3(42.0f, 0.040f, 7.2f);

            Collider bodyCol = feltBody.GetComponent<Collider>();
            if (bodyCol != null)
            {
                if (Application.isPlaying) Destroy(bodyCol);
                else DestroyImmediate(bodyCol);
            }

            Color crimsonColor = new Color32(136, 45, 34, 255); // #882d22 (Deep Crimson)
            Material feltMat = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                name = "Runtime 3D LowPoly Crimson Felt Material",
                color = crimsonColor
            };
            if (feltMat.HasProperty("_BaseColor")) feltMat.SetColor("_BaseColor", crimsonColor);
            if (feltMat.HasProperty("_Color")) feltMat.SetColor("_Color", crimsonColor);
            feltMat.SetFloat("_Smoothness", 0.12f);
            feltMat.SetFloat("_Metallic", 0f);

            MeshRenderer bodyMr = feltBody.GetComponent<MeshRenderer>();
            bodyMr.material = feltMat;
            bodyMr.shadowCastingMode = ShadowCastingMode.TwoSided;
            bodyMr.receiveShadows = true;

            // 2. 상/하 앤틱 골드 리본 트림 2줄 (안쪽 인셋 ±2.75f)
            Color goldColor = new Color32(229, 169, 60, 255); // #e5a93c (Antique Gold)
            Material goldMat = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                name = "Runtime 3D LowPoly Antique Gold Ribbon Material",
                color = goldColor
            };
            if (goldMat.HasProperty("_BaseColor")) goldMat.SetColor("_BaseColor", goldColor);
            if (goldMat.HasProperty("_Color")) goldMat.SetColor("_Color", goldColor);
            goldMat.SetFloat("_Smoothness", 0.78f);
            goldMat.SetFloat("_Metallic", 0.88f);

            float[] trimZ = { -2.75f, 2.75f };
            for (int t = 0; t < 2; t++)
            {
                GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trim.name = $"Gold Trim {(t == 0 ? "Top" : "Bottom")}";
                trim.layer = DecorationLayer;
                trim.transform.SetParent(runnerRoot.transform, false);
                trim.transform.localPosition = new Vector3(0f, 0.004f, trimZ[t]);
                trim.transform.localScale = new Vector3(42.0f, 0.044f, 0.20f);

                Collider trimCol = trim.GetComponent<Collider>();
                if (trimCol != null)
                {
                    if (Application.isPlaying) Destroy(trimCol);
                    else DestroyImmediate(trimCol);
                }

                MeshRenderer trimMr = trim.GetComponent<MeshRenderer>();
                trimMr.material = goldMat;
                trimMr.shadowCastingMode = ShadowCastingMode.TwoSided;
                trimMr.receiveShadows = true;
            }
        }

        private void CreateYachtTrayVisual()
        {
            if (yachtTrayMesh == null) return;
            GameObject tray = new("Yacht Tray Visual", typeof(MeshFilter), typeof(MeshRenderer));
            tray.transform.SetParent(layoutRoot, false);
            tray.transform.localPosition = new Vector3(CenterSectionX, TrayVisualY, DiceBoardMetrics.TrayCenterZ);
            tray.transform.localRotation = Quaternion.identity;
            tray.transform.localScale = Vector3.one * TrayScale;

            Mesh trayMeshInstance = yachtTrayMesh;
            if (trayMeshInstance.uv == null || trayMeshInstance.uv.Length == 0)
            {
                trayMeshInstance = Instantiate(yachtTrayMesh);
                Vector3[] verts = trayMeshInstance.vertices;
                Vector3[] norms = trayMeshInstance.normals;
                Vector2[] uvs = new Vector2[verts.Length];
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 v = verts[i];
                    Vector3 n = (norms != null && i < norms.Length) ? norms[i] : Vector3.up;
                    if (Mathf.Abs(n.y) >= 0.7f)
                        uvs[i] = new Vector2(v.x * (1f / 50f), v.z * (1f / 50f));
                    else
                        uvs[i] = new Vector2((Mathf.Abs(n.x) > Mathf.Abs(n.z) ? v.z : v.x) * (1f / 50f), v.y * (1f / 50f));
                }
                trayMeshInstance.uv = uvs;
            }
            tray.GetComponent<MeshFilter>().sharedMesh = trayMeshInstance;

            Material rim = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            rim.name = "Runtime Yacht Tray Rim Material";
            rim.color = new Color(0.045f, 0.045f, 0.05f);
            rim.SetFloat("_Smoothness", 0.22f);

            Material felt = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            felt.name = "Runtime Yacht Tray Felt Material";
            Texture2D corduroyTex = CreateBurgundyCorduroyTexture();
            felt.mainTexture = corduroyTex;
            if (felt.HasProperty("_BaseMap")) felt.SetTexture("_BaseMap", corduroyTex);
            if (felt.HasProperty("_MainTex")) felt.SetTexture("_MainTex", corduroyTex);
            if (felt.HasProperty("_BaseColor")) felt.SetColor("_BaseColor", Color.white);
            if (felt.HasProperty("_Color")) felt.SetColor("_Color", Color.white);
            felt.mainTextureScale = new Vector2(1f, 1f);
            felt.color = Color.white;
            felt.SetFloat("_Smoothness", 0.12f);

            tray.GetComponent<MeshRenderer>().sharedMaterials = new[] { rim, felt };
            tray.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.TwoSided;
            tray.GetComponent<MeshRenderer>().receiveShadows = true;
        }

        public void DebugAdvanceRuneLighting()
        {
            ResolveRunicMatrix();
            runicSlateMatrix?.AdvanceDebugRuneLighting();
            UpdateRunicDebugButtonLabels();
        }

        public void DebugCycleRuneStones()
        {
            ResolveRunicMatrix();
            runicSlateMatrix?.CycleDebugRuneStoneCount();
            UpdateRunicDebugButtonLabels();
        }

        public void GrantExtraTurnsFromAugment(int amount)
        {
            ResolveRunicMatrix();
            runicSlateMatrix?.GrantExtraTurns(amount);
        }

        public bool ConsumeExtraTurn()
        {
            ResolveRunicMatrix();
            return runicSlateMatrix != null && runicSlateMatrix.ConsumeExtraTurn();
        }

        public bool ApplyAugmentScoreOverwrite(int playerIndex, ScoreCategory category, int score, int grantedExtraTurns)
        {
            if (parchmentScoreSheet == null) parchmentScoreSheet = FindFirstObjectByType<ParchmentScoreSheet>();
            if (parchmentScoreSheet == null || !parchmentScoreSheet.OverwriteScoreFromAugment(playerIndex, category, score)) return false;

            GrantExtraTurnsFromAugment(grantedExtraTurns);
            return true;
        }

        private void ResolveRunicMatrix()
        {
            if (runicSlateMatrix == null) runicSlateMatrix = FindFirstObjectByType<RunicSlateMatrix>();
            if (runicSlateMatrix == null) return;

            runicSlateMatrix.StateChanged -= UpdateRunicDebugButtonLabels;
            runicSlateMatrix.StateChanged += UpdateRunicDebugButtonLabels;
        }

        private void UpdateRunicDebugButtonLabels()
        {
            ResolveRunicMatrix();
            int runeProgress = runicSlateMatrix != null ? runicSlateMatrix.OuterRuneProgress : 0;
            int stoneCount = runicSlateMatrix != null ? runicSlateMatrix.ExtraTurnCount : 0;
            int stoneCapacity = runicSlateMatrix != null ? runicSlateMatrix.MaxExtraTurns : 4;

            Text runeLabel = runeFxButton != null ? runeFxButton.GetComponentInChildren<Text>() : null;
            if (runeLabel != null) runeLabel.text = $"Runes: {runeProgress}/12";

            Text stoneLabel = runeStoneButton != null ? runeStoneButton.GetComponentInChildren<Text>() : null;
            if (stoneLabel != null) stoneLabel.text = $"Stones: {stoneCount}/{stoneCapacity}";
        }

        private void BuildPresentation()
        {
            EnsureEventSystem();

            // 씬 전체의 모든 구버전 Pixel Presentation 및 Display 1 Camera 전수 검색 및 영구 삭제
            GameObject[] allSceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject go in allSceneObjects)
            {
                if (go == null) continue;
#if UNITY_EDITOR
                if (UnityEditor.EditorUtility.IsPersistent(go)) continue;
#endif
                if (go.name == "Pixel Presentation" || go.name == "Display 1 Camera")
                {
                    if (Application.isPlaying) Destroy(go);
                    else DestroyImmediate(go);
                }
            }

            GameObject canvasObject = new("Pixel Presentation", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            EnsureCameraRig();
            cameraRig.CreatePresentationCamera();
            presentationCamera = cameraRig.PresentationCamera;

            GameObject gameArea = new("Game Area", typeof(RectTransform));
            gameArea.transform.SetParent(canvasObject.transform, false);
            gameAreaRect = gameArea.GetComponent<RectTransform>();
            gameAreaRect.anchorMin = Vector2.zero;
            gameAreaRect.anchorMax = Vector2.one;
            gameAreaRect.offsetMin = gameAreaRect.offsetMax = Vector2.zero;

            GameObject imageObject = new("Point Upscale", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(gameArea.transform, false);
            gameImageRect = imageObject.GetComponent<RectTransform>();
            gameImageRect.anchorMin = gameImageRect.anchorMax = new Vector2(0.5f, 0.5f);
            gameImageRect.pivot = new Vector2(0.5f, 0.5f);
            gameImage = imageObject.GetComponent<RawImage>();
            gameImage.raycastTarget = false;

            EnsureCameraRig();
            cameraRig.SetGameImage(gameImage, gameImageRect);
            cameraRig.EnsureUpscaleMaterial();
            imageObject.SetActive(true);

            CreateButton(canvasObject.transform, "Debug", "960 / 640", new Vector2(18f, -18f), new Vector2(130f, 38f), new Vector2(0f, 1f), ToggleResolution);
            
            runeFxButton = CreateButton(canvasObject.transform, "RuneFxDebug", "Runes: 0/12", new Vector2(333f, -18f), new Vector2(140f, 38f), new Vector2(0f, 1f), DebugAdvanceRuneLighting);
            runeStoneButton = CreateButton(canvasObject.transform, "RuneStoneDebug", "Stones: 0/4", new Vector2(483f, -18f), new Vector2(150f, 38f), new Vector2(0f, 1f), DebugCycleRuneStones);
keyLightToggleButton = CreateButton(canvasObject.transform, "KeyLightToggle", $"Light: {KeyLightPresetName}", new Vector2(158f, -18f), new Vector2(165f, 38f), new Vector2(0f, 1f), ToggleKeyLightPreset);

            statusText = CreateText(canvasObject.transform, "Status", "", new Vector2(0f, -20f), new Vector2(600f, 30f), new Vector2(0.5f, 1f), 15, TextAnchor.MiddleCenter);
            Canvas.ForceUpdateCanvases();
            EnsureCameraRig();
            cameraRig.CreateRenderTarget();
            BindPresentationActions();

            if (parchmentScoreSheet == null) parchmentScoreSheet = FindFirstObjectByType<ParchmentScoreSheet>();
            if (parchmentScoreSheet != null)
            {
                parchmentScoreSheet.EnsureStructure();
                parchmentScoreSheet.RefreshAllScores();
            }
        }


        private Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Vector2 anchor, UnityEngine.Events.UnityAction action)
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

        private static Text CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size, Vector2 anchor, int fontSize, TextAnchor alignment, bool stretch = false)
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









        private void OnDestroy()
        {
            if (parchmentScoreSheet != null)
            {
                parchmentScoreSheet.ScoreSelected -= OnScoreSelected;
            }
            if (hourglassTimer != null)
            {
                hourglassTimer.OnTimerStarted -= OnTurnTimerStarted;
                hourglassTimer.OnTimerTick -= OnTurnTimerTick;
                hourglassTimer.OnTimerExpired -= OnTurnTimerExpired;
            }
            cameraRig?.Dispose();
            dicePool?.Dispose();
        }
    }
}
