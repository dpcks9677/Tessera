using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tessera.Games.Yacht;
using Tessera.Tabletop;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 턴 흐름과 화면 단계를 소유한다(M10-T6).
    ///
    /// 권위 계층(<see cref="YachtGameSession"/>)에 명령을 넣고, 그 결과를 화면 단계
    /// <see cref="PresentationPhase"/>와 상태 문구·타이머·점수표·증강 트레이에 반영한다.
    /// 주사위 애니메이션은 <see cref="YachtDiceRoundPresenter"/>가, 씬 구성은 컨트롤러가 맡는다.
    ///
    /// 턴 지연은 <see cref="ITurnDelaySource"/>로만 다룬다. 모래시계 프롭을 빼거나 다른 연출로
    /// 바꿔도 턴 흐름은 그대로 돈다.
    /// </summary>
    public sealed class YachtTurnFlowPresenter : MonoBehaviour
    {
        private ParchmentScoreSheet scoreSheet;

        /// <summary>증강 아이콘이다. 스티커 위에 올리며 Resources 조회를 매번 하지 않도록 담아 둔다.</summary>
        private readonly Dictionary<string, Sprite> stickerIcons = new();

        private readonly List<AugmentStickerPlacement> stickerBuffer = new();
        private readonly List<AugmentVfxRequest> vfxBuffer = new();
        /// <summary>
        /// 지금 붙어 있는 스티커다. 칸마다 어느 증강인지까지 들고 있어야 교체를 알아본다.
        /// 점수표가 플레이어별 Categories 열을 가지므로 두 사람 것을 따로 센다.
        /// </summary>
        private readonly Dictionary<ScoreCategory, string>[] shownStickers = { new(), new() };
        private readonly Dictionary<ScoreCategory, string> nextStickers = new();
        private readonly List<ScoreCategory> attachedStickers = new();
        private readonly List<ScoreCategory> removedStickers = new();

        /// <summary>이미 연출로 소비한 명령의 리비전이다. 같은 이벤트를 두 번 재생하지 않는다.</summary>
        private long lastVfxRevision = -1;
        private AugmentTrayPresenter augmentTray;
        private YachtDiceRoundPresenter dice;
        private RollOrb rollOrb;
        private RollCosmicCube rollCosmicCube;
        private RerollCounterBar rerollCounterBar;
        private RunicSlateMatrix runicSlateMatrix;
        private TurnBalanceIndicator turnBalanceIndicator;
        private ITurnDelaySource turnDelay;
        private Transform timerAnchor;
        private Camera worldCamera;

        private Text statusText;
        private Text timerText;
        private Text resultText;
        private GameObject startGameOverlay;
        private GameObject gameResultOverlay;

        private int diceCount = 5;
        private int presetClipCount = 20;
        private YachtGameMode launchMode = YachtGameMode.Normal;

        private YachtGameSession gameSession;
        private YachtGameCommandResult pendingRollResult;
        private string pendingTurnTransitionMessage;
        private Coroutine rollRoutine;
        private Coroutine smokeRoutine;

        private const float TurnDurationSeconds = YachtGameOptions.DefaultTurnDurationSeconds;

        // 56 dice-alchemy 연기 가림 타임라인. 값은 사양서 §9.4의 사용자 확정치다.
        private const float SmokeDigitsHideSeconds = 0.15f;
        private const float SmokeSwapSeconds = 0.25f;
        private const float SmokeDigitsShowSeconds = 0.28f;
        private const float SmokeTotalSeconds = 0.60f;

        /// <summary>모드가 시작됐다. 트레이 표시와 주사위 색상처럼 씬 쪽 표현은 컨트롤러가 맡는다.</summary>
        public event Action<YachtGameMode> ModeStarted;

        /// <summary>
        /// 증강 트레이를 갱신하기 직전에 참조를 다시 맞춰 달라고 알린다.
        /// 트레이와 카메라는 씬 해석 순서에 따라 늦게 채워지므로, 한 번 묶은 참조를 계속 쓰면
        /// 첫 갱신 시점의 null을 그대로 부여잡는다.
        /// </summary>
        public event Action TrayRebindRequested;

        public PresentationPhase Phase { get; private set; } = PresentationPhase.Idle;
        public YachtGameSession Session => gameSession;
        public YachtGameMode GameMode => gameSession?.Mode ?? launchMode;

        public void BindProps(
            ParchmentScoreSheet sheet,
            AugmentTrayPresenter tray,
            YachtDiceRoundPresenter diceRound,
            RollOrb orb,
            RollCosmicCube cube,
            RerollCounterBar reroll,
            RunicSlateMatrix runes,
            TurnBalanceIndicator turnBalance,
            ITurnDelaySource delaySource,
            Transform timerWorldAnchor,
            Camera camera)
        {
            scoreSheet = sheet;
            rollOrb = orb;
            rollCosmicCube = cube;
            rerollCounterBar = reroll;
            runicSlateMatrix = runes;
            turnBalanceIndicator = turnBalance;
            turnDelay = delaySource;
            timerAnchor = timerWorldAnchor;
            worldCamera = camera;

            if (augmentTray != tray)
            {
                augmentTray = tray;
            }
            if (dice != diceRound)
            {
                if (dice != null)
                {
                    dice.ArrangeStarted -= OnArrangeStarted;
                    dice.ArrangeCompleted -= OnArrangeCompleted;
                }
                dice = diceRound;
                if (dice != null)
                {
                    dice.ArrangeStarted += OnArrangeStarted;
                    dice.ArrangeCompleted += OnArrangeCompleted;
                }
            }
        }

        public void BindHud(Text status, Text timer, GameObject startOverlay, GameObject resultOverlay, Text result)
        {
            statusText = status;
            timerText = timer;
            startGameOverlay = startOverlay;
            gameResultOverlay = resultOverlay;
            resultText = result;
        }

        public void BindRules(int count, int presetClips, YachtGameMode mode)
        {
            diceCount = count;
            presetClipCount = presetClips;
            launchMode = mode;
        }

        /// <summary>게임을 시작할 수 있는 대기 상태로 만든다.</summary>
        public void Initialize()
        {
            if (scoreSheet == null) return;

            scoreSheet.EnsureStructure();
            scoreSheet.ScoreSelected -= OnScoreSelected;
            scoreSheet.ScoreSelected += OnScoreSelected;

            gameSession = CreateGameSession(launchMode);
            scoreSheet.SetActivePlayer(-1, false);
            turnBalanceIndicator?.SetActiveSide(TurnSide.None, false);

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
            augmentTray?.EnsureOwnedCardViews();
            Phase = PresentationPhase.Idle;
            ResetDiceForTurn();
            dice?.SetVisible(false);
            SetTimerTextIdle();
            SetRollInteraction(false);
            UpdateStatusText("게임 시작 버튼을 눌러 주세요.");
        }

        public void StartNewGame()
        {
            StartNewGame(launchMode);
        }

        public void StartNewGame(YachtGameMode mode)
        {
            if (scoreSheet == null) return;

            if (rollRoutine != null)
            {
                StopCoroutine(rollRoutine);
                rollRoutine = null;
            }
            if (smokeRoutine != null)
            {
                StopCoroutine(smokeRoutine);
                smokeRoutine = null;
                dice?.SetUnkeptCrispRenderersVisible(true);
                dice?.ApplyValuesToVisuals();
            }
            dice?.StopAnimations();

            launchMode = mode;
            gameSession = CreateGameSession(mode);
            turnDelay?.SetIdle(TurnDurationSeconds);
            gameSession.StartNewGame();
            ModeStarted?.Invoke(mode);
            turnBalanceIndicator?.SetActiveSide(TurnSide.Left, false);
            scoreSheet.RefreshAllScores();
            scoreSheet.SetActivePlayer(gameSession.CurrentPlayerIndex, false);
            startGameOverlay?.SetActive(false);
            gameResultOverlay?.SetActive(false);
            runicSlateMatrix?.SetRoundProgress(gameSession.CurrentRound);
            ResetDiceForTurn();
            dice?.SetVisible(false);
            string augmentMessage = GetAugmentEventMessage(gameSession.LastCommandResult);
            RefreshAugmentPresentation(augmentMessage);
            if (gameSession.IsDrafting)
            {
                Phase = PresentationPhase.Idle;
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
            var options = new YachtGameOptions
            {
                Mode = mode,
                DiceCount = diceCount,
                PresetClipCount = Mathf.Max(1, presetClipCount),
                TurnDurationSeconds = TurnDurationSeconds
            };
            var session = new YachtGameSession(options);
            scoreSheet?.BindPlayers(session.State.Players);
            return session;
        }

        public void SelectDraftOption(int optionIndex)
        {
            if (gameSession == null || !gameSession.IsDrafting) return;
            IReadOnlyList<string> options = gameSession.State.Draft.Options;
            if (optionIndex < 0 || optionIndex >= options.Count) return;

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
                // 드래프트 중에는 활성 플레이어를 넘기지 않는다(-1). D-040의 열 확장은 실제 턴에만 적용한다.
                scoreSheet?.SetActivePlayer(-1, false);
                UpdateStatusText(message);
                return;
            }

            ResetDiceForTurn();
            scoreSheet?.SetActivePlayer(gameSession.CurrentPlayerIndex, false);
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
                Phase = PresentationPhase.Idle;
                turnDelay?.SetIdle(TurnDurationSeconds);
                SetTimerTextIdle();
                SetRollInteraction(false);
                RefreshAugmentPresentation();
                return;
            }
            Phase = PresentationPhase.TurnTransition;
            scoreSheet?.SetActivePlayer(gameSession.CurrentPlayerIndex, false);
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
                    Phase = PresentationPhase.Idle;
                    turnDelay?.Stop(false);
                    SetTimerTextIdle();
                    SetRollInteraction(false);
                    // 드래프트 중에는 활성 플레이어를 넘기지 않는다(-1). D-040의 열 확장은 실제 턴에만 적용한다.
                    scoreSheet?.SetActivePlayer(-1, false);
                    RefreshAugmentPresentation(transitionMessage);
                    UpdateStatusText(transitionMessage);
                    return;
                }
                ResetDiceForTurn();
                dice?.SetVisible(false);
                rerollCounterBar?.SetRollsRemaining(YachtGameSession.MaxRolls, YachtGameSession.MaxRolls);
                runicSlateMatrix?.SetRoundProgress(gameSession.CurrentRound);
                scoreSheet?.SetActivePlayer(gameSession.CurrentPlayerIndex, false);
                turnBalanceIndicator?.SetActiveSide(MapPlayerToTurnSide(gameSession.CurrentPlayerIndex), true);
            }

            if (advancedTurn && turnDelay != null)
            {
                float turnDuration = gameSession.CurrentTurnDurationSeconds;
                turnDelay.Reset(turnDuration);
                turnDelay.Resume();
                SetTimerText(turnDuration);
            }

            Phase = PresentationPhase.AwaitingRoll;
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
            scoreSheet.RefreshAllScores();
            string augmentMessage = GetAugmentEventMessage(gameSession.LastCommandResult);
            HandleTurnCompleted(result, augmentMessage ?? "시간 초과로 점수가 자동 확정되었습니다.");
        }

        private void OnScoreSelected(int playerIndex, ScoreCategory category)
        {
            CommitScore(playerIndex, category);
        }

        /// <summary>
        /// 지금 턴을 끝내고 다음 턴으로 넘긴다. 시간 초과와 같은 경로라
        /// 남은 족보 중 최고점이 자동으로 기입된다. 디버그 패널이 부른다.
        /// </summary>
        public void SkipTurn() => OnTurnTimerExpired();

        /// <summary>
        /// 점수를 확정하고 턴을 넘긴다. 점수표 클릭과 같은 경로이며,
        /// 클릭을 흉내 낼 수 없는 검증 도구가 직접 부른다.
        /// </summary>
        public void CommitScore(int playerIndex, ScoreCategory category)
        {
            if (gameSession == null || playerIndex != gameSession.CurrentPlayerIndex) return;
            if (!gameSession.TryCommitScore(category, out YachtTurnResult result)) return;

            scoreSheet.RefreshAllScores();
            string augmentMessage = GetAugmentEventMessage(gameSession.LastCommandResult);
            string scoreMessage = $"P{result.ScoredPlayerIndex + 1} 점수 {result.Score}점 확정";
            HandleTurnCompleted(result, string.IsNullOrEmpty(augmentMessage) ? scoreMessage : $"{scoreMessage} · {augmentMessage}");
        }

        private void HandleTurnCompleted(YachtTurnResult result, string message)
        {
            turnDelay?.Stop(false);
            scoreSheet.ClearCandidateScores();
            SetRollInteraction(false);
            // 사라지는 연출이 아직 없으므로 기입과 동시에 주사위를 치운다.
            dice?.SetVisible(false);

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
            Phase = PresentationPhase.TurnTransition;
            SetRollInteraction(false);
            SetTimerText(TurnDurationSeconds);

            if (turnDelay != null) turnDelay.Begin(TurnDurationSeconds, true);
            else OnTurnTimerStarted();
        }

        private void FinishGame()
        {
            Phase = PresentationPhase.Idle;
            turnDelay?.Stop();
            dice?.SetVisible(false);
            rerollCounterBar?.SetRollsRemaining(0, YachtGameSession.MaxRolls);
            scoreSheet?.SetActivePlayer(-1, false);
            turnBalanceIndicator?.SetActiveSide(TurnSide.None, true);
            SetRollInteraction(false);
            SetTimerTextIdle();

            int p1 = gameSession.GetPlayer(0).TotalScore;
            int p2 = gameSession.GetPlayer(1).TotalScore;
            string winner = p1 == p2 ? "무승부" : (p1 > p2 ? "P1 승리" : "P2 승리");
            if (resultText != null) resultText.text = $"{winner}\nP1  {p1}점   ·   P2  {p2}점";
            gameResultOverlay?.SetActive(true);
            RefreshAugmentPresentation();
            UpdateStatusText("게임이 종료되었습니다.");
        }

        private void ResetDiceForTurn()
        {
            dice?.ResetForTurn(gameSession?.State?.Dice);
        }

        private void RefreshGameInteraction()
        {
            if (gameSession == null)
            {
                SetRollInteraction(false);
                return;
            }

            bool canRoll = gameSession.CanRoll && Phase.IsInteractive() && !dice.AllKept;
            SetRollInteraction(canRoll);

            if (gameSession.Phase == YachtGamePhase.ScoreSelection && Phase != PresentationPhase.TurnTransition)
            {
                scoreSheet?.ShowCandidateScores(gameSession.CurrentPlayerIndex, gameSession.CurrentCandidates);
            }
            else if (gameSession.Phase == YachtGamePhase.Draft)
            {
                scoreSheet?.SetActivePlayer(gameSession.State.Draft.PlayerIndex, false);
            }
            else if (gameSession.Phase != YachtGamePhase.GameOver)
            {
                scoreSheet?.SetActivePlayer(gameSession.CurrentPlayerIndex, false);
            }
            RefreshAugmentPresentation();
        }

        /// <summary>지금 굴림을 시작해도 되는가. 굴림 트리거 프롭도 이 조건을 본다.</summary>
        public bool CanInitiateRoll()
        {
            if (gameSession == null || !gameSession.CanRoll || !Phase.IsInteractive()) return false;
            return !dice.AllKept;
        }

        public void RollDice()
        {
            if (!CanInitiateRoll())
            {
                if (gameSession != null && gameSession.RollsRemaining <= 0)
                    UpdateStatusText("이번 턴의 굴림 횟수를 모두 사용했습니다.");
                else if (dice.AllKept)
                    UpdateStatusText("모든 주사위가 킵되어 있습니다.");
                return;
            }

            if (!gameSession.TryRoll(out pendingRollResult)) return;
            dice.SyncFromAuthority(gameSession.State.Dice);
            turnDelay?.Pause();
            scoreSheet?.ClearCandidateScores();
            rerollCounterBar?.SetRollsRemaining(gameSession.RollsRemaining, YachtGameSession.MaxRolls);
            SetRollInteraction(false);
            rollRoutine = StartCoroutine(RunRollSequence());
        }

        public void ResetAndRollDice()
        {
            if (Phase == PresentationPhase.Rolling || Phase == PresentationPhase.Arranging) return;

            for (int i = 0; i < dice.DiceCount; i++)
            {
                if (dice.IsKept(i)) gameSession?.TrySetDieKept(i, false);
            }
            dice.ClearKeepMarks();
            RollDice();
        }

        public void UseTableFlip()
        {
            UseAugmentAction(YachtAugmentRuntime.TableFlipId);
        }

        public void UseAugmentAction(string augmentId)
        {
            if (gameSession == null || !Phase.IsInteractive()) return;
            if (!gameSession.TryUseAugmentAction(augmentId, out pendingRollResult))
            {
                UpdateStatusText(pendingRollResult?.ErrorMessage);
                RefreshAugmentPresentation(pendingRollResult?.ErrorMessage);
                return;
            }

            if (pendingRollResult.RollPresentation == null)
            {
                bool diceCountUnchanged = dice.VisualCount == gameSession.State.Dice.Count;
                if (!diceCountUnchanged)
                {
                    ResetDiceForTurn();
                    Phase = PresentationPhase.AwaitingRoll;
                }

                dice.SetVisible(true);
                scoreSheet?.ClearCandidateScores();

                // 56 dice-alchemy: 눈이 연기에 가려 바뀌는 사이 값 반영을 늦춘다. 대상 판정은
                // AugmentVfxPlanner가 테이블 주도로 한다(사양서 §3.4).
                if (diceCountUnchanged && HasDiceSmokeSwap(pendingRollResult))
                {
                    smokeRoutine = StartCoroutine(RunDiceSmokeSwapSequence(GetAugmentEventMessage(pendingRollResult)));
                    return;
                }

                if (diceCountUnchanged) dice.SyncFromAuthority(gameSession.State.Dice);
                if (gameSession.Phase == YachtGamePhase.ScoreSelection)
                    scoreSheet?.ShowCandidateScores(gameSession.CurrentPlayerIndex, gameSession.CurrentCandidates);
                string message = GetAugmentEventMessage(pendingRollResult);
                RefreshAugmentPresentation(message);
                UpdateStatusText(message);
                // 이 분기는 SetRollInteraction을 타지 않는다. 증강이 굴림 예산 조건을 바꿨을 수 있으므로 직접 부른다.
                RefreshRollBudgetState();
                return;
            }

            dice.SyncFromAuthority(gameSession.State.Dice);
            turnDelay?.Pause();
            scoreSheet?.ClearCandidateScores();
            SetRollInteraction(false);
            RefreshAugmentPresentation(GetAugmentEventMessage(pendingRollResult));
            rollRoutine = StartCoroutine(RunRollSequence());
        }

        private IEnumerator RunRollSequence()
        {
            Phase = PresentationPhase.Rolling;
            SetRollInteraction(false);

            // 코스믹 큐브 / 수정구 황도 12궁 다음 별자리로 순차 전환 (부드러운 크로스페이드)
            rollCosmicCube?.AdvanceZodiac();
            rollOrb?.AdvanceZodiac();

            RollPresentation presentation = pendingRollResult?.RollPresentation;
            if (presentation == null)
            {
                rollRoutine = null;
                Phase = PresentationPhase.AwaitingRoll;
                turnDelay?.Resume();
                RefreshGameInteraction();
                yield break;
            }

            dice.SetVisible(true);
            UpdateStatusText($"주사위 굴리는 중... (Preset #{presentation.PresetIndex + 1})");
            yield return dice.PlayRoll(presentation);

            Phase = PresentationPhase.Settled;
            rollRoutine = null;
            pendingRollResult = null;
            scoreSheet?.ShowCandidateScores(gameSession.CurrentPlayerIndex, gameSession.CurrentCandidates);
            turnDelay?.Resume();
            RefreshGameInteraction();
            RefreshAugmentPresentation(GetAugmentEventMessage(gameSession.LastCommandResult));
            UpdateStatusText();
        }

        /// <summary>이 명령이 연기 가림 연출을 요구하는지. 어떤 증강이 그런지는 planner가 판정한다.</summary>
        private bool HasDiceSmokeSwap(YachtGameCommandResult result)
        {
            if (result?.Events == null || result.Events.Length == 0) return false;

            vfxBuffer.Clear();
            AugmentVfxPlanner.Plan(result.Events, gameSession.State, vfxBuffer);
            for (int i = 0; i < vfxBuffer.Count; i++)
            {
                if (vfxBuffer[i].Cue == AugmentVfxCue.DiceSmokeSwap) return true;
            }
            return false;
        }

        /// <summary>
        /// 연기가 주사위를 가린 사이에 눈을 바꾼다(56 `dice-alchemy`).
        ///
        /// 로직은 이미 눈을 바꿨고 여기서 미루는 것은 표시뿐이다. 사양서 §9.4의 타임라인을 따른다.
        /// </summary>
        private IEnumerator RunDiceSmokeSwapSequence(string message)
        {
            if (gameSession == null || dice == null) yield break;
            dice.BurstSmokeOverUnkeptDice();

            yield return new WaitForSeconds(SmokeDigitsHideSeconds);
            if (gameSession == null || dice == null) yield break;
            dice.SetUnkeptCrispRenderersVisible(false);

            yield return new WaitForSeconds(SmokeSwapSeconds - SmokeDigitsHideSeconds);
            if (gameSession == null || dice == null) yield break;
            dice.SyncFromAuthority(gameSession.State.Dice);
            dice.ApplyValuesToVisuals();
            if (gameSession.Phase == YachtGamePhase.ScoreSelection)
                scoreSheet?.ShowCandidateScores(gameSession.CurrentPlayerIndex, gameSession.CurrentCandidates);
            RefreshAugmentPresentation(message);
            UpdateStatusText(message);

            yield return new WaitForSeconds(SmokeDigitsShowSeconds - SmokeSwapSeconds);
            if (gameSession == null || dice == null) yield break;
            dice.SetUnkeptCrispRenderersVisible(true);

            yield return new WaitForSeconds(SmokeTotalSeconds - SmokeDigitsShowSeconds);
            smokeRoutine = null;
            RefreshRollBudgetState();
        }

        public bool SetDieKept(int index, bool kept)
        {
            if (gameSession == null || !gameSession.CanKeepDice) return false;
            if (Phase != PresentationPhase.Settled) return false;
            if (index < 0 || index >= dice.DiceCount || !dice.HasVisual(index)) return false;
            if (dice.IsKept(index) == kept) return true;

            if (!gameSession.TrySetDieKept(index, kept)) return false;
            dice.ApplyKeep(index, kept);
            return true;
        }

        public void ToggleKeep(int dieIndex)
        {
            if (dieIndex < 0 || dieIndex >= dice.DiceCount) return;
            SetDieKept(dieIndex, !dice.IsKept(dieIndex));
        }

        private void OnArrangeStarted()
        {
            Phase = PresentationPhase.Arranging;
        }

        private void OnArrangeCompleted()
        {
            Phase = PresentationPhase.Settled;
            UpdateStatusText();
            RefreshGameInteraction();
        }

        private void SetRollInteraction(bool interactable)
        {
            rollCosmicCube?.SetInteractable(interactable);
            rollOrb?.SetInteractable(interactable);
            RefreshRollBudgetState();
        }

        /// <summary>
        /// 굴림 예산을 코스믹 큐브 상태로 옮긴다. 판 뒤집기는 게이트가 아니다.
        /// 등가교환만이 "보라(대기)"와 "회색(소진)"을 가른다.
        ///
        /// CanRoll이 아니라 RollsRemaining을 본다. CanRoll은 위상을, RefreshGameInteraction의
        /// canRoll은 dice.AllKept를 함께 접는데 둘 다 이 신호에 속하지 않고 이미 isInteractable을 탄다.
        /// </summary>
        public static RollBudgetState ResolveRollBudgetState(int rollsRemaining, bool equivalentExchangeReady)
        {
            if (rollsRemaining > 0) return RollBudgetState.Normal;
            return equivalentExchangeReady ? RollBudgetState.AugmentReady : RollBudgetState.Drained;
        }

        /// <summary>
        /// 코스믹 큐브의 굴림 예산 상태를 다시 계산한다. 상호작용 토글과 같은 자리에서 도는 이유는
        /// 하나다. 굴림 수를 건드리거나 턴을 넘기는 모든 경로가 이미 여기를 지나므로, 여기 한 곳에
        /// 두면 상태가 고착될 경로가 남지 않는다. 두 신호 자체는 큐브 안에서 여전히 분리되어 있다.
        /// </summary>
        private void RefreshRollBudgetState()
        {
            if (rollCosmicCube == null) return;
            rollCosmicCube.SetRollBudgetState(gameSession == null
                ? RollBudgetState.Normal
                : ResolveRollBudgetState(gameSession.RollsRemaining, gameSession.CanUseEquivalentExchange));
        }

        public void RefreshAugmentPresentation(string message = null)
        {
            TrayRebindRequested?.Invoke();
            augmentTray?.Refresh(gameSession, Phase.IsInteractive(), message);
            SyncAugmentStickers();
        }

        /// <summary>
        /// 점수표에 붙는 변형 증강 스티커를 현재 상태에 맞춘다.
        ///
        /// 두 사람이 각자 Categories 열을 가지므로 양쪽 것을 모두 붙인다. 접힌 쪽은 아이콘 섹터
        /// 폭만 남으므로 상대가 무엇을 교체했는지는 증강 아이콘으로 읽는다.
        /// 새로 생긴 스티커에는 부착 연출을, 확정한 칸에는 낙인 연출을 준다.
        /// 시각 사양은 <c>docs/augmented_yacht_m17_vfx_spec.md</c> §3.1.1이다.
        /// </summary>
        private void SyncAugmentStickers()
        {
            if (scoreSheet == null || gameSession == null) return;

            // 드래프트 도중에는 손대지 않는다. 한 명이 고를 때마다 붙었다 떨어지면 어수선하다.
            // 드래프트가 끝난 뒤 한 번에 붙인다.
            if (gameSession.IsDrafting) return;

            for (int playerIndex = 0; playerIndex < 2; playerIndex++) SyncAugmentStickers(playerIndex);

            PlayPendingStickerStamps();
        }

        private void SyncAugmentStickers(int playerIndex)
        {
            AugmentStickerCatalog.CollectFor(gameSession.State, playerIndex, stickerBuffer);

            nextStickers.Clear();
            attachedStickers.Clear();
            removedStickers.Clear();

            Dictionary<ScoreCategory, string> shown = shownStickers[playerIndex];

            for (int i = 0; i < stickerBuffer.Count; i++)
            {
                AugmentStickerPlacement placement = stickerBuffer[i];
                YachtAugmentDefinition definition = YachtAugmentRuntime.Lookup(placement.AugmentId);
                if (definition == null) continue;

                if (!scoreSheet.HasStickerSlot(playerIndex, placement.Category))
                {
                    Debug.LogWarning($"[증강 스티커] {placement.Category} 칸의 슬롯이 없습니다. 점수표 UI가 아직 만들어지지 않았습니다.");
                    continue;
                }

                nextStickers[placement.Category] = placement.AugmentId;

                // 같은 증강이 이미 그 칸에 붙어 있으면 그대로 둔다. 다시 붙이면 진행 중인 연출이 끊긴다.
                if (shown.TryGetValue(placement.Category, out string current)
                    && string.Equals(current, placement.AugmentId, StringComparison.Ordinal)) continue;

                scoreSheet.SetSticker(
                    playerIndex,
                    placement.Category,
                    AugmentStickerCatalog.BaseColor(placement.AugmentId),
                    AugmentStickerCatalog.BorderColor,
                    ResolveStickerIcon(placement.AugmentId),
                    AugmentStickerCatalog.MarkLabel(placement.AugmentId, definition.DisplayName));
                attachedStickers.Add(placement.Category);
            }

            foreach (KeyValuePair<ScoreCategory, string> entry in shown)
                if (!nextStickers.ContainsKey(entry.Key)) removedStickers.Add(entry.Key);

            for (int i = 0; i < removedStickers.Count; i++) scoreSheet.ClearSticker(playerIndex, removedStickers[i]);
            for (int i = 0; i < attachedStickers.Count; i++) scoreSheet.PlayStickerAttach(playerIndex, attachedStickers[i]);

            shown.Clear();
            foreach (KeyValuePair<ScoreCategory, string> next in nextStickers) shown[next.Key] = next.Value;
        }

        /// <summary>마지막 명령의 이벤트에서 낙인 연출을 뽑는다. 리비전이 같으면 이미 처리한 것이다.</summary>
        private void PlayPendingStickerStamps()
        {
            YachtGameCommandResult result = gameSession.LastCommandResult;
            if (result?.Events == null || result.Events.Length == 0) return;

            long revision = gameSession.State.Revision;
            if (revision == lastVfxRevision) return;
            lastVfxRevision = revision;

            vfxBuffer.Clear();
            AugmentVfxPlanner.Plan(result.Events, gameSession.State, vfxBuffer);
            for (int i = 0; i < vfxBuffer.Count; i++)
            {
                AugmentVfxRequest request = vfxBuffer[i];
                if (request.Cue != AugmentVfxCue.StickerStamp) continue;

                // 낙인은 그 칸으로 점수를 확정한 사람 것이다. 이 시점에는 턴이 이미 넘어갔을 수
                // 있으므로 현재 플레이어가 아니라 이벤트가 지목한 사람을 쓴다.
                int actor = request.PlayerIndex;
                if (actor < 0 || actor > 1) continue;
                if (!shownStickers[actor].ContainsKey(request.Category)) continue;
                scoreSheet.PlayStickerStamp(actor, request.Category);
            }
        }

        /// <summary>증강 고유 아이콘이다. 카드와 같은 경로를 쓴다(<c>D-022</c>).</summary>
        private Sprite ResolveStickerIcon(string augmentId)
        {
            if (string.IsNullOrEmpty(augmentId)) return null;
            if (stickerIcons.TryGetValue(augmentId, out Sprite cached)) return cached;

            Sprite icon = Resources.Load<Sprite>($"AugmentIcons/{augmentId}");
            stickerIcons[augmentId] = icon;
            return icon;
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

        /// <summary>타이머 문구를 모래시계 위 화면 좌표에 붙인다. 매 프레임 컨트롤러가 부른다.</summary>
        public void UpdateTimerTextPosition()
        {
            if (timerText == null || timerAnchor == null || worldCamera == null) return;
            Vector3 worldPosition = timerAnchor.position + Vector3.up * 2.8f;
            Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z > 0f) timerText.rectTransform.position = screenPosition;
        }

        public void UpdateStatusText(string message = null)
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

            int hovered = dice.HoveredIndex;
            string interaction = hovered >= 0 && Phase == PresentationPhase.Settled
                ? (dice.IsKept(hovered) ? "CLICK: UNKEEP" : "CLICK: KEEP")
                : $"KEEP {dice.KeptCount}/{dice.DiceCount}";

            string valuesSummary = Phase.HasCompletedRoll() ? $" [ {string.Join(", ", dice.Values)} ]" : "";
            string currentZodiac = rollCosmicCube != null ? rollCosmicCube.CurrentZodiacName : (rollOrb != null ? rollOrb.CurrentZodiacName : "");
            string zodiacInfo = !string.IsNullOrEmpty(currentZodiac) ? $"  |  ★ {currentZodiac}" : "";
            string modeText = gameSession.Mode == YachtGameMode.Augmented ? "증강" : "일반";
            string turnInfo = $"{modeText}  |  P{gameSession.CurrentPlayerIndex + 1}  |  {gameSession.CurrentRound}/12 라운드  |  굴림 {gameSession.RollsRemaining}회";

            statusText.text = string.IsNullOrEmpty(message)
                ? $"{turnInfo}  |  {interaction}{valuesSummary}{zodiacInfo}"
                : $"{message}  |  {turnInfo}  |  {interaction}{valuesSummary}{zodiacInfo}";
        }

        private static TurnSide MapPlayerToTurnSide(int playerIndex)
        {
            return playerIndex == 0 ? TurnSide.Left : TurnSide.Right;
        }

        private void OnDestroy()
        {
            if (scoreSheet != null) scoreSheet.ScoreSelected -= OnScoreSelected;
            if (turnDelay != null)
            {
                turnDelay.Started -= OnTurnTimerStarted;
                turnDelay.Ticked -= OnTurnTimerTick;
                turnDelay.Expired -= OnTurnTimerExpired;
            }
            if (dice != null)
            {
                dice.ArrangeStarted -= OnArrangeStarted;
                dice.ArrangeCompleted -= OnArrangeCompleted;
            }
        }
    }
}
