# 게임 구현 영향 범위

> **문서 종류**: 태스크 사전 조사 (AI 대상)
> **코드 대조 기준**: 2026-10-25 · 커밋 `a1cb2a6` (`tessera-scout` 조사)
> **대상**: `BAL-T1`~`BAL-T6`, 결정 `D-057`·`D-058`·`D-059`

경로는 `Assets/02.Scripts/Games/Yacht/` 기준입니다. 줄 번호 앞의 `~`는 근처라는 뜻입니다. 각 태스크는 착수할 때 이 목록을 다시 대조합니다.

섹션 이름과 코드 용어: Numbers는 코드의 상단(upper, Aces~Sixes), Combinations는 하단(lower, Choice~Yacht)입니다(`D-059`).

## BAL-T1 15턴과 단계 드래프트

- 총 라운드: `Rules/LocalGameAuthority.cs:713` `YachtGameSession.LastRound = 12`. 사용처는 `:413` `AdvanceTurn`(라운드 증가)과 `:452` `Commit`(종료 판정, 추가 턴이 있으면 연장)입니다. 기본 모드(`NormalYachtRuleSet`)도 같은 값을 씁니다.
- `/12` 표기: `Presentation/Flow/YachtTurnFlowPresenter.cs:1054`, `Presentation/Flow/AugmentedYachtController.cs:314`, `Presentation/Hud/YachtDebugPanel.cs:122`.
- 라운드 룬: `Tabletop/RunicSlateMatrix.cs:16` `OuterRuneCount = 12`, `[Range(0, OuterRuneCount)]` 필드 `:44`, `:48`. 호출은 `YachtTurnFlowPresenter.cs:236`, `:371`의 `SetRoundProgress`입니다. 상수를 바꿔도 직렬화 값은 그대로이고 Range 상한만 바뀝니다. 룬 배치는 시각 확인 대상입니다.
- 드래프트 라운드: `Augments/Framework/YachtAugmentRuntime.cs:851` `IsDraftRound`(1·6·9), `:852` `ExpectedSelectionCount`. 호출은 `:436`, `:438`, `:506`이고 `TryBeginDraft`는 `LocalGameAuthority.cs:~423`에서 부릅니다.
- 첫 드래프트 가정: `YachtAugmentRuntime.cs:875`는 `CurrentRound == 1`이면 선공을 무작위로 정하고, `:887` `DraftTotalScore`는 총점으로 순서를 정합니다. 첫 드래프트가 4라운드로 바뀌면 이 조건을 다시 정합니다.
- 단계 필드: `PhaseOneOnly` 정의 `YachtAugmentRuntime.cs:29`, 유일한 사용처 `:917` `CanAcquire`, 베이스 `Augments/Modification/ModificationAugment.cs:18,27`, `Augments/Quest/QuestAugment.cs:14,26`. 오버라이드는 `Blackjack21`, `FibonacciNumbers`, `SecondChoice`, `ReverseChoice`, `FastStraight`, `StepByStep`입니다.
- 후보 풀: `CreateDraftOptions` `:890~911`(전체를 `CanAcquire`로 거른 뒤 셔플, 같은 대상 칸 변형은 한 장만 `:903~907`). `random-box` 교체 풀은 `ResolveRandomBoxes` `:1050~1074`(퀘스트 제외, `CanAcquire` 경유)입니다.
- 테스트: `YachtDraftOrderTests.cs:40,41,50,58`, `YachtGameRulesTests.cs:195,272,319,540`, `YachtRandomFullGamePlaythroughTests.cs:36`. 증강 테스트 대부분이 `CurrentRound = 1`을 초기값으로 씁니다.

## BAL-T2 증강 턴 규칙

- `Augments/Quest/FastStraight.cs:37` `DeadlineTurn = 8`, 판정 `:75~77`(`TurnsTaken + 1 <= DeadlineTurn`).
- `Augments/Quest/Holdout.cs:29` `MinTurn = 9`, 판정 `:55~57`.
- `Augments/Quest/Nozdormu.cs:55` 목표 턴, `:63~67` 15초 제한, `:69~82` 보상.
- `Augments/ManualAction/DoubleDown.cs:32` `MinTurnsTaken = 8`, 사용 조건 `:77`, 문구 `:23`.
- `TurnsTaken`은 플레이어별 누적 카운터입니다(`YachtAugmentRuntime.cs:82`, 증가 `:726`).

