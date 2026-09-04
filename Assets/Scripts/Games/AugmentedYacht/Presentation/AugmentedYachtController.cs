using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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
        private AugmentTrayPresenter augmentTray;
        private DiceVisualPool dicePool;

        /// <summary>턴 지연 연출의 출처. 지금은 모래시계가 맡는다(M10-T6b).</summary>
        private ITurnDelaySource turnDelay;
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
        private Text timerText;
        private Text resultText;
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

        /// <summary>
        /// 화면이 입력을 받을 수 있는 안정 상태인가(M10-T6c).
        ///
        /// 턴 전환 연출 중도 아니고, 굴림 코루틴도 돌지 않고, 주사위 정렬 중도 아닌 상태다.
        /// 굴림 버튼·증강 행동 버튼·판 뒤집기가 모두 같은 조건을 보므로 한 곳에 모았다.
        /// </summary>
        private bool IsInteractive => !turnTransitionInProgress && rollRoutine == null && !isArranging;

        /// <summary>
        /// 주사위가 굴러 멈춘 뒤인가. 에디터 물리 검증 도구가 쓴다.
        /// 턴 전환은 보지 않는다. 게임 흐름이 아니라 물리 안정화만 판정하기 때문이다.
        /// </summary>
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
            TabletopSurfaceBuilder.SyncTrayMaterial();
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

            turnDelay = hourglassTimer;
            if (turnDelay != null)
            {
                turnDelay.Started -= OnTurnTimerStarted;
                turnDelay.Ticked -= OnTurnTimerTick;
                turnDelay.Expired -= OnTurnTimerExpired;
                turnDelay.Started += OnTurnTimerStarted;
                turnDelay.Ticked += OnTurnTimerTick;
                turnDelay.Expired += OnTurnTimerExpired;
                turnDelay.SetIdle(TurnDurationSeconds);
            }

            runicSlateMatrix?.SetRoundProgress(0);
            rerollCounterBar?.SetRollsRemaining(YachtGameSession.MaxRolls, YachtGameSession.MaxRolls);
            EnsureGameFlowUI();
            EnsureAugmentTray();
            augmentTray.EnsureOwnedCardViews();
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
            Transform existingTimer = canvasObject.transform.Find("Yacht Turn Timer Text");
            if (existingTimer != null)
            {
                if (Application.isPlaying) Destroy(existingTimer.gameObject);
                else DestroyImmediate(existingTimer.gameObject);
            }

            timerText = YachtHudFactory.CreateText(canvasObject.transform, "Yacht Turn Timer Text", "--", Vector2.zero,
                new Vector2(120f, 46f), new Vector2(0.5f, 0.5f), 30, TextAnchor.MiddleCenter);
            timerText.color = new Color32(255, 226, 151, 255);

            startGameOverlay = YachtHudFactory.CreateFullScreenOverlay(canvasObject.transform, "Yacht Game Start Overlay");
            Text title = YachtHudFactory.CreateText(startGameOverlay.transform, "Title", "요트 다이스", new Vector2(0f, 90f),
                new Vector2(620f, 90f), new Vector2(0.5f, 0.5f), 42, TextAnchor.MiddleCenter);
            title.color = new Color32(255, 222, 151, 255);
            YachtHudFactory.CreateButton(startGameOverlay.transform, "Start Normal Yacht Game", "일반 요트", new Vector2(0f, -5f),
                new Vector2(260f, 64f), new Vector2(0.5f, 0.5f), () => StartNewGame(YachtGameMode.Normal));
            YachtHudFactory.CreateButton(startGameOverlay.transform, "Start Augmented Yacht Game", "증강 요트", new Vector2(0f, -85f),
                new Vector2(260f, 64f), new Vector2(0.5f, 0.5f), () => StartNewGame(YachtGameMode.Augmented));

            gameResultOverlay = YachtHudFactory.CreateFullScreenOverlay(canvasObject.transform, "Yacht Game Result Overlay");
            resultText = YachtHudFactory.CreateText(gameResultOverlay.transform, "Result", "", new Vector2(0f, 35f),
                new Vector2(720f, 150f), new Vector2(0.5f, 0.5f), 36, TextAnchor.MiddleCenter);
            resultText.color = new Color32(255, 222, 151, 255);
            YachtHudFactory.CreateButton(gameResultOverlay.transform, "Restart Yacht Game", "다시 시작", new Vector2(0f, -105f),
                new Vector2(240f, 64f), new Vector2(0.5f, 0.5f), StartNewGame);
            gameResultOverlay.SetActive(false);

            EnsureAugmentTray();
            EnsureCameraRig();
            AugmentParchmentVisuals.PixelFilterResolution = cameraRig.InternalResolution;
            augmentTray.BuildUi(canvasObject.transform);
        }

        /// <summary>
        /// 증강 카드 프레젠터를 붙이고 참조를 맞춘다(M10-T7).
        ///
        /// 트레이와 카메라는 씨 해석 순서에 따라 늦게 채워지므로 호출할 때마다 다시 넘긴다.
        /// 한 번만 묶으면 첫 갱신 시점의 null을 그대로 부여잡는다.
        /// </summary>
        private void EnsureAugmentTray()
        {
            if (augmentTray == null)
            {
                augmentTray = GetComponent<AugmentTrayPresenter>() ?? gameObject.AddComponent<AugmentTrayPresenter>();
                augmentTray.DraftOptionSelected += SelectDraftOption;
                augmentTray.ActionRequested += UseAugmentAction;
            }

            augmentTray.Bind(augmentCardTray, worldCamera);
        }

        private void RefreshAugmentPresentation(string message = null)
        {
            EnsureAugmentTray();
            augmentTray.Refresh(gameSession, IsInteractive, message);
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
                turnDelay?.SetIdle(TurnDurationSeconds);
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

            if (turnDelay != null) turnDelay.Begin(turnDuration, true);
            else OnTurnTimerStarted();
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

            if (advancedTurn && turnDelay != null)
            {
                float turnDuration = gameSession.CurrentTurnDurationSeconds;
                turnDelay.Reset(turnDuration);
                turnDelay.Resume();
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

            if (turnDelay != null) turnDelay.Begin(TurnDurationSeconds, true);
            else OnTurnTimerStarted();
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
            bool canRoll = gameSession.CanRoll && IsInteractive && !allKept;
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
            if (gameSession == null || !gameSession.CanRoll || !IsInteractive) return false;
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
            if (gameSession == null || !IsInteractive) return;
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
            isArranging = true;
            yield return dicePool.AnimateLayout(0.45f, activeDice, keptDice, keptSlotIndices, diceValues, bakedDiceController);
            isArranging = false;
            UpdateStatusText();

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
            if (gameSession == null || !gameSession.CanKeepDice) return false;
            if (!IsInteractive || !hasCompletedRoll) return false;
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
            isArranging = true;
            yield return dicePool.AnimateLayout(0.32f, activeDice, keptDice, keptSlotIndices, diceValues, bakedDiceController);
            isArranging = false;
            UpdateStatusText();
            keepRoutine = null;
            RefreshGameInteraction();
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
            augmentTray.SetHoveredCard(card);
        }

        private void OnAugmentCardClicked(AugmentTrayCardView card)
        {
            augmentTray.ToggleSelection(card);
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
            augmentTray?.RefreshDraftCardParchment();
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
                    keyLightToggleButton = YachtHudFactory.CreateButton(canvasObj.transform, "KeyLightToggle", $"Light: {KeyLightPresetName}", new Vector2(158f, -18f), new Vector2(165f, 38f), new Vector2(0f, 1f), ToggleKeyLightPreset);
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
                    runeFxButton = YachtHudFactory.CreateButton(canvasObject.transform, "RuneFxDebug", "Runes: 0/12", new Vector2(333f, -18f), new Vector2(140f, 38f), new Vector2(0f, 1f), DebugAdvanceRuneLighting);
                }
                if (runeStoneButton == null)
                {
                    runeStoneButton = YachtHudFactory.CreateButton(canvasObject.transform, "RuneStoneDebug", "Stones: 0/4", new Vector2(483f, -18f), new Vector2(150f, 38f), new Vector2(0f, 1f), DebugCycleRuneStones);
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
            TabletopSurfaceBuilder.Regenerate(layoutRoot, yachtTrayMesh, CenterSectionX, TrayVisualY, TrayScale);
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

            YachtHudFactory.CreateButton(canvasObject.transform, "Debug", "960 / 640", new Vector2(18f, -18f), new Vector2(130f, 38f), new Vector2(0f, 1f), ToggleResolution);
            
            runeFxButton = YachtHudFactory.CreateButton(canvasObject.transform, "RuneFxDebug", "Runes: 0/12", new Vector2(333f, -18f), new Vector2(140f, 38f), new Vector2(0f, 1f), DebugAdvanceRuneLighting);
            runeStoneButton = YachtHudFactory.CreateButton(canvasObject.transform, "RuneStoneDebug", "Stones: 0/4", new Vector2(483f, -18f), new Vector2(150f, 38f), new Vector2(0f, 1f), DebugCycleRuneStones);
keyLightToggleButton = YachtHudFactory.CreateButton(canvasObject.transform, "KeyLightToggle", $"Light: {KeyLightPresetName}", new Vector2(158f, -18f), new Vector2(165f, 38f), new Vector2(0f, 1f), ToggleKeyLightPreset);

            statusText = YachtHudFactory.CreateText(canvasObject.transform, "Status", "", new Vector2(0f, -20f), new Vector2(600f, 30f), new Vector2(0.5f, 1f), 15, TextAnchor.MiddleCenter);
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

        private void OnDestroy()
        {
            if (parchmentScoreSheet != null)
            {
                parchmentScoreSheet.ScoreSelected -= OnScoreSelected;
            }
            if (hourglassTimer != null)
            {
                turnDelay.Started -= OnTurnTimerStarted;
                turnDelay.Ticked -= OnTurnTimerTick;
                turnDelay.Expired -= OnTurnTimerExpired;
            }
            cameraRig?.Dispose();
            dicePool?.Dispose();
        }
    }
}
