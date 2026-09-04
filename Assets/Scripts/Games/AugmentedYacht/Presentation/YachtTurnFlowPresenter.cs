using System;
using System.Collections;
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

        private const float TurnDurationSeconds = YachtGameOptions.DefaultTurnDurationSeconds;

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
            return new YachtGameSession(scoreSheet.Player1, scoreSheet.Player2, options);
        }

        public void SelectDraftOption(int optionIndex)
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
                scoreSheet?.SetActivePlayer(gameSession.State.Draft.PlayerIndex, false);
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
                    scoreSheet?.SetActivePlayer(gameSession.State.Draft.PlayerIndex, false);
                    RefreshAugmentPresentation(transitionMessage);
                    UpdateStatusText(transitionMessage);
                    return;
                }
                ResetDiceForTurn();
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
            rerollCounterBar?.SetRollsRemaining(0, YachtGameSession.MaxRolls);
            scoreSheet?.SetActivePlayer(-1, false);
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
                if (dice.VisualCount != gameSession.State.Dice.Length)
                {
                    ResetDiceForTurn();
                    Phase = PresentationPhase.AwaitingRoll;
                }
                else
                {
                    dice.SyncFromAuthority(gameSession.State.Dice);
                }
                scoreSheet?.ClearCandidateScores();
                if (gameSession.Phase == YachtGamePhase.ScoreSelection)
                    scoreSheet?.ShowCandidateScores(gameSession.CurrentPlayerIndex, gameSession.CurrentCandidates);
                string message = GetAugmentEventMessage(pendingRollResult);
                RefreshAugmentPresentation(message);
                UpdateStatusText(message);
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
        }

        public void RefreshAugmentPresentation(string message = null)
        {
            TrayRebindRequested?.Invoke();
            augmentTray?.Refresh(gameSession, Phase.IsInteractive(), message);
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
