using UnityEngine;
using UnityEngine.UI;
using Tessera.Core;
using Tessera.Dice;
using Tessera.Games.Yacht;
using Tessera.Rendering;
using Tessera.Tabletop;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 증강 요트 화면의 구성 루트(M10).
    ///
    /// 직접 하는 일은 셋뿐이다. 씬 참조를 잇고, 역할별 컴포넌트를 붙여 서로 소개하고,
    /// 입력 사건을 알맞은 컴포넌트로 넘긴다. 턴 흐름은 <see cref="YachtTurnFlowPresenter"/>,
    /// 주사위 화면은 <see cref="YachtDiceRoundPresenter"/>, 씬·HUD 구성은
    /// <see cref="YachtSceneAssembler"/>, 카메라·조명·오디오·증강 트레이는 각자의 컴포넌트가 맡는다.
    /// </summary>
    public sealed class AugmentedYachtController : MonoBehaviour
    {
        [Header("Source assets")]
        [SerializeField] private GameObject diceModel;
        // 몸체 형상이 다른 특수 주사위(M7-T5). Tessera/Bake/Dice Shapes 로 구운 프리팹이다.
        [SerializeField] private GameObject octahedronDieModel;
        [SerializeField] private GameObject sevensDieModel;
        [SerializeField] private Mesh yachtTrayMesh;
        [SerializeField] private Texture2D playmatTexture;

        [Header("Rendering")]
        [SerializeField] private Shader upscaleShader;

        [Header("Game Settings")]
        [SerializeField, Min(1)] private int diceCount = 5;
        [SerializeField] private DieType selectedDieType = DieType.Normal;
        [SerializeField] private YachtGameMode launchMode = YachtGameMode.Normal;

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

        private DicePresetCatalog presetCatalog;
        private BakedDiceController bakedDiceController;
        private YachtAudioService audioService;
        private YachtInputRouter inputRouter;
        private YachtCameraRig cameraRig;
        private YachtLightingRig lightingRig;

        /// <summary>
        /// 테이블·러너·소품의 Lit 재질을 셀로 바꾸고 되돌린다(M10.8-T4).
        /// 주사위는 <see cref="dicePool"/>이, 렌더 타깃은 <see cref="cameraRig"/>가 따로 관리한다.
        /// </summary>
        private readonly CelStyleSwitcher celStyleSwitcher = new();
        private RenderStyle renderStyle = RenderStyle.Baseline;

        /// <summary>셀 전환이 실제로 몇 개의 렌더러를 바꿨는지. 검증 도구가 전환 여부를 확인할 때 읽는다.</summary>
        public int CelConvertedRendererCount => celStyleSwitcher.ConvertedRendererCount;
        private AugmentTrayPresenter augmentTray;
        private DiceVisualPool dicePool;
        private YachtDiceRoundPresenter diceRound;
        private YachtTurnFlowPresenter turnFlow;
        private YachtRunicPresenter runicPresenter;

        private readonly YachtSceneAssembler.SceneRefs sceneRefs = new();
        private YachtSceneAssembler.HudRefs hud;
        private YachtSceneAssembler.DebugButtons debugButtons;

        private const float TableWidth = 15.6f;
        private const float LeftSectionWidth = TableWidth * 0.25f;
        private const float CenterSectionWidth = TableWidth * 0.45f;
        private const float RightSectionWidth = TableWidth * 0.3f;
        private const float CenterSectionX = -TableWidth * 0.5f + LeftSectionWidth + CenterSectionWidth * 0.5f;
        private const int DecorationLayer = 11;

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
        public YachtGameSession GameSession => turnFlow?.Session;
        public YachtGameMode GameMode => turnFlow != null ? turnFlow.GameMode : launchMode;
        public YachtTurnFlowPresenter TurnFlow => turnFlow;

        /// <summary>
        /// 주사위가 굴러 멈춘 뒤인가. 에디터 물리 검증 도구가 쓴다.
        /// 턴 전환 연출 중에는 false다. 게임 흐름이 아니라 물리 안정화만 판정한다.
        /// </summary>
        public bool IsSettled => turnFlow != null && turnFlow.Phase == PresentationPhase.Settled;
        public int KeptDieCount => diceRound != null ? diceRound.KeptCount : 0;

        public int GetDieValue(int index)
        {
            return diceRound != null ? diceRound.GetValue(index) : 0;
        }

        private void Awake()
        {
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;

            if (diceModel == null)
            {
#if UNITY_EDITOR
                diceModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Reference/normal_dice.fbx");
#endif
            }
#if UNITY_EDITOR
            if (octahedronDieModel == null)
            {
                octahedronDieModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dice/Die_Octahedron.prefab");
            }
            if (sevensDieModel == null)
            {
                sevensDieModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dice/Die_Sevens.prefab");
            }
#endif

            EnsureDicePool();

            // 배치는 씬이 소유한다(M9). 여기서는 바인딩만 하고, 씬이 비어 있을 때만 최소 월드를 세운다.
            if (!ResolveSceneLayout())
            {
                Debug.LogWarning(
                    "[AugmentedYachtController] 씬에서 레이아웃을 찾지 못해 카메라와 프레젠테이션만 생성합니다. " +
                    "테이블 프롭은 Assets/Prefabs/Tabletop 의 프리팹을 씬에 배치해야 합니다.");
                YachtSceneAssembler.BuildWorld(sceneRefs, transform, CenterSectionX);
                ConfigureLighting();
                EnsureCameraRig();
                debugButtons = YachtSceneAssembler.BuildPresentation(sceneRefs, transform, cameraRig, CreateHudActions());
                EnsureCameraRig();
                cameraRig.CreateRenderTarget();
                debugButtons = YachtSceneAssembler.BindPresentationActions(CreateHudActions());
                EnsureRunicPresenter();
                RefreshScoreSheetStructure();
            }
            else
            {
                YachtSceneAssembler.EnsureEventSystem();
                debugButtons = YachtSceneAssembler.BindPresentationActions(CreateHudActions());
                EnsureRunicPresenter();
                EnsureCameraRig();
                cameraRig.CreateRenderTarget();
            }

            cameraRig?.ApplyRenderSettings();
            ConfigureLighting();
            sceneRefs.WorldCamera = YachtSceneAssembler.EnsureSingleAudioListener(sceneRefs.WorldCamera);
            EnsureAudioService();
            InitializePresetCatalog();
            InitializeBakedController();
            EnsureDicePool();
            EnsureDiceRound();
            diceRound.EnsureDiceState();
            EnsureInputRouter();

            BindTabletopProps();

            WarmUpRollAssets();
            EnsureRunicPresenter();
            InitializeYachtGame();
        }

        private void OnValidate()
        {
            if (Application.isPlaying && diceRound != null && diceRound.VisualCount > 0)
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

        private void Start()
        {
            StartCoroutine(audioService.LoadClipsAsync());
        }

        private void Update()
        {
            // 입력은 YachtInputRouter가 읽어 사건으로 알린다(M10-T1).
            turnFlow?.UpdateTimerTextPosition();
            cameraRig?.FitFullScreen();
            cameraRig?.SyncCrispUiTargetToScreen();
        }

        /// <summary>씬에 이미 배치된 레이아웃을 찾아 참조를 잇는다.</summary>
        private bool ResolveSceneLayout()
        {
            if (!YachtSceneAssembler.ResolveExistingLayout(sceneRefs)) return false;

            sceneRefs.WorldCamera = YachtSceneAssembler.EnsureSingleAudioListener(sceneRefs.WorldCamera);
            BindTabletopProps();
            YachtSceneAssembler.ApplyWorldCameraFraming(sceneRefs.WorldCamera, CenterSectionX);
            EnsureCameraRig();
            cameraRig.CreateRenderTarget();
            cameraRig.ApplyRenderSettings();
            return true;
        }

        /// <summary>턴 흐름을 시작 대기 상태로 세운다. 화면 UI를 먼저 만들고 흐름에 넘긴다.</summary>
        private void InitializeYachtGame()
        {
            if (!Application.isPlaying || parchmentScoreSheet == null) return;

            hud = YachtSceneAssembler.BuildGameFlowUi(CreateHudActions());
            EnsureAugmentTray();
            EnsureCameraRig();
            if (hud != null)
            {
                augmentTray.BuildUi(hud.Canvas);
            }

            EnsureTurnFlow();
            turnFlow.BindHud(sceneRefs.StatusText, hud?.TimerText, hud?.StartOverlay, hud?.ResultOverlay, hud?.ResultText);
            turnFlow.Initialize();
        }

        private YachtSceneAssembler.HudActions CreateHudActions()
        {
            return new YachtSceneAssembler.HudActions
            {
                ToggleResolution = ToggleResolution,
                ToggleKeyLight = ToggleKeyLightPreset,
                AdvanceRuneLighting = () => runicPresenter?.AdvanceDebugRuneLighting(),
                CycleRuneStones = () => runicPresenter?.CycleDebugRuneStones(),
                StartNormalGame = () => turnFlow?.StartNewGame(YachtGameMode.Normal),
                StartAugmentedGame = () => turnFlow?.StartNewGame(YachtGameMode.Augmented),
                RestartGame = () => turnFlow?.StartNewGame(),
                TogglePixelEdge = TogglePixelEdgeFilter,
                CycleQuantize = CyclePixelQuantizeMode,
                ToggleRenderStyle = ToggleRenderStyle,
                ResolutionPresetLabel = () => $"{PixelFilterSettings.ResolutionA.x} / {PixelFilterSettings.ResolutionB.x}",
                KeyLightPresetName = () => KeyLightPresetName,
                PixelEdgeEnabled = () => cameraRig != null && cameraRig.EdgeFilterEnabled,
                QuantizeModeName = () => cameraRig != null ? cameraRig.QuantizeModeName : "Off",
                RenderStyleName = () => cameraRig != null ? cameraRig.RenderStyleName : "Baseline"
            };
        }

        /// <summary>턴 흐름 프레젠터를 붙이고 참조를 맞춘다(M10-T6).</summary>
        private void EnsureTurnFlow()
        {
            EnsureDiceRound();
            EnsureAugmentTray();

            if (turnFlow == null)
            {
                turnFlow = GetComponent<YachtTurnFlowPresenter>() ?? gameObject.AddComponent<YachtTurnFlowPresenter>();
                turnFlow.ModeStarted += OnModeStarted;
                turnFlow.TrayRebindRequested += EnsureAugmentTray;
            }

            turnFlow.BindProps(parchmentScoreSheet, augmentTray, diceRound, rollOrb, rollCosmicCube, rerollCounterBar,
                runicSlateMatrix, turnBalanceIndicator, hourglassTimer,
                hourglassTimer != null ? hourglassTimer.transform : null, sceneRefs.WorldCamera);
            turnFlow.BindRules(diceCount, presetCatalog != null ? presetCatalog.NormalFiveDiceClipCount : 20, launchMode);
        }

        /// <summary>모드가 시작됐다. 트레이 표시와 주사위 색상은 씬 쪽 표현이라 여기서 맞춘다.</summary>
        private void OnModeStarted(YachtGameMode mode)
        {
            launchMode = mode;
            // 일반 모드에서도 왼쪽 증강 트레이 그래픽은 사용자의 현재 아트 기준에 따라 임시 유지한다.
            // 드래프트, 특수 주사위, 증강 명령은 Normal 규칙 세트에서 생성되지 않는다.
            if (augmentCardTray != null) augmentCardTray.gameObject.SetActive(true);
            if (mode == YachtGameMode.Normal) SetDieType(DieType.Normal);
        }

        /// <summary>
        /// 증강 카드 프레젠터를 붙이고 참조를 맞춘다(M10-T7).
        ///
        /// 트레이와 카메라는 씬 해석 순서에 따라 늦게 채워지므로 호출할 때마다 다시 넘긴다.
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

            augmentTray.Bind(augmentCardTray, sceneRefs.WorldCamera);
        }

        /// <summary>주사위 화면 프레젠터를 붙인다(M10-T8).</summary>
        private void EnsureDiceRound()
        {
            EnsureDicePool();
            if (diceRound == null)
            {
                diceRound = GetComponent<YachtDiceRoundPresenter>() ?? gameObject.AddComponent<YachtDiceRoundPresenter>();
            }

            diceRound.Bind(dicePool, bakedDiceController, presetCatalog, diceCount);
        }

        /// <summary>주사위 비주얼 풀을 붙인다(M10-T5).</summary>
        private void EnsureDicePool()
        {
            if (dicePool == null)
            {
                dicePool = GetComponent<DiceVisualPool>() ?? gameObject.AddComponent<DiceVisualPool>();
            }

            dicePool.Bind(diceModel, sceneRefs.LayoutRoot, CenterSectionX, selectedDieType);
            dicePool.BindSpecialModels(octahedronDieModel, sevensDieModel);
        }

        /// <summary>
        /// 입력 라우터를 붙이고 사건을 기존 처리에 연결한다(M10-T1).
        /// 라우터는 무엇을 가리키고 눌렀는지만 알리고, 무엇을 할지는 여기서 정한다.
        /// </summary>
        private void EnsureInputRouter()
        {
            if (inputRouter != null) return;

            inputRouter = GetComponent<YachtInputRouter>() ?? gameObject.AddComponent<YachtInputRouter>();
            inputRouter.WorldCamera = sceneRefs.WorldCamera;
            // 이 게이트는 주사위뿐 아니라 굴림 오브젝트 클릭과 장식물 피드백까지 함께 막는다.
            // 숨긴 주사위는 GameObject가 꺼져 콜라이더도 없으므로 여기서 다시 거를 필요가 없다.
            inputRouter.DicePointerEnabled = () => diceRound != null && diceRound.VisualCount > 0;
            // 선택 카드도 월드의 3D 두루마리라 드래프트 중에도 포인터가 살아 있어야 한다.
            inputRouter.AugmentPointerEnabled = () => turnFlow?.Session != null
                && turnFlow.Session.Mode == YachtGameMode.Augmented;

            inputRouter.RollRequested += RollDice;
            inputRouter.ResolutionPresetRequested += OnResolutionPresetRequested;
            inputRouter.PixelEdgeToggleRequested += TogglePixelEdgeFilter;
            inputRouter.PixelQuantizeCycleRequested += CyclePixelQuantizeMode;
            inputRouter.RenderStyleToggleRequested += ToggleRenderStyle;
            inputRouter.DieTypeRequested += SetDieType;
            inputRouter.DieHoverChanged += OnDieHoverChanged;
            inputRouter.DieClicked += ToggleKeep;
            inputRouter.RollTriggerHoverChanged += OnRollTriggerHoverChanged;
            inputRouter.RollTriggerClicked += OnRollTriggerClicked;
            inputRouter.AugmentCardHoverChanged += OnAugmentCardHoverChanged;
            inputRouter.AugmentCardClicked += OnAugmentCardClicked;
        }

        /// <summary>렌더 파이프라인 리그를 붙이고 씬 구성 요소를 넘긴다(M10-T2).</summary>
        private void EnsureCameraRig()
        {
            if (cameraRig == null)
            {
                cameraRig = GetComponent<YachtCameraRig>() ?? gameObject.AddComponent<YachtCameraRig>();
                cameraRig.CrispUiCameraReady += OnCrispUiCameraReady;
            }

            cameraRig.Bind(sceneRefs.WorldCamera, sceneRefs.PresentationCamera, sceneRefs.GameImage,
                sceneRefs.GameImageRect, sceneRefs.LayoutRoot, upscaleShader);
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

        /// <summary>조명 리그를 붙인다(M10-T3).</summary>
        private void EnsureLightingRig()
        {
            if (lightingRig != null) return;

            lightingRig = GetComponent<YachtLightingRig>() ?? gameObject.AddComponent<YachtLightingRig>();
            lightingRig.PresetChanged += OnKeyLightPresetChanged;
        }

        /// <summary>오디오 서비스를 붙인다(M10-T4).</summary>
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
            YachtSceneAssembler.SetKeyLightLabel(debugButtons, presetName);
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

        private void InitializePresetCatalog()
        {
            // 일반·혼합·판 뒤집기 프리셋의 인덱스를 먼저 읽고 실제 파일은 최초 사용 시 적재한다.
            presetCatalog = DicePresetCatalog.LoadAll();
            if (!presetCatalog.IsLoaded) presetCatalog = DicePresetCatalog.LoadNormalFiveDice();

            // 기본 5개 프리셋 파일만 미리 적재한다. 파일 하나가 약 230KB이고 파싱이 동기라,
            // 첫 굴림에서 읽으면 그 프레임이 통째로 멈춘다. 나머지 파일은 증강 구성에 따라
            // 쓰일지 알 수 없으므로 최초 사용 시점에 맡긴다.
            //
            // 이 적재는 아래 로그가 NormalFiveDiceClipCount를 읽는 부수 효과로도 일어난다.
            // 로그를 지우면 조용히 사라지는 의존이므로 별도 문장으로 드러내 둔다.
            int warmedClipCount = presetCatalog.NormalFiveDiceClipCount;
            Debug.Log($"Preset Catalog loaded: {warmedClipCount} clips available.");
        }

        private static void WarmUpRollAssets()
        {
            // 별자리 텍스처 연출은 폐기됐으므로 게임에서는 적재하지 않는다(ZodiacConstellationData.EnabledInGame).
            // 다시 켜는 경우에만 미리 굽는다. 저장된 씬의 기존 롤 오브젝트는 지오메트리를 재생성하지 않아
            // 별자리 캐시가 비어 있을 수 있고, 입력 처리 전에 베이킹을 끝내야
            // 첫 RollDice 호출이 프레임을 점유하지 않는다.
            if (!ZodiacConstellationData.EnabledInGame) return;

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

        public void RollDice() => turnFlow?.RollDice();

        public void ResetAndRollDice() => turnFlow?.ResetAndRollDice();

        public void UseTableFlip() => turnFlow?.UseTableFlip();

        public void UseAugmentAction(string augmentId) => turnFlow?.UseAugmentAction(augmentId);

        private void SelectDraftOption(int optionIndex) => turnFlow?.SelectDraftOption(optionIndex);

        public bool SetDieKept(int index, bool kept) => turnFlow != null && turnFlow.SetDieKept(index, kept);

        public void ToggleKeep(int dieIndex) => turnFlow?.ToggleKeep(dieIndex);

        /// <summary>주사위 색상 팔레트를 바꾼다. 입력 라우터와 모드 전환이 호출한다.</summary>
        public void SetDieType(DieType type)
        {
            selectedDieType = type;
            EnsureDicePool();
            diceRound?.SetDieType(type);
        }

        private void OnResolutionPresetRequested(int presetIndex)
        {
            SetResolution(presetIndex == 0 ? PixelFilterSettings.ResolutionA : PixelFilterSettings.ResolutionB);
        }

        private void OnDieHoverChanged(int dieIndex)
        {
            if (diceRound != null) diceRound.HoveredIndex = dieIndex;
            turnFlow?.UpdateStatusText();
        }

        private void OnRollTriggerHoverChanged(bool hovered)
        {
            if (rollCosmicCube != null) rollCosmicCube.SetHovered(hovered);
            if (rollOrb != null) rollOrb.SetHovered(hovered);
        }

        private void OnRollTriggerClicked()
        {
            if (turnFlow == null || !turnFlow.CanInitiateRoll()) return;

            if (rollCosmicCube != null) rollCosmicCube.TriggerClickFeedback();
            if (rollOrb != null) rollOrb.TriggerClickFeedback();

            turnFlow.RollDice();
        }

        private void OnAugmentCardHoverChanged(AugmentTrayCardView card)
        {
            augmentTray.SetHoveredCard(card);
        }

        private void OnAugmentCardClicked(AugmentTrayCardView card)
        {
            augmentTray.ToggleSelection(card);
        }

        public void ToggleResolution()
        {
            EnsureCameraRig();
            SetResolution(PixelFilterSettings.NextPreset(cameraRig.InternalResolution));
        }

        private void SetResolution(Vector2Int resolution)
        {
            EnsureCameraRig();
            cameraRig.SetInternalResolution(resolution);
        }

        /// <summary>픽셀 엣지 필터를 켜고 끈다(F3). 끈 화면이 엣지 도입 이전의 픽셀 필터다.</summary>
        public void TogglePixelEdgeFilter()
        {
            EnsureCameraRig();
            cameraRig.ToggleEdgeFilter();
            YachtSceneAssembler.SetPixelEdgeLabel(debugButtons, cameraRig.EdgeFilterEnabled);
        }

        /// <summary>색 양자화 모드를 끔 → 단계 → 팔레트 순으로 돌린다(Q).</summary>
        public void CyclePixelQuantizeMode()
        {
            EnsureCameraRig();
            cameraRig.CycleQuantizeMode();
            YachtSceneAssembler.SetQuantizeLabel(debugButtons, cameraRig.QuantizeModeName);
        }

        /// <summary>
        /// Baseline과 Cel 연출 방식을 왕복한다(V, M10.8).
        ///
        /// 재료·조명·SSAO·렌더 타깃이 한 번에 따라간다. 기본값이 Baseline이라 아무것도 채택하지
        /// 않은 상태가 M10.7까지의 화면과 같다.
        /// </summary>
        public void ToggleRenderStyle()
        {
            SetRenderStyle(renderStyle == RenderStyle.Cel ? RenderStyle.Baseline : RenderStyle.Cel);
        }

        private void SetRenderStyle(RenderStyle style)
        {
            renderStyle = style;

            EnsureCameraRig();
            cameraRig.SetRenderStyle(style);
            lightingRig?.SetRenderStyle(style);
            dicePool?.SetRenderStyle(style);

            // 주사위는 풀이, Crisp UI는 별도 카메라가 담당하므로 두 레이어는 제외한다.
            int excluded = TesseraLayers.Mask(TesseraLayers.Dice) | TesseraLayers.Mask(TesseraLayers.CrispUI);
            celStyleSwitcher.Apply(sceneRefs.LayoutRoot, style, excluded);

            YachtSceneAssembler.SetRenderStyleLabel(debugButtons, cameraRig.RenderStyleName);
        }

        /// <summary>
        /// 씬에 배치된 테이블 프롭을 컨트롤러 참조에 연결한다.
        /// 생성하지 않는다. 누락된 프롭은 경고로만 알리고, 프리팹을 씬에 배치해 해결한다.
        /// </summary>
        private void BindTabletopProps()
        {
            parchmentScoreSheet = BindProp(parchmentScoreSheet);
            augmentCardTray = BindProp(augmentCardTray);
            rollOrb = BindProp(rollOrb, warnIfMissing: false);
            rollCosmicCube = BindProp(rollCosmicCube);
            rerollCounterBar = BindProp(rerollCounterBar);
            hourglassTimer = BindProp(hourglassTimer);
            candleStand = BindProp(candleStand);
            runicSlateMatrix = BindProp(runicSlateMatrix);
            trinketCluster = BindProp(trinketCluster);
            turnBalanceIndicator = BindProp(turnBalanceIndicator);

            RefreshScoreSheetStructure();
            if (rerollCounterBar != null) rerollCounterBar.SetRollsRemaining(3, 3);
        }

        private T BindProp<T>(T current, bool warnIfMissing = true) where T : Component
        {
            T resolved = current != null ? current : FindFirstObjectByType<T>();
            if (resolved == null && warnIfMissing)
            {
                Debug.LogWarning($"[AugmentedYachtController] 씬에 {typeof(T).Name} 프롭이 없습니다. Assets/Prefabs/Tabletop 에서 배치하십시오.");
            }
            return resolved;
        }

        private void RefreshScoreSheetStructure()
        {
            if (parchmentScoreSheet == null) parchmentScoreSheet = FindFirstObjectByType<ParchmentScoreSheet>();
            if (parchmentScoreSheet == null) return;

            parchmentScoreSheet.EnsureStructure();
            parchmentScoreSheet.RefreshAllScores();
        }

        // 자동 호출하지 않는다. 트레이 메시의 UV와 펠트 텍스처는 M9에서 에셋으로 구웠고,
        // 이 메서드는 런타임 생성 텍스처를 머티리얼에 덮어써 구워둔 참조를 지운다.
        // 형상을 다시 만들어야 할 때만 수동으로 실행한 뒤 프리팹을 다시 굽는다.
        [ContextMenu("Regenerate Tray Visual Material (bake 전용)")]
        public void SyncTrayVisualMat()
        {
            TabletopSurfaceBuilder.SyncTrayMaterial();
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

            sceneRefs.LayoutRoot = YachtSceneAssembler.EnsureLayoutRoot(sceneRefs.LayoutRoot, transform);
            TabletopSurfaceBuilder.Regenerate(sceneRefs.LayoutRoot, yachtTrayMesh, CenterSectionX, DiceBoardMetrics.TrayVisualY, DiceBoardMetrics.TrayScale);
        }

        /// <summary>룬 슬레이트 창구를 붙인다(M10-T8).</summary>
        private void EnsureRunicPresenter()
        {
            if (runicPresenter == null)
            {
                runicPresenter = GetComponent<YachtRunicPresenter>() ?? gameObject.AddComponent<YachtRunicPresenter>();
            }

            runicPresenter.Bind(runicSlateMatrix, parchmentScoreSheet, debugButtons);
        }

        private void OnDestroy()
        {
            if (turnFlow != null)
            {
                turnFlow.ModeStarted -= OnModeStarted;
                turnFlow.TrayRebindRequested -= EnsureAugmentTray;
            }
            cameraRig?.Dispose();
            dicePool?.Dispose();
            celStyleSwitcher.Dispose();
        }
    }
}