## BAL-T3 Hills 규칙 코어

- 칸 enum `Rules/YachtGameCore.cs:19~35`: Aces 0 ~ Sixes 5, Bonus 6, Choice 7 ~ Yacht 12, Total 13.
- 칸 목록 `YachtGameCore.cs:472` `ScorableCategories`(12칸). 사용처는 `LocalGameAuthority.cs:376`(시간 초과), `~600`(후보 갱신), `687`(추가 턴 판단), `YachtAugmentRuntime.cs:636,642`, `Augments/Quest/BountyHunter.cs:116`, `Presentation/Table/ParchmentScoreSheet.cs:1656`입니다.
- 저장: `YachtGameCore.cs:90~` `PlayerScoreData`. 상단·하단 배열이 6칸씩이고 `[Serializable]` 공개 배열입니다. `Reset` `:108~112`와 `EnsureArrays` `:189~192`가 길이 6을 하드코딩합니다.
- 인덱스 오프셋(상단 0~5, 하단 `index - 7`) 하드코딩: `YachtGameCore.cs:183`, `LocalGameAuthority.cs:648`, `:662`(범위 밖이면 `ArgumentOutOfRangeException`), `YachtAugmentRuntime.cs:1020~1048`, `Augments/Quest/BountyHunter.cs:126~129`, `Augments/Quest/Copycat.cs:65,81~93`, `ParchmentScoreSheet.cs:1610~1612`.
- 총점: `YachtGameCore.cs:136` `RecalculateTotal`(상단 + 보너스 + 증강 보너스 + 하단). 플레이어 객체 안에서 끝나 상대 정보가 없습니다. 두 플레이어를 함께 가진 객체는 `YachtGameState`(`YachtGameCore.cs:231`)입니다.
- 선례: `Augments/Enhance/Duel.cs:46~`가 상대 점수에 의존하는 보너스를 `RoundScores`와 `AddBonusTo`(`AugmentContexts.cs:80`)로 처리합니다. 다만 증강 훅은 증강 모드에서만 돕니다(`LocalGameAuthority.cs:~434`). Hills는 기본 모드에도 적용하므로 규칙 계층에 둡니다.
- 점수 계산: 기본 모드는 `YachtScoreCalculator.Calculate`(`YachtGameCore.cs:~481`), 증강 모드는 `YachtAugmentScoreEngine.cs:18` `CalculateBaseScores`입니다. 둘 다 칸별 딕셔너리이고, `YachtAugmentRuntime.cs:643`이 키를 직접 인덱싱하므로 키가 빠지면 예외가 납니다.
- 증강 효과의 자동 적용: `CreateScoreCandidates`(`YachtAugmentRuntime.cs:627`)는 `ScorableCategories` 전체에 같은 루프를 돕니다. Hills가 목록과 딕셔너리에 들어가면 주사위 필터(`yacht-bank`, `GetScoringDice`), 가산(`CalculateDiceBonus`, 기본 점수 0이면 생략 `:664`), 배수(`IScoreEnhancementModifier`, `double-down` `:655~659`)가 칸 분기 없이 걸립니다. 주사위 개수는 `GetDiceCount`(`:565`)가 정합니다.
- 칸 이름: `Rules/ScoreCategoryNames.cs`(12칸 switch). 테스트 `AugmentProgressTextTests.cs:235`.
- 시간 초과 자동 기입: `LocalGameAuthority.cs:371~395`. 미기입 칸 가운데 후보 점수가 가장 높은 칸을 고르고, 굴리지 않았으면 0으로 기입합니다. 호출은 `YachtTurnFlowPresenter.cs:395`, `:412`입니다. Hills 칸에 들어가면 값 대신 X(기권)를 적고 무조건 집니다(`D-059`). X는 기입 값 0과 구별되는 상태로 저장해야 판정과 표시가 섞이지 않습니다.
- 최종 승패 표시: `YachtTurnFlowPresenter.cs:470~473`(`TotalScore` 비교).
- 테스트: `YachtRandomFullGamePlaythroughTests.cs:40`(모든 칸이 찼는지), `YachtModificationAugmentTests.cs:172`.

