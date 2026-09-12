# 프로젝트 데드코드 정리 작업 제안서 (Dead Code Cleanup Proposal)

> **문서 종류**: 시점 기록 (보관)
> **작성 시점**: 2026-09-12 — 본문은 이 시점의 코드 상태를 기술하며, 이후 변경을 반영하지 않습니다.
> **보관 사유**: 제안 내용이 커밋 `53fc78e`로 실행되어 보관합니다. 실행 결과와의 차이는 아래에 적었습니다.
>
> 이 문서를 실행 지침으로 쓰지 마십시오. 보관 색인은 [`archive/README.md`](../README.md)입니다.

> **문서 상태**: `실행 완료` — 2026-09-12 커밋 `53fc78e`("데드코드 정리 4단계 적용")로 적용됨. 보관용입니다.
> **실행 결과와의 차이** (2026-09-13 대조):
> - Phase 3의 `ZodiacConstellationData.cs` 삭제는 **수행되지 않았습니다.** 파일이 520줄 그대로 남아 있고
>   `RollCosmicCube.cs`와 `AugmentedYachtController.cs`가 여전히 참조합니다. 황도 12궁 연출은 폐기된
>   기능이지만 코드는 남기고 게임에 적재만 하지 않는 상태입니다 (`EnabledInGame = false`).
>   따라서 Phase 3 절감량 "약 1,630 LOC"도 실제로는 `RollOrb.cs`(약 1,110줄) 제거분뿐입니다.
> - Phase 4의 `Assets/TutorialInfo/**`는 삭제 완료됐습니다. 디렉터리가 존재하지 않습니다.
>
> **이 문서를 실행 지침으로 다시 쓰지 마십시오.** 남은 항목을 처리하려면 현재 코드를 새로 조사해
> 새 제안을 작성합니다.
>
> **원래 상태 표기**: 제안 (Proposal / Ready for Implementation)  
> **기준 일자**: 2026-09-12  
> **대상 저장소**: Tessera (`c:\Users\dpcks\Unity\Tessera`)  
> **선행 작업**: 2026-09-12 데드코드 전수조사 (Assets 내 177개 C# 스크립트, 씬, 프리팹, ScriptableObject 에셋 대상 정적 분석)

---

## 1. 개요 및 목적

본 제안서는 2026-09-12 전수조사에서 식별된 **미사용 코드(Dead Code)**, **폐기된 레거시 서브시스템**, **컴파일러 경고 유발 필드**를 다른 AI 에이전트 또는 엔지니어가 안전하고 외과수술적(Surgical)으로 제거할 수 있도록 작업 절차와 기술적 세부사항을 정의한 작업 지침서입니다.

### 작업 기본 원칙 (AGENTS.md 준수)
1. **외과수술적 변경 (Surgical Changes)**: 지목된 데드코드만 정확히 제거합니다. 인접한 정상 코드 재포맷, 무관한 주석 수정, 미요청 리팩토링은 절대 금지합니다.
2. **식별자는 영문 (English Identifiers)**: 신규 추가/수정하는 모든 식별자 및 테스트는 영문으로 작성합니다.
3. **단계적 실행 및 검증 (Stepwise Verification)**: 단계(Phase)별로 작업을 수행하고, 각 단계 종료 시 반드시 컴파일 경고 및 유닛 테스트를 검증합니다.
4. **회귀 방지 (No Regressions)**: 메인 게임 루프([Augmented Dice.unity](../../../Assets/Scenes/Augmented Dice.unity)), 렌더링 파이프라인, 에디터 도구에 영향이 없어야 합니다.

---

## 2. 데드코드 카탈로그 및 단계별 분할

작업은 위험도와 결합도에 따라 4단계로 분할합니다.

| 단계 | 목표 | 주요 대상 | 예상 절감 | 위험도 |
|---|---|---|---|:---:|
| **Phase 1** | 컴파일 경고 해소 및 순수 미사용 내부 헬퍼/인터페이스 정리 | `playmatTexture`, `YachtAugmentRuntime` 헬퍼 4종, `IOnTurnEnded` 등 | 약 100 LOC | **낮음 (안전)** |
| **Phase 2** | 외부 참조 0개인 공개 멤버 및 유틸리티 메서드 정리 | `GetUprightRotation`, `DescribeNames`, `RunicSlateMatrix` 미사용 제어 메서드 등 | 약 90 LOC | **낮음** |
| **Phase 3** | 폐기된 대형 서브시스템 제거 (`RollOrb` & `ZodiacConstellationData`) | `RollOrb.cs`, `ZodiacConstellationData.cs` 및 관련 결합부 | **약 1,630 LOC** | **중간 (구조적)** |
| **Phase 4** | 프로젝트 기본 템플릿 번들 에셋 정리 | `Assets/TutorialInfo/` 전체 | 디렉터리 정리 | **낮음** |

---

## 3. 단계별 상세 작업 지침 (Implementation Guide)

### Phase 1: 컴파일 경고 해소 및 순수 미사용 내부 헬퍼/인터페이스 정리 (Safe Deletions)

외부 결합도가 전혀 없는 `private` 멤버, 컴파일러 경고 유발 필드, 구현체 없는 인터페이스를 정리합니다.

#### 1.1 [AugmentedYachtController.cs](../../../Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentedYachtController.cs) 미사용 필드 및 상수 제거
- **위치 1**: Line 27
  ```csharp
  [SerializeField] private Texture2D playmatTexture;
  ```
  - **작업**: 필드 선언 삭제 (CS0169 경고 해소).
- **위치 2**: Line 85, Line 87
  ```csharp
  private const float RightSectionWidth = TableWidth * 0.3f;
  ...
  private const int DecorationLayer = 11;
  ```
  - **작업**: 두 미사용 `const` 상수 선언 삭제.

#### 1.2 [YachtAugmentRuntime.cs](../../../Assets/Scripts/Games/AugmentedYacht/Logic/YachtAugmentRuntime.cs) 레거시 헬퍼 4종 제거
과거 증강 로직이 `Augments/` 폴더 내 개별 클래스들로 분리되기 전에 사용되던 잔재 헬퍼들입니다.
- **위치**: Line 1078 ~ Line 1136
  - `GrantBonus(YachtGameState state, int playerIndex, string augmentId, int score, ICollection<YachtGameEvent> events)` (Line 1078-1095)
  - `CountUsedOnes(ScoreCategory category, int baseScore, IReadOnlyList<YachtDieState> dice)` (Line 1097-1108)
  - `GetBaseScore(PlayerScoreData scores, ScoreCategory category)` (Line 1118-1125)
  - `SelectEmptyCategory(YachtGameState state, int playerIndex, IRandomSource random)` (Line 1127-1136)
- **작업**: 위 4개 `private static` 메서드 블록 완전 삭제. (참조하는 곳 없음 확인 완료)

#### 1.3 [AugmentCardView.cs](../../../Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentCardView.cs) 미사용 픽셀 지우기 헬퍼 제거
- **위치**: Line 401 ~ Line 404
  ```csharp
  private static void ClearRect(Color32[] pixels, int minX, int minY, int maxX, int maxY)
  {
      FillRect(pixels, minX, minY, maxX, maxY, default);
  }
  ```
- **작업**: `ClearRect` 메서드 삭제 (`ClearDiamond`만 실제로 사용됨).

#### 1.4 [TurnBalanceIndicator.cs](../../../Assets/Scripts/Tabletop/TurnBalanceIndicator.cs) 미사용 지오메트리 헬퍼 제거
- **위치**: Line 640 ~ Line 648
  ```csharp
  private static void CreateRod(string name, Transform parent, Vector3 start, Vector3 end, float radius, Material material)
  ...
  ```
- **작업**: `CreateRod` 메서드 삭제.

#### 1.5 [IAugmentHandler.cs](../../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Core/IAugmentHandler.cs) 미사용 인터페이스 제거
- **위치**: Line 47 ~ Line 51
  ```csharp
  /// <summary>턴이 끝날 때 호출됩니다. <see cref="IAfterScoreCommit"/> 다음에 처리합니다.</summary>
  public interface IOnTurnEnded
  {
      void OnTurnEnded(AugmentCommitContext context);
  }
  ```
- **작업**: 인터페이스 `IOnTurnEnded` 선언 삭제 (45종 증강 중 구현체 0개, 디스패처 호출 0개).

#### 1.6 [YachtRunicPresenter.cs](../../../Assets/Scripts/Games/AugmentedYacht/Presentation/YachtRunicPresenter.cs) 쓰기 전용 필드 정리
- **위치**: Line 16, Line 18, Line 21
  ```csharp
  private ParchmentScoreSheet scoreSheet; // Line 16 (쓰기만 되고 읽히지 않음)
  ...
  public void Bind(RunicSlateMatrix matrix, ParchmentScoreSheet sheet) // Line 18
  {
      runicSlateMatrix = matrix;
      scoreSheet = sheet; // Line 21
      ResolveMatrix();
  }
  ```
- **작업**: 
  - `private ParchmentScoreSheet scoreSheet;` 필드 삭제.
  - `scoreSheet = sheet;` 대입문 삭제.
  - 호출부([AugmentedYachtController.cs#L704](../../../Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentedYachtController.cs#L704))의 시그니처 호환성을 고려해 `Bind` 메서드의 파라미터 `ParchmentScoreSheet sheet`는 `_`로 폐기하거나, 시그니처를 `Bind(RunicSlateMatrix matrix)`로 단순화하고 `AugmentedYachtController.cs:704`의 호출을 `runicPresenter.Bind(runicSlateMatrix);`로 수정.

#### 1.7 [LocalGameAuthority.cs](../../../Assets/Scripts/Games/Yacht/LocalGameAuthority.cs) 미사용 래퍼 제거
- **위치**: Line 741 ~ Line 744
  ```csharp
  public bool TryUseTableFlip(out YachtGameCommandResult result)
  {
      return TryUseAugmentAction(YachtAugmentRuntime.TableFlipId, out result);
  }
  ```
- **작업**: `TryUseTableFlip` 삭제 (외부 호출처 0개이며 `TryUseAugmentAction`으로 일원화됨).

---

### Phase 2: 외부 참조 0개인 공개 멤버 및 유틸리티 정리 (Low-Medium Risk)

#### 2.1 [DiceFaceOrientation.cs](../../../Assets/Scripts/Dice/DiceFaceOrientation.cs)
- **위치**: Line 107 ~ Line 110
  ```csharp
  public static Quaternion GetUprightRotation(Quaternion landingRotation, Vector3 faceUpDirection)
  {
      return GetTopRotation(GetTopValue(landingRotation), faceUpDirection);
  }
  ```
- **작업**: `GetUprightRotation` 메서드 삭제 (`GetCameraFacingRotation`, `GetCameraFacingUprightRotation`만 사용됨).

#### 2.2 [DiceShapeBaker.cs](../../../Assets/Editor/DiceShapeBaker.cs)
- **위치**: Line 468 ~ Line 473
  ```csharp
  private static string DescribeNames(List<Transform> transforms)
  {
      var names = new List<string>();
      for (int i = 0; i < transforms.Count && i < 8; i++) names.Add(transforms[i].name);
      return string.Join(", ", names);
  }
  ```
- **작업**: 미사용 헬퍼 `DescribeNames` 삭제.

#### 2.3 [RunicSlateMatrix.cs](../../../Assets/Scripts/Tabletop/RunicSlateMatrix.cs) 미사용 내부 및 제어 멤버 정리
- **위치 및 대상**:
  - Line 81: `public bool StoneRunesLit => stoneRunesLit;` (참조 0개 프로퍼티)
  - Line 199 ~ Line 206: `public void SetMaxExtraTurns(int capacity)`
  - Line 219 ~ Line 224: `public void ClearRoundProgress()`
  - Line 268 ~ Line 305: `public void PlayOuterRuneSequence()`
  - Line 307 ~ Line 317: `public void ResetVisualState(bool clearStones)`
  - Line 558 ~ Line 582: `private void BuildInnerArcGuide()` (아크 가이드 생성 private 헬퍼이나 내부 미호출)
- **작업**: 위 6개 멤버/메서드 삭제.

#### 2.4 기타 단일 미사용 프로퍼티 정리
- [DicePresetCatalog.cs#L137](../../../Assets/Scripts/Dice/DicePresetCatalog.cs#L137): `public int FrameCount => Frames.Length;` 삭제.
- [AugmentTrayCardView.cs#L64](../../../Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentTrayCardView.cs#L64): `public Transform DepthMask => depthMask;` 삭제 (`depthMask` 필드는 내부에서 유지).
- [AugmentStateStore.cs#L18, L20](../../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Core/AugmentStateStore.cs#L18): `public string IdAt(int index)` 및 `public IAugmentState StateAt(int index)` 삭제.
- [AugmentContexts.cs#L217](../../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Core/AugmentContexts.cs#L217): `public int TurnNumber => Player.TurnsTaken + 1;` 삭제.

---

### Phase 3: 폐기된 대형 서브시스템 제거 (`RollOrb` & `ZodiacConstellationData`, 약 1,630줄)

> [!WARNING]
> 이 단계는 파일 삭제 및 결합 컴포넌트의 코드 수정이 수반됩니다. 순서대로 진행해야 컴파일 오류가 발생하지 않습니다.

#### 3.1 사전 배경 확인
- `RollOrb`는 초기 프로토타입의 3D 수정구 굴림 버튼이었으나, 현재 메인 씬([Augmented Dice.unity](../../../Assets/Scenes/Augmented Dice.unity))에서는 `RollCosmicCube`(성운 큐브)가 단독으로 사용되고 있습니다.
- `ZodiacConstellationData`는 `RollOrb`의 별자리 연출 전용 데이터였으며, `EnabledInGame = false`로 꺼져 있습니다.

#### 3.2 결합 코드 정리 (선행 필수)
1. **[AugmentedYachtController.cs](../../../Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentedYachtController.cs)**
   - Line 42: `[SerializeField] private RollOrb rollOrb;` 필드 삭제.
   - Line 91: `public RollOrb RollOrb => rollOrb;` 프로퍼티 삭제.
2. **[RerollCounterBar.cs](../../../Assets/Scripts/Tabletop/RerollCounterBar.cs)**
   - Line 232 ~ Line 238:
     ```csharp
     // 1. RollOrb의 lowerBase_stone 및 upperBase_stone, goldTrim, goldDark와 100% 동일한 머티리얼 구성
     RollOrb rollOrb = FindFirstObjectByType<RollOrb>();
     if (rollOrb != null)
     {
         ...
     }
     else
     {
         ...
     }
     ```
   - **수정**: `FindFirstObjectByType<RollOrb>()` 분기 및 `RollOrb` 타입 참조를 제거하고, 현재도 항상 실행되고 있는 `else` 블록(독립적인 Stylized Wood/Stone 머티리얼 생성 로직)으로 단일화.
3. **[RollCosmicCube.cs](../../../Assets/Scripts/Tabletop/RollCosmicCube.cs)**
   - Line 28, Line 36: `currentZodiacIndex`, `ZodiacConstellationData.GetDefinition(...)` 관련 프로퍼티 삭제 또는 상수로 대체.
   - Line 196 ~ Line 203: `ZodiacConstellationData.EnabledInGame` 확인 분기 및 `constellationRenderer` 무력화 코드 정리.
   - `ZodiacConstellationData` 네임스페이스 및 타입 참조 제거.

#### 3.3 파일 및 메타데이터 삭제 (후행)
- `Assets/Scripts/Tabletop/RollOrb.cs` 및 `Assets/Scripts/Tabletop/RollOrb.cs.meta` 삭제
- `Assets/Scripts/Tabletop/ZodiacConstellationData.cs` 및 `Assets/Scripts/Tabletop/ZodiacConstellationData.cs.meta` 삭제

---

### Phase 4: 프로젝트 기본 템플릿 번들 에셋 정리 (Hygiene)

- **대상 디렉터리**: `Assets/TutorialInfo/`
- **내용**:
  - `Assets/TutorialInfo/Scripts/Readme.cs`
  - `Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs`
  - `Assets/TutorialInfo/Icons/`
  - `Assets/TutorialInfo/Readme.asset`
- **작업**: Unity URP 기본 템플릿 생성 시 자동 포함된 잔재로, 게임 프로젝트와 완전히 무관하므로 디렉터리 전체 삭제.

---

## 4. 검증 계획 (Verification Plan)

에이전트는 각 Phase 완료 시마다 아래 검증을 순차적으로 수행해야 합니다.

### 1) C# 솔루션 빌드 및 경고 검증 (CLI)
터미널에서 모든 경고를 활성화한 상태로 빌드를 실행합니다:
```powershell
dotnet build Assembly-CSharp.csproj /p:NoWarn="" /p:WarningLevel=5
dotnet build Assembly-CSharp-Editor.csproj /p:NoWarn="" /p:WarningLevel=5
```
- **성공 기준**: 컴파일 오류 0개, 경고 0개 (기존 `CS0169` 경고가 해소되어야 함).

### 2) 단위 테스트 회귀 검증 (EditMode Tests)
Unity Editor 환경에서 NUnit EditMode 테스트를 실행하거나, CLI를 통해 테스트가 손상되지 않았음을 확인합니다:
- `YachtGameRulesTests`
- `YachtQuestAugmentTests`
- `YachtEnhanceAugmentTests`
- `YachtModificationAugmentTests`
- `AugmentCardViewTests`
- `RerollCounterBarTests`
- **성공 기준**: 기존 통과 테스트(889개 이상) 전원 Pass.

### 3) 씬 참조 무결성 검증
- [Augmented Dice.unity](../../../Assets/Scenes/Augmented Dice.unity) 메인 씬을 에디터에서 열었을 때 `Missing (MonoBehaviour)` 또는 끊어진 직렬화 레퍼런스가 발생하지 않는지 확인.

---

## 5. 롤백 대비 및 주의사항 (Safeguards)

1. **Git 브랜치 보호**: 작업 전 반드시 작업용 별도 브랜치(예: `chore/dead-code-cleanup`)를 생성하여 진행할 것.
2. **Phase별 커밋**: Phase 1부터 Phase 4까지 한꺼번에 커밋하지 말고, 각 Phase 완료 및 빌드 통과 시 개별 커밋으로 분할할 것 (`chore: remove unused private helpers and CS0169 field`, `chore: remove abandoned RollOrb and Zodiac systems` 등).
3. **확장 메서드 클래스 보존**: `PresentationPhaseExtensions`는 클래스명이 직접 호출되지 않더라도 `phase.IsInteractive()`, `phase.HasCompletedRoll()` 확장 메서드로 광범위하게 쓰이므로 절대 삭제하지 말 것.
4. **에디터 툴 및 메뉴 메서드 보존**: `[ContextMenu]`, `[MenuItem]`, `[InitializeOnLoadMethod]` 특성이 붙은 메서드(`RegenerateTableSurfaces`, `RebuildScoreSheetMenuItem`, `GenerateMissingAssetsAfterReload` 등)는 C# 참조 카운트가 0이어도 Unity 에디터 인터페이스이므로 삭제하지 말 것.
