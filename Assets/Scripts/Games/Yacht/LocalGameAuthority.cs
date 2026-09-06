using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tessera.Games.Yacht
{
    public sealed class LocalGameAuthority : IGameAuthority
    {
        private readonly YachtGameOptions options;
        private readonly IRandomSource random;
        private readonly IRandomSource visualRandom;
        private readonly IYachtRuleSet rules;
        private readonly YachtAugmentRuntime augmentRuntime = new();
        private readonly HashSet<string> acceptedCommandIds = new(StringComparer.Ordinal);
        private readonly YachtGameState state;

        public LocalGameAuthority(
            YachtGameOptions options = null,
            IRandomSource random = null,
            PlayerScoreData[] scoreData = null,
            IYachtRuleSet rules = null,
            IRandomSource visualRandom = null)
        {
            this.options = options?.Clone() ?? new YachtGameOptions();
            if (this.options.PlayerCount != YachtGameSession.PlayerCount)
                throw new ArgumentOutOfRangeException(nameof(options), "현재 로컬 요트는 2인만 지원합니다.");
            if (this.options.DiceCount != 5)
                throw new ArgumentOutOfRangeException(nameof(options), "기본 요트는 주사위 5개를 사용합니다.");

            this.random = random ?? new SystemRandomSource();
            this.visualRandom = visualRandom ?? new SystemRandomSource();
            this.rules = rules ?? YachtRuleSetFactory.Create(this.options.Mode);
            if (this.rules.Mode != this.options.Mode)
                throw new ArgumentException("실행 옵션과 규칙 세트의 게임 모드가 다릅니다.", nameof(rules));

            PlayerScoreData[] players = scoreData ?? new[] { new PlayerScoreData(), new PlayerScoreData() };
            if (players.Length != this.options.PlayerCount)
                throw new ArgumentException("플레이어 점수 데이터 수가 잘못되었습니다.", nameof(scoreData));
            for (int i = 0; i < players.Length; i++) players[i] ??= new PlayerScoreData();

            state = new YachtGameState
            {
                Mode = this.options.Mode,
                Phase = YachtGamePhase.WaitingToStart,
                Players = players,
                Dice = this.rules.CreateInitialDice(this.options.DiceCount)
            };
            ResetGameState(false);
        }

        /// <summary>
        /// 권위 계층 자신과 그 테스트 하네스가 쓰는 내부 상태 핸들이다. 의도적으로 구체 타입이다.
        /// 테스트가 시나리오를 조립하려면 상태를 직접 세울 수 있어야 하고, 여기는 상태의 안쪽이다.
        /// 화면이 읽는 경로는 <see cref="YachtGameSession.State"/>이며 그쪽은 읽기 전용 뷰다.
        /// </summary>
        public YachtGameState CurrentState => state;
        public YachtGameOptions Options => options.Clone();
        public IYachtRuleSet RuleSet => rules;
        public float CurrentTurnDurationSeconds => state.Mode == YachtGameMode.Augmented
            ? augmentRuntime.GetTurnDuration(state, state.CurrentPlayerIndex, options.TurnDurationSeconds)
            : options.TurnDurationSeconds;
        public Task<YachtGameCommandResult> ExecuteAsync(YachtGameCommand command) => Task.FromResult(Execute(command));

        public YachtGameCommandResult Execute(YachtGameCommand command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.CommandId))
                return Reject(YachtCommandErrorCode.InvalidCommand, "명령 ID가 필요합니다.");
            if (acceptedCommandIds.Contains(command.CommandId))
                return Reject(YachtCommandErrorCode.DuplicateCommand, "이미 처리한 명령입니다.");
            if (command.ExpectedRevision != state.Revision)
                return Reject(YachtCommandErrorCode.RevisionMismatch, "예상 revision이 현재 상태와 다릅니다.");
            if (command.PlayerIndex < 0 || command.PlayerIndex >= options.PlayerCount)
                return Reject(YachtCommandErrorCode.InvalidPlayer, "플레이어 인덱스가 잘못되었습니다.");

            YachtGameCommandResult result = command.Type switch
            {
                YachtCommandType.StartGame => StartGame(command),
                YachtCommandType.SelectAugment => SelectAugment(command),
                YachtCommandType.RollDice => RollDice(command),
                YachtCommandType.SetDieKept => SetDieKept(command),
                YachtCommandType.CommitScore => CommitScore(command),
                YachtCommandType.ResolveTimeout => ResolveTimeout(command),
                YachtCommandType.UseAugmentAction => UseAugmentAction(command),
                YachtCommandType.AdvanceTurn => AdvanceTurn(),
                _ => Reject(YachtCommandErrorCode.InvalidCommand, "지원하지 않는 명령입니다.")
            };
            if (!result.Accepted) return result;

            acceptedCommandIds.Add(command.CommandId);
            state.Revision++;
            result.State = state.Clone();
            return result;
        }

        private YachtGameCommandResult StartGame(YachtGameCommand command)
        {
            ResetGameState(true);
            var events = new List<YachtGameEvent>
            {
                new() { Type = YachtGameEventType.GameStarted, PlayerIndex = command.PlayerIndex }
            };
            if (augmentRuntime.TryBeginDraft(state, random, visualRandom, out YachtGameEvent draftEvent)) events.Add(draftEvent);
            return Accept(events.ToArray());
        }

        private YachtGameCommandResult SelectAugment(YachtGameCommand command)
        {
            if (state.Mode != YachtGameMode.Augmented)
                return Reject(YachtCommandErrorCode.AugmentUnavailable, "일반 모드에서는 증강을 선택할 수 없습니다.");
            if (!augmentRuntime.TrySelectAugment(
                    state,
                    command.PlayerIndex,
                    command.AugmentId,
                    random,
                    visualRandom,
                    out YachtGameEvent[] events,
                    out YachtCommandErrorCode code,
                    out string message))
                return Reject(code, message);

            if (state.Phase == YachtGamePhase.TurnReady)
            {
                augmentRuntime.PrepareTurn(state, state.CurrentPlayerIndex, random, false);
                ResetDiceForCurrentPlayer();
            }
            return Accept(events);
        }

        private YachtGameCommandResult RollDice(YachtGameCommand command) => RollDice(command, null);

        private YachtGameCommandResult RollDice(YachtGameCommand command, string freeRollAugmentId)
        {
            YachtGameCommandResult turnError = ValidateCurrentPlayer(command);
            if (turnError != null) return turnError;
            if (state.Phase != YachtGamePhase.TurnReady && state.Phase != YachtGamePhase.ScoreSelection)
                return Reject(YachtCommandErrorCode.InvalidPhase, "현재 단계에서는 주사위를 굴릴 수 없습니다.");
            bool freeRoll = !string.IsNullOrEmpty(freeRollAugmentId);
            bool tableFlip = string.Equals(freeRollAugmentId, YachtAugmentRuntime.TableFlipId, StringComparison.Ordinal);
            if (!freeRoll && state.RollsRemaining <= 0)
                return Reject(YachtCommandErrorCode.NoRollsRemaining, "남은 굴림 횟수가 없습니다.");

            bool allKept = state.Dice.Length > 0;
            for (int i = 0; i < state.Dice.Length; i++) allKept &= state.Dice[i].IsKept;
            if (allKept) return Reject(YachtCommandErrorCode.AllDiceKept, "모든 주사위가 킵되어 있습니다.");

            if (!freeRoll) state.RollsRemaining--;
            for (int i = 0; i < state.Dice.Length; i++)
            {
                YachtDieState die = state.Dice[i];
                if (!die.IsKept)
                    die.Value = state.Mode == YachtGameMode.Augmented
                        ? augmentRuntime.RollValue(die, random, () => rules.RollValue(die, random))
                        : rules.RollValue(die, random);
            }
            state.HasRolled = true;
            state.Phase = YachtGamePhase.ScoreSelection;
            UpdateCandidates();

            var presentation = new RollPresentation
            {
                PresetFile = state.Mode == YachtGameMode.Augmented
                    ? augmentRuntime.SelectPresetFile(state.Dice, tableFlip)
                    : rules.SelectPresetFile(state.Dice),
                PresetIndex = random.NextInt(0, Math.Max(1, options.PresetClipCount)),
                IsMirrored = random.NextBool(),
                DurationSeconds = 2.5f,
                FinalValues = new YachtDieResult[state.Dice.Length]
            };
            for (int i = 0; i < state.Dice.Length; i++)
            {
                YachtDieState die = state.Dice[i];
                presentation.FinalValues[i] = new YachtDieResult { Id = die.Id, Type = die.Type, Value = die.Value };
            }
            YachtGameEvent gameEvent = freeRoll
                ? new YachtGameEvent
                {
                    Type = YachtGameEventType.AugmentActionUsed,
                    PlayerIndex = command.PlayerIndex,
                    AugmentId = freeRollAugmentId,
                    Message = $"{freeRollAugmentId} 사용"
                }
                : new YachtGameEvent { Type = YachtGameEventType.DiceRolled, PlayerIndex = command.PlayerIndex };
            return Accept(gameEvent, presentation);
        }

        private YachtGameCommandResult UseAugmentAction(YachtGameCommand command)
        {
            YachtGameCommandResult turnError = ValidateCurrentPlayer(command);
            if (turnError != null) return turnError;
            if (YachtAugmentCatalog.Find(command.AugmentId) is not IManualActionAugment action)
                return Reject(YachtCommandErrorCode.AugmentUnavailable, "지원하지 않는 증강 행동입니다.");

            if (state.Phase != action.RequiredPhase)
            {
                string phaseMessage = action.RequiredPhase == YachtGamePhase.TurnReady
                    ? "첫 굴림 전에만 사용할 수 있습니다."
                    : "현재 단계에서는 사용할 수 없습니다.";
                if (string.Equals(command.AugmentId, YachtAugmentRuntime.TableFlipId, StringComparison.Ordinal))
                    phaseMessage = "현재 단계에서는 판 뒤집기를 사용할 수 없습니다.";
                else if (string.Equals(command.AugmentId, YachtAugmentRuntime.EquivalentExchangeId, StringComparison.Ordinal))
                    phaseMessage = "현재 단계에서는 등가교환을 사용할 수 없습니다.";
                else if (string.Equals(command.AugmentId, YachtAugmentRuntime.DiceAlchemyId, StringComparison.Ordinal))
                    phaseMessage = "첫 굴림 후 주사위 연금술을 사용할 수 있습니다.";

                return Reject(YachtCommandErrorCode.InvalidPhase, phaseMessage);
            }

            var actionContext = new AugmentActionContext(state, command.PlayerIndex, random, null);
            actionContext.BindAugment(command.AugmentId);
            if (!action.CanUse(actionContext, out YachtCommandErrorCode code, out string message))
                return Reject(code, message);

            if (action.RerollsDice)
            {
                YachtGameCommandResult result = RollDice(command, command.AugmentId);
                if (result.Accepted) action.Use(actionContext);
                return result;
            }

            action.Use(actionContext);
            if (string.Equals(command.AugmentId, YachtAugmentRuntime.GambitId, StringComparison.Ordinal))
                ResetDiceForCurrentPlayer();
            else if (string.Equals(command.AugmentId, YachtAugmentRuntime.DiceAlchemyId, StringComparison.Ordinal))
                UpdateCandidates();

            return Accept(new YachtGameEvent
            {
                Type = YachtGameEventType.AugmentActionUsed,
                PlayerIndex = command.PlayerIndex,
                AugmentId = command.AugmentId,
                Message = string.Equals(command.AugmentId, YachtAugmentRuntime.DiceAlchemyId, StringComparison.Ordinal)
                    ? "주사위 연금술 사용"
                    : $"{command.AugmentId} 발동"
            });
        }

        private YachtGameCommandResult SetDieKept(YachtGameCommand command)
        {
            YachtGameCommandResult turnError = ValidateCurrentPlayer(command);
            if (turnError != null) return turnError;
            if (state.Phase != YachtGamePhase.ScoreSelection || !state.HasRolled)
                return Reject(YachtCommandErrorCode.RollRequired, "굴린 주사위만 킵할 수 있습니다.");

            YachtDieState die = FindDie(command.DieId);
            if (die == null) return Reject(YachtCommandErrorCode.DieNotFound, "주사위를 찾을 수 없습니다.");
            die.IsKept = command.IsKept;
            if (command.IsKept && die.KeepSlotIndex < 0)
            {
                int nextSlot = 0;
                for (int i = 0; i < state.Dice.Length; i++)
                    if (state.Dice[i].IsKept && state.Dice[i].KeepSlotIndex >= nextSlot)
                        nextSlot = state.Dice[i].KeepSlotIndex + 1;
                die.KeepSlotIndex = nextSlot;
            }
            else if (!command.IsKept) die.KeepSlotIndex = -1;
            return Accept(new YachtGameEvent
            {
                Type = YachtGameEventType.DieKeepChanged,
                PlayerIndex = command.PlayerIndex,
                DieId = die.Id
            });
        }

        private YachtGameCommandResult CommitScore(YachtGameCommand command)
        {
            YachtGameCommandResult turnError = ValidateCurrentPlayer(command);
            if (turnError != null) return turnError;
            if (state.Phase != YachtGamePhase.ScoreSelection || !state.HasRolled)
                return Reject(YachtCommandErrorCode.RollRequired, "점수 기입 전에 주사위를 굴려야 합니다.");
            if (!YachtScoreCalculator.IsScorable(command.Category))
                return Reject(YachtCommandErrorCode.CategoryUnavailable, "기입할 수 없는 족보입니다.");
            if (IsCategoryFilled(command.PlayerIndex, command.Category))
                return Reject(YachtCommandErrorCode.CategoryAlreadyFilled, "이미 기입한 족보입니다.");
            if (!TryGetCandidate(command.Category, out YachtScoreCandidate candidate))
                return Reject(YachtCommandErrorCode.CategoryUnavailable, "현재 족보의 점수를 계산할 수 없습니다.");
            return Commit(command.PlayerIndex, candidate, false);
        }

        private YachtGameCommandResult ResolveTimeout(YachtGameCommand command)
        {
            YachtGameCommandResult turnError = ValidateCurrentPlayer(command);
            if (turnError != null) return turnError;
            if (state.Phase != YachtGamePhase.TurnReady && state.Phase != YachtGamePhase.ScoreSelection)
                return Reject(YachtCommandErrorCode.InvalidPhase, "현재 단계에서는 시간 초과를 처리할 수 없습니다.");

            ScoreCategory selected = default;
            int bestScore = int.MinValue;
            bool found = false;
            for (int i = 0; i < YachtScoreCalculator.ScorableCategories.Length; i++)
            {
                ScoreCategory category = YachtScoreCalculator.ScorableCategories[i];
                if (IsCategoryFilled(command.PlayerIndex, category)) continue;
                YachtScoreCandidate candidate = state.HasRolled && TryGetCandidate(category, out YachtScoreCandidate calculated)
                    ? calculated
                    : new YachtScoreCandidate { Category = category, BaseScore = 0, Score = 0 };
                if (found && candidate.Score <= bestScore) continue;
                selected = category;
                bestScore = candidate.Score;
                found = true;
            }
            return found && TryGetCandidate(selected, out YachtScoreCandidate selectedCandidate)
                ? Commit(command.PlayerIndex, selectedCandidate, true)
                : found ? Commit(command.PlayerIndex, new YachtScoreCandidate { Category = selected, BaseScore = 0, Score = 0 }, true)
                : Reject(YachtCommandErrorCode.CategoryUnavailable, "남은 족보가 없습니다.");
        }

        private YachtGameCommandResult AdvanceTurn()
        {
            if (state.Phase != YachtGamePhase.TurnTransition)
                return Reject(YachtCommandErrorCode.InvalidPhase, "턴 전환 단계가 아닙니다.");

            if (state.IsExtraTurnPhase)
            {
                int extraPlayer = FindNextExtraTurnPlayer(state.CurrentPlayerIndex);
                if (extraPlayer < 0)
                {
                    state.Phase = YachtGamePhase.GameOver;
                    return Accept(new YachtGameEvent { Type = YachtGameEventType.GameEnded, PlayerIndex = state.CurrentPlayerIndex });
                }
                state.CurrentPlayerIndex = extraPlayer;
            }
            else if (state.CurrentPlayerIndex == 0)
            {
                state.CurrentPlayerIndex = 1;
            }
            else if (state.CurrentRound < YachtGameSession.LastRound)
            {
                state.CurrentPlayerIndex = 0;
                state.CurrentRound++;
            }
            else
            {
                int extraPlayer = FindNextExtraTurnPlayer(state.CurrentPlayerIndex);
                if (extraPlayer < 0)
                {
                    state.Phase = YachtGamePhase.GameOver;
                    return Accept(new YachtGameEvent { Type = YachtGameEventType.GameEnded, PlayerIndex = state.CurrentPlayerIndex });
                }
                state.CurrentPlayerIndex = extraPlayer;
                state.IsExtraTurnPhase = true;
            }
            BeginTurn();
            var events = new List<YachtGameEvent>
            {
                new() { Type = YachtGameEventType.TurnAdvanced, PlayerIndex = state.CurrentPlayerIndex }
            };
            if (!state.IsExtraTurnPhase && augmentRuntime.TryBeginDraft(state, random, visualRandom, out YachtGameEvent draftEvent))
                events.Add(draftEvent);
            return Accept(events.ToArray());
        }

        private YachtGameCommandResult Commit(int playerIndex, YachtScoreCandidate candidate, bool timeout)
        {
            ScoreCategory category = candidate.Category;
            int score = candidate.Score;
            SetScore(state.Players[playerIndex], category, candidate.BaseScore, score);
            int normalRollCount = YachtGameSession.MaxRolls - state.RollsRemaining;
            var augmentEvents = new List<YachtGameEvent>();
            if (state.Mode == YachtGameMode.Augmented)
                augmentEvents.AddRange(augmentRuntime.AfterScoreCommit(
                    state, playerIndex, normalRollCount, category, candidate.BaseScore, score, state.Dice, random));
            if (state.IsExtraTurnPhase && state.AugmentPlayers[playerIndex].ExtraTurns > 0)
                state.AugmentPlayers[playerIndex].ExtraTurns--;

            bool standardTurnsComplete = playerIndex == options.PlayerCount - 1 && state.CurrentRound >= YachtGameSession.LastRound;
            bool gameEnded = (standardTurnsComplete || state.IsExtraTurnPhase) && FindNextExtraTurnPlayer(playerIndex) < 0;
            state.Candidates = Array.Empty<YachtScoreCandidate>();
            state.HasRolled = false;
            if (gameEnded) state.RollsRemaining = 0;
            state.Phase = gameEnded ? YachtGamePhase.GameOver : YachtGamePhase.TurnTransition;

            var events = new List<YachtGameEvent>
            {
                new YachtGameEvent
                {
                    Type = YachtGameEventType.ScoreCommitted,
                    PlayerIndex = playerIndex,
                    Category = category,
                    Score = score
                }
            };
            events.AddRange(augmentEvents);
            if (timeout) events.Add(new YachtGameEvent { Type = YachtGameEventType.TimeoutResolved, PlayerIndex = playerIndex, Category = category, Score = score });
            if (gameEnded) events.Add(new YachtGameEvent { Type = YachtGameEventType.GameEnded, PlayerIndex = playerIndex });
            return Accept(events.ToArray());
        }

        private void ResetGameState(bool startImmediately)
        {
            // 중복 명령 방지 집합은 한 게임 안에서만 의미가 있다. 새 게임을 시작할 때 비우지 않으면
            // 판을 거듭할수록 계속 자라기만 한다. 원격 클라이언트가 명령 ID를 정하게 되는 M18
            // 이후에는 이 집합이 클라이언트 입력에 따라 무한히 커지는 구조가 되므로 지금 끊어 둔다.
            // revision은 계속 증가하므로, 옛 명령이 다시 들어와도 ExpectedRevision 검사에서 걸린다.
            acceptedCommandIds.Clear();

            for (int i = 0; i < state.Players.Length; i++) state.Players[i].Reset();
            state.Mode = options.Mode;
            state.CurrentPlayerIndex = 0;
            state.CurrentRound = 1;
            state.Dice = rules.CreateInitialDice(options.DiceCount);
            augmentRuntime.Initialize(state, options.PlayerCount);
            ResetDiceForCurrentPlayer();
            state.Candidates = Array.Empty<YachtScoreCandidate>();
            state.HasRolled = false;
            state.RollsRemaining = YachtGameSession.MaxRolls;
            state.IsExtraTurnPhase = false;
            state.Phase = startImmediately ? YachtGamePhase.TurnReady : YachtGamePhase.WaitingToStart;
        }

        private void BeginTurn()
        {
            state.RollsRemaining = YachtGameSession.MaxRolls;
            state.HasRolled = false;
            state.Candidates = Array.Empty<YachtScoreCandidate>();
            if (state.Mode == YachtGameMode.Augmented)
                augmentRuntime.PrepareTurn(state, state.CurrentPlayerIndex, random, true);
            ResetDiceForCurrentPlayer();
            state.Phase = YachtGamePhase.TurnReady;
        }

        private void ResetDiceForCurrentPlayer()
        {
            int diceCount = state.Mode == YachtGameMode.Augmented
                ? augmentRuntime.GetDiceCount(state, state.CurrentPlayerIndex, options.DiceCount)
                : options.DiceCount;
            state.Dice = rules.CreateInitialDice(diceCount);
            if (state.Mode == YachtGameMode.Augmented)
                augmentRuntime.ConfigureDice(state, state.CurrentPlayerIndex, state.Dice);
        }

        private void UpdateCandidates()
        {
            if (state.Mode == YachtGameMode.Augmented)
            {
                YachtScoreCandidate[] augmentedCalculated = augmentRuntime.CreateScoreCandidates(state, state.CurrentPlayerIndex, state.Dice);
                var augmented = new List<YachtScoreCandidate>(augmentedCalculated.Length);
                for (int i = 0; i < augmentedCalculated.Length; i++)
                    if (!IsCategoryFilled(state.CurrentPlayerIndex, augmentedCalculated[i].Category)) augmented.Add(augmentedCalculated[i]);
                state.Candidates = augmented.ToArray();
                return;
            }
            Dictionary<ScoreCategory, int> calculated = rules.CalculateScores(state.Dice);
            var candidates = new List<YachtScoreCandidate>(calculated.Count);
            foreach (ScoreCategory category in YachtScoreCalculator.ScorableCategories)
            {
                if (!IsCategoryFilled(state.CurrentPlayerIndex, category) && calculated.TryGetValue(category, out int score))
                    candidates.Add(new YachtScoreCandidate { Category = category, BaseScore = score, Score = score });
            }
            state.Candidates = candidates.ToArray();
        }

        private YachtGameCommandResult ValidateCurrentPlayer(YachtGameCommand command) =>
            command.PlayerIndex == state.CurrentPlayerIndex
                ? null
                : Reject(YachtCommandErrorCode.NotCurrentPlayer, "현재 플레이어의 명령이 아닙니다.");

        private YachtDieState FindDie(int dieId)
        {
            for (int i = 0; i < state.Dice.Length; i++) if (state.Dice[i].Id == dieId) return state.Dice[i];
            return null;
        }

        private bool TryGetCandidate(ScoreCategory category, out YachtScoreCandidate candidate)
        {
            for (int i = 0; i < state.Candidates.Length; i++)
            {
                if (state.Candidates[i].Category != category) continue;
                candidate = state.Candidates[i];
                return true;
            }
            candidate = null;
            return false;
        }

        public bool IsCategoryFilled(int playerIndex, ScoreCategory category)
        {
            if (playerIndex < 0 || playerIndex >= state.Players.Length) return true;
            int categoryIndex = (int)category;
            if (categoryIndex >= 0 && categoryIndex <= 5)
                return state.Players[playerIndex].upperFilled[categoryIndex] || state.Players[playerIndex].upperScores[categoryIndex] != -1;
            if (categoryIndex >= 7 && categoryIndex <= 12)
                return state.Players[playerIndex].lowerFilled[categoryIndex - 7] || state.Players[playerIndex].lowerScores[categoryIndex - 7] != -1;
            return true;
        }

        private static void SetScore(PlayerScoreData data, ScoreCategory category, int baseScore, int score)
        {
            int categoryIndex = (int)category;
            if (categoryIndex >= 0 && categoryIndex <= 5)
            {
                data.upperScores[categoryIndex] = score;
                data.upperBaseScores[categoryIndex] = baseScore;
                data.upperFilled[categoryIndex] = true;
            }
            else if (categoryIndex >= 7 && categoryIndex <= 12)
            {
                int lower = categoryIndex - 7;
                data.lowerScores[lower] = score;
                data.lowerBaseScores[lower] = baseScore;
                data.lowerFilled[lower] = true;
            }
            else throw new ArgumentOutOfRangeException(nameof(category));
            data.RecalculateTotal();
        }

        private int FindNextExtraTurnPlayer(int afterPlayerIndex)
        {
            if (state.Mode != YachtGameMode.Augmented) return -1;
            for (int step = 1; step <= options.PlayerCount; step++)
            {
                int playerIndex = (afterPlayerIndex + step) % options.PlayerCount;
                if (state.AugmentPlayers[playerIndex].ExtraTurns > 0 && HasRemainingCategory(playerIndex))
                    return playerIndex;
            }
            return -1;
        }

        private bool HasRemainingCategory(int playerIndex)
        {
            for (int i = 0; i < YachtScoreCalculator.ScorableCategories.Length; i++)
                if (!IsCategoryFilled(playerIndex, YachtScoreCalculator.ScorableCategories[i])) return true;
            return false;
        }

        private YachtGameCommandResult Accept(YachtGameEvent gameEvent, RollPresentation presentation = null) => Accept(new[] { gameEvent }, presentation);
        private YachtGameCommandResult Accept(YachtGameEvent[] events, RollPresentation presentation = null) => new()
        {
            Accepted = true,
            ErrorCode = YachtCommandErrorCode.None,
            Events = events ?? Array.Empty<YachtGameEvent>(),
            RollPresentation = presentation
        };
        private YachtGameCommandResult Reject(YachtCommandErrorCode code, string message) => new()
        {
            Accepted = false,
            ErrorCode = code,
            ErrorMessage = message,
            State = state.Clone()
        };
    }

    /// <summary>Unity 프레젠테이션을 권위 명령 API에 연결하는 얇은 로컬 파사드입니다.</summary>
    public sealed class YachtGameSession
    {
        public const int PlayerCount = 2;
        public const int LastRound = 12;
        public const int MaxRolls = 3;

        private readonly LocalGameAuthority authority;
        private long nextCommandId;

        /// <summary>
        /// 점수표를 권위가 직접 소유하는 기본 경로다.
        ///
        /// 예전에는 <c>ParchmentScoreSheet</c>가 <c>PlayerScoreData</c>를 직렬화 필드로 들고
        /// 그것을 권위에 넘겼다. 그러면 화면 컴포넌트가 권위 데이터의 저장소를 겸하게 되어
        /// 소유권이 갈라진다. 이제 화면은 <see cref="State"/>의 읽기 전용 뷰만 본다.
        /// </summary>
        public YachtGameSession(
            YachtGameOptions options = null,
            IRandomSource random = null,
            IRandomSource visualRandom = null)
            : this(new PlayerScoreData(), new PlayerScoreData(), options, random, visualRandom)
        {
        }

        /// <summary>점수표 인스턴스를 밖에서 넘기는 경로다. 테스트가 결과를 직접 들여다볼 때 쓴다.</summary>
        public YachtGameSession(
            PlayerScoreData playerOne,
            PlayerScoreData playerTwo,
            YachtGameOptions options = null,
            IRandomSource random = null,
            IRandomSource visualRandom = null)
        {
            authority = new LocalGameAuthority(options, random, new[]
            {
                playerOne ?? throw new ArgumentNullException(nameof(playerOne)),
                playerTwo ?? throw new ArgumentNullException(nameof(playerTwo))
            }, visualRandom: visualRandom);
        }

        /// <summary>
        /// 화면이 읽는 권위 상태다. 스냅샷이 아니라 같은 객체를 가리키는 읽기 전용 뷰이므로
        /// 읽기 비용은 없고, 대신 화면 쪽에서의 쓰기가 컴파일 시점에 막힌다.
        /// 상태를 바꾸려면 이 클래스의 <c>Try*</c> 명령을 거쳐야 한다.
        /// </summary>
        public IReadOnlyYachtGameState State => authority.CurrentState;

        /// <summary>세션 내부 계산용. 읽기 전용 뷰에 없는 값(후보·점수 등)까지 본다.</summary>
        private YachtGameState AuthorityState => authority.CurrentState;

        public YachtGamePhase Phase => AuthorityState.Phase;
        public YachtGameMode Mode => AuthorityState.Mode;
        public int CurrentPlayerIndex => AuthorityState.CurrentPlayerIndex;
        public int CurrentRound => AuthorityState.CurrentRound;
        public int RollsRemaining => AuthorityState.RollsRemaining;
        public bool HasRolled => AuthorityState.HasRolled;
        public bool IsDrafting => Phase == YachtGamePhase.Draft;
        public float CurrentTurnDurationSeconds => authority.CurrentTurnDurationSeconds;
        public YachtGameCommandResult LastCommandResult { get; private set; }
        public bool CanRoll => (Phase == YachtGamePhase.TurnReady || Phase == YachtGamePhase.ScoreSelection) && RollsRemaining > 0;
        public bool CanKeepDice => Phase == YachtGamePhase.ScoreSelection && HasRolled;
        public bool CanUseTableFlip => Phase == YachtGamePhase.ScoreSelection
            && AuthorityState.AugmentPlayers != null
            && CurrentPlayerIndex < AuthorityState.AugmentPlayers.Length
            && !AuthorityState.AugmentPlayers[CurrentPlayerIndex].TableFlipUsed
            && ContainsOwnedAugment(CurrentPlayerIndex, YachtAugmentRuntime.TableFlipId);
        public IReadOnlyDictionary<ScoreCategory, int> CurrentCandidates
        {
            get
            {
                var result = new Dictionary<ScoreCategory, int>();
                for (int i = 0; i < AuthorityState.Candidates.Length; i++) result[AuthorityState.Candidates[i].Category] = AuthorityState.Candidates[i].Score;
                return result;
            }
        }

        public void StartNewGame() => LastCommandResult = Execute(YachtCommandType.StartGame, 0);
        public bool TrySelectAugment(string augmentId, out YachtGameCommandResult result)
        {
            int playerIndex = AuthorityState.Draft?.PlayerIndex ?? -1;
            result = playerIndex >= 0
                ? Execute(YachtCommandType.SelectAugment, playerIndex, augmentId: augmentId)
                : new YachtGameCommandResult { Accepted = false, ErrorCode = YachtCommandErrorCode.NotDrafting };
            LastCommandResult = result;
            return result.Accepted;
        }
        public bool TryRoll(out YachtGameCommandResult result)
        {
            result = LastCommandResult = Execute(YachtCommandType.RollDice, CurrentPlayerIndex);
            return result.Accepted;
        }
        public bool TryUseTableFlip(out YachtGameCommandResult result)
        {
            return TryUseAugmentAction(YachtAugmentRuntime.TableFlipId, out result);
        }
        public bool TryUseAugmentAction(string augmentId, out YachtGameCommandResult result)
        {
            result = LastCommandResult = Execute(YachtCommandType.UseAugmentAction, CurrentPlayerIndex, augmentId: augmentId);
            return result.Accepted;
        }
        public bool TrySetDieKept(int dieIndex, bool kept)
        {
            if (dieIndex < 0 || dieIndex >= AuthorityState.Dice.Length) return false;
            LastCommandResult = Execute(YachtCommandType.SetDieKept, CurrentPlayerIndex, AuthorityState.Dice[dieIndex].Id, kept);
            return LastCommandResult.Accepted;
        }
        public bool TryCommitScore(ScoreCategory category, out YachtTurnResult result)
        {
            int player = CurrentPlayerIndex;
            YachtGameCommandResult commandResult = Execute(YachtCommandType.CommitScore, player, category: category);
            LastCommandResult = commandResult;
            result = CreateTurnResult(commandResult, player, category);
            return commandResult.Accepted;
        }
        public bool ResolveTimeout(out YachtTurnResult result)
        {
            int player = CurrentPlayerIndex;
            YachtGameCommandResult commandResult = Execute(YachtCommandType.ResolveTimeout, player);
            LastCommandResult = commandResult;
            result = CreateTurnResult(commandResult, player, default);
            return commandResult.Accepted;
        }
        public bool AdvanceTurnAfterAnimation()
        {
            LastCommandResult = Execute(YachtCommandType.AdvanceTurn, CurrentPlayerIndex);
            return LastCommandResult.Accepted;
        }
        public bool IsCategoryFilled(int playerIndex, ScoreCategory category) => authority.IsCategoryFilled(playerIndex, category);
        public IReadOnlyPlayerScoreData GetPlayer(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= AuthorityState.Players.Length) throw new ArgumentOutOfRangeException(nameof(playerIndex));
            return AuthorityState.Players[playerIndex];
        }

        private YachtGameCommandResult Execute(
            YachtCommandType type,
            int playerIndex,
            int dieId = 0,
            bool isKept = false,
            ScoreCategory category = default,
            string augmentId = null)
        {
            return authority.Execute(new YachtGameCommand
            {
                CommandId = $"local-{++nextCommandId}",
                ExpectedRevision = AuthorityState.Revision,
                PlayerIndex = playerIndex,
                Type = type,
                DieId = dieId,
                IsKept = isKept,
                Category = category,
                AugmentId = augmentId
            });
        }

        private bool ContainsOwnedAugment(int playerIndex, string augmentId)
        {
            string[] owned = AuthorityState.AugmentPlayers[playerIndex].OwnedIds;
            for (int i = 0; i < owned.Length; i++)
                if (string.Equals(owned[i], augmentId, StringComparison.Ordinal)) return true;
            return false;
        }

        private static YachtTurnResult CreateTurnResult(YachtGameCommandResult result, int playerIndex, ScoreCategory fallback)
        {
            int score = 0;
            ScoreCategory category = fallback;
            for (int i = 0; i < result.Events.Length; i++)
            {
                YachtGameEvent gameEvent = result.Events[i];
                if (gameEvent.Type != YachtGameEventType.ScoreCommitted) continue;
                category = gameEvent.Category;
                score = gameEvent.Score;
                break;
            }
            return new YachtTurnResult(playerIndex, category, score, result.State?.Phase == YachtGamePhase.GameOver);
        }
    }
}