## BAL-T4 점수판 UI

- 행은 코드로 만듭니다: `Presentation/Table/ParchmentScoreSheet.cs:613` `BuildHighResScoreSheetUI`. 프리팹 `Assets/03.Prefabs/Tabletop/3D Layered Parchment Score Sheet.prefab`과 씬 `Assets/01.Scenes/Augmented Dice.unity`에는 행 오브젝트가 없습니다.
- 15행 균등 분할 `:710`(헤더, 상단 6, 보너스, 하단 6, 푸터), 밴드 위치 `:737~739`, 행 생성 루프 `:777~815`. 섹션 순서는 Numbers(상단 6, 보너스), Hills 3, Combinations(하단 6)이고(`D-059`) 헤더·푸터를 합쳐 18행이 됩니다. Hills 칸은 기권이면 X를 표시합니다.
- 길이 14 배열 `:45~80`(라벨·슬롯·버튼·타깃 프레임·뱃지·스티커), `StickerKey = playerIndex * 14 + categoryIndex` `:1007`, 표시 갱신 `:1690~1730`(라벨 인덱스 고정).
- 테스트: `Assets/Editor/Tests/Games/Yacht/ScoreSheetColumnLayoutTests.cs`(열 배치). 점수판 구조를 쓰는 `AugmentBountyHunterTargetMarkTests.cs`, `AugmentParchmentStateTests.cs`, `AugmentStickerCatalogTests.cs`, `YachtBankHighlightTests.cs`는 세부가 미확정입니다.

## BAL-T5 증강과 Hills의 상호작용

- 칸 분기를 가진 증강: `Augments/Quest/Copycat.cs:65`(`>= Choice` 비교), `Augments/Quest/EveryLittleCounts.cs:73`(`CountUsedOnes`), `Augments/Quest/StepByStep.cs:79`, `Augments/Quest/BountyHunter.cs:116,126~129`(대상 칸을 `ScorableCategories`에서 뽑음).
- 점수 값을 보는 퀘스트: `doubling`(같은 점수 두 번), `prophet`. Hills 기입 값을 이 퀘스트의 점수로 볼지 정해야 합니다.
- 변형 덮어쓰기: `YachtAugmentRuntime.cs:995~997`에서 `ResetFilledTarget`(`:1020`)을 부르고, 찬 칸이면 지운 뒤 `ExtraTurns`를 올립니다. 대상 칸은 `ModificationAugment.Target`(`ModificationAugment.cs:15`)을 문자열로 저장합니다.
- 드래프트 순서: `DraftTotalScore`(`YachtAugmentRuntime.cs:887`)가 총점을 읽으므로 Hills 판정 점수가 순서에 반영됩니다.
- X 기입(`D-059`): `momentum`은 기본 점수 0인 기입을 발동 조건으로 봅니다(`Augments/Enhance/Momentum.cs`). X를 0점 기입으로 볼지 정해야 합니다. 점수 값을 비교하는 `doubling`·`prophet`도 같습니다.

## 착수 때 정할 것

- `BAL-T1`: 첫 드래프트(4라운드)의 순서 규칙(`YachtAugmentRuntime.cs:875`).
- `BAL-T3`: Hills의 enum 배치. 끝(14~16)에 붙이면 길이 14 배열과 `SetScore` 범위 검사가 바로 걸리고, `Copycat`의 `>= Choice` 비교가 Hills를 하단으로 셉니다. 중간에 끼우면 Choice~Total 값이 밀리므로, 직렬화된 `ScoreCategory` 값이 있으면 깨집니다(직렬화 사용처는 미확정, `.claude/rules/serialization.md`). 점수판 표시 순서(Numbers·Hills·Combinations)는 enum 순서를 강제하지 않습니다(`D-059`).
- `BAL-T3`: 자동 기입이 Hills를 고르는 조건. 가정은 Hills 밖 빈 칸이 있으면 현행 규칙대로 그중에서 고르고, 빈 칸이 Hills뿐일 때만 X를 적는 것입니다. 현행처럼 후보 점수 최댓값으로 고르면 눈의 합이 큰 Hills가 자주 뽑혀 기권이 잦아집니다.
- 봇·자동 진행 로직은 찾지 못했습니다(미확정). 자동 기입은 시간 초과 경로뿐입니다.
