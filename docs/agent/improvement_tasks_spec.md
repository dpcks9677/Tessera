# Tessera 프로젝트 내부 개선 작업 명세서 (AI 실행용)

> **문서 종류**: 작업 지시서 (AI 대상)
> **코드 대조 기준**: 2026-09-13 · 커밋 `8cc0273`
> 이 문서는 위 시점의 코드에서 확인된 것만 기술합니다. 계획 항목은 상태 표기로 구분합니다.
> 작성 원칙은 [`docs/README.md`](../README.md)를 보십시오.
>
> **문서 상태**: 준비 완료 (Ready for Implementation)  
> **기준 일자**: 2026-09-13  
> **대상 저장소**: Tessera (`c:\Users\dpcks\Unity\Tessera`)  
> **목적**: 다른 AI 코딩 에이전트 또는 엔지니어가 독립적으로 각 태스크를 읽고 코드를 직접 수정·검증할 수 있도록 정의된 실행 기준 명세서.

---

## 1. AI 에이전트 작업 원칙 (Crucial Rules)

작업을 수행하는 AI 에이전트는 다음 수칙을 반드시 준수해야 합니다:

1. **단일 태스크 단위 수행 (One Task at a Time)**:
   - 한 번에 여러 태스크를 동시에 수정하지 않습니다. 반드시 하나의 작업 ID(예: `BUILD-01`)만 선택하여 진행합니다.
   - 작업 시작 전 상태를 `DOING`으로 인지하고, 수정 후 검증을 통과한 뒤 `DONE`으로 마감합니다.
2. **외과수술적 변경 (Surgical Changes)**:
   - 지정된 파일과 관련된 메서드/라인만 정확히 수정합니다. 인접한 무관한 코드를 재포맷하거나 임의로 리팩터링하지 않습니다.
   - Microsoft C#/.NET 규약과 [`docs/reference/coding_conventions.md`](../reference/coding_conventions.md)를 준수합니다.
3. **식별자는 영문 (English Identifiers)**:
   - 클래스, 메서드, 프로퍼티, 필드, 지역 변수, **테스트 메서드명까지 반드시 영문으로만** 작성합니다. (한글 식별자 절대 금지)
   - 한국어 맥락은 주석과 커밋 메시지, 문서에만 기술합니다.
4. **Unity 직렬화 및 씬 무결성 절대 보존 (Zero Scene Breakage)**:
   - `MonoBehaviour` 컴포넌트의 기존 `[SerializeField]` 필드명이나 타입을 변경하면 Unity 씬 및 프리팹 데이터가 유실(Missing Reference)됩니다.
5. **회귀 방지 및 단위 테스트 통과**:
   - 각 태스크 완료 후 반드시 CLI 컴파일(`dotnet build`)과 관련 NUnit EditMode 테스트를 실행하여 회귀가 없음을 증명합니다.

---

## 2. 작업 상태 종합 요약표

| 작업 ID | 우선순위 | 작업명 | 주요 대상 파일 | 예상 영향도 | 상태 |
|:---:|:---:|---|---|:---:|:---:|
| **BUILD-01** | **P1** | `PixelEdgeRendererFeature.cs` 컴파일 경고 해소 (CS0672, CS0618) | `Assets/Scripts/Rendering/PixelEdgeRendererFeature.cs` | 최하 (안전) | `TODO` |
| **TEST-01** | **P1** | `FontFallbackTests.cs` 직렬화 API 버그 픽스 검증 및 커밋 | `Assets/Editor/FontFallbackTests.cs` | 최하 (테스트) | `TODO` |
| **DOC-01** | **P2** | 기술 문서 정합성 동기화 (`RollOrb` 폐기 반영 및 클린업 완료 기록) | `docs/agent/solid_refactoring_work_plan.md`, `docs/archive/plans/dead_code_cleanup_proposal.md` | 최하 (문서) | `DONE` |
| **SOLID-T05**| **P2** | `IYachtRuleSet` ISP 분리 (`SelectPresetFile` 뷰 결합 추출) | `Assets/Scripts/Games/Yacht/YachtGameCore.cs`, `LocalGameAuthority.cs` | 낮음 (구조) | `TODO` |
| **AUG-01** | **P3** | 증강 핸들러 내 불필요한 과도기 `GetOrSync` 동기화 헬퍼 정리 | `Assets/Scripts/Games/AugmentedYacht/Logic/Augments/...` (12개 파일) | 낮음 (단순화) | `TODO` |
| **PERF-01** | **P3** | `AugmentCardView` 증강 아이콘 로딩 캐시 및 GC 할당 완화 (`RES-2`) | `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentCardView.cs` | 낮음 (성능) | `TODO` |
| **SOLID-T07**| **P4** | `YachtScoreCalculator` 가변 주사위 및 범위 방어적 처리 | `Assets/Scripts/Games/Yacht/YachtGameCore.cs` | 낮음 (안정성) | `TODO` |
| **ARCH-01** | **P5** | Pure C# 게임 규칙 계층 독립 어셈블리 정의(`.asmdef`) 도입 가이드 | `Assets/Scripts/Games/Yacht/`, `AugmentedYacht/Logic/` | 중간 (구조) | `TODO` |

---

## 3. 태스크별 상세 실행 명세서 (Step-by-Step Implementation Guide)

---

### [BUILD-01] `PixelEdgeRendererFeature.cs` 컴파일 경고 해소 (CS0672, CS0618)

#### 1. 배경 및 목적
Unity 6 URP에서 Render Graph 파이프라인이 기본 활성화되면서, 비 Render Graph(호환 모드/Compatibility Mode)용 레거시 API 재정의에 대해 3건의 경고(`CS0672`, `CS0618`)가 발생합니다. 이로 인해 전체 솔루션이 'Clean Build (경고 0개)' 상태를 달성하지 못하고 있습니다.

#### 2. 대상 파일
- [PixelEdgeRendererFeature.cs](../../Assets/Scripts/Rendering/PixelEdgeRendererFeature.cs) (Line 166 ~ Line 190)

#### 3. 작업 지침
호환 모드 메서드 블록 앞뒤에 `#pragma warning disable CS0618, CS0672`와 `#pragma warning restore CS0618, CS0672`를 감싸거나, Unity 권장대로 `[System.Obsolete]`를 부여합니다.

```csharp
<<<< BEFORE (Line 166-189)
            // 아래 두 메서드는 호환 모드(Render Graph 비활성)에서만 쓰인다.
            // 설정을 되돌렸을 때 기능이 조용히 사라지지 않도록 남겨 둔다.
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                RenderingUtils.ReAllocateHandleIfNeeded(ref temporaryColor, descriptor, FilterMode.Point,
                    TextureWrapMode.Clamp, name: "_PixelEdgeTemp");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null || temporaryColor == null) return;

                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                if (source == null) return;

                CommandBuffer cmd = CommandBufferPool.Get(ProfilerTag);
                Blitter.BlitCameraTexture(cmd, source, temporaryColor, material, 0);
                Blitter.BlitCameraTexture(cmd, temporaryColor, source);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
====
>>>> AFTER
            // 아래 두 메서드는 호환 모드(Render Graph 비활성)에서만 쓰인다.
            // 설정을 되돌렸을 때 기능이 조용히 사라지지 않도록 남겨 둔다.
            #pragma warning disable CS0618, CS0672
            [System.Obsolete]
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                RenderingUtils.ReAllocateHandleIfNeeded(ref temporaryColor, descriptor, FilterMode.Point,
                    TextureWrapMode.Clamp, name: "_PixelEdgeTemp");
            }

            [System.Obsolete]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null || temporaryColor == null) return;

                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                if (source == null) return;

                CommandBuffer cmd = CommandBufferPool.Get(ProfilerTag);
                Blitter.BlitCameraTexture(cmd, source, temporaryColor, material, 0);
                Blitter.BlitCameraTexture(cmd, temporaryColor, source);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
            #pragma warning restore CS0618, CS0672
<<<<
```

#### 4. 검증 절차
```powershell
dotnet build Assembly-CSharp.csproj /p:WarningLevel=5
```
- **성공 기준**: `경고 0개`, `오류 0개`.

---

### [TEST-01] `FontFallbackTests.cs` 직렬화 API 버그 픽스 검증 및 커밋

#### 1. 배경 및 목적
기존 `FontFallbackTests.cs`는 `TrueTypeFontImporter`의 내부 직렬화 프로퍼티 `fallbackFontReferences`를 `SerializedObject`로 탐색했으나, Unity 버전 변경으로 해당 직렬화 프로퍼티명이 내부에서 달라져 항상 null을 반환하여 2건의 테스트가 실패하고 있었습니다. 현재 워킹 트리에 공개 API인 `importer.fontReferences`를 사용하도록 정교하게 수정되어 있습니다.

#### 2. 대상 파일
- [FontFallbackTests.cs](../../Assets/Editor/FontFallbackTests.cs) (Line 23 ~ Line 50)

#### 3. 작업 지침
1. 현재 수정 내역(`git diff Assets/Editor/FontFallbackTests.cs`)이 공개 API `importer.fontReferences`를 사용하는지 확인합니다.
2. `dotnet build Assembly-CSharp-Editor.csproj /p:WarningLevel=5`를 실행하여 컴파일 오류 및 경고 0개를 확인합니다.
3. 확인 후 해당 파일만 깔끔하게 커밋합니다 (`git add Assets/Editor/FontFallbackTests.cs`, 커밋 메시지: `fix(test): use public fontReferences API in FontFallbackTests`).

#### 4. 검증 절차
```powershell
dotnet build Assembly-CSharp-Editor.csproj /p:WarningLevel=5
```
- **성공 기준**: 컴파일 오류 0개, 경고 0개.

---

### [DOC-01] 기술 문서 정합성 동기화 (`RollOrb` 폐기 및 클린업 반영) — `DONE`

**완료: 2026-09-13.** docs 폴더 전체 정리 작업에 흡수되어 처리됐습니다.

#### 1. 배경 및 목적

커밋 `53fc78e`에서 레거시 프롭 `RollOrb.cs`가 삭제됐습니다. 그러나 리팩토링 계획서에
`SOLID-T12`(RollOrb 지오메트리 빌더 분리)가 `TODO`로 남아 혼선을 줄 수 있었고,
`dead_code_cleanup_proposal.md`는 실행이 끝났는데도 "제안" 상태에 untracked로 남아 있었습니다.

> **원 명세의 사실 오류 정정**: 이 태스크는 `ZodiacConstellationData.cs`도 "완전히 삭제되었다"고
> 적었으나 **사실이 아닙니다.** 해당 파일은 `Assets/Scripts/Tabletop/ZodiacConstellationData.cs`에
> 520줄 그대로 있고, `RollCosmicCube.cs`와 `AugmentedYachtController.cs`가 여전히 참조합니다.
> 황도 12궁 연출은 폐기된 기능이지만 코드는 남기고 게임에 적재만 하지 않는 상태입니다
> (`EnabledInGame = false`). 삭제된 것은 `RollOrb.cs` 하나뿐입니다.

#### 2. 대상 파일

- [solid_refactoring_work_plan.md](solid_refactoring_work_plan.md)
- [dead_code_cleanup_proposal.md](../archive/plans/dead_code_cleanup_proposal.md)

#### 3. 실제로 한 일

1. `solid_refactoring_work_plan.md`의 `SOLID-T12` 상태를 **`VOID`**로 바꾸고 무효 사유와 날짜를
   남겼습니다. 행은 지우지 않았습니다.
   - 원 명세는 `OBSOLETE`를 제안했으나, 상태 어휘는 `work_plan.md` §2 「상태 표기 규칙」에 정의된
     것만 씁니다. 그 표에 `VOID`(대상이 사라져 태스크가 무효)와 `DROPPED`(작업 자체를 폐기)를
     새로 등록하고 이쪽을 썼습니다.
   - 같은 문서의 다른 낡은 표기도 함께 고쳤습니다. `ParchmentScoreSheet.cs` 경로와 LOC(1,480 → 1,647),
     `YachtTurnFlowPresenter` LOC(840 → 913), 존재하지 않는 `Logic/Augments/Handlers/` 경로.
2. `dead_code_cleanup_proposal.md`를 `docs/archive/plans/`로 옮기고 git 추적에 포함시켰습니다.
   상단에 `실행 완료` 표기와 함께 **실행 결과와의 차이**를 명시했습니다 (Phase 3의 Zodiac 삭제 미수행,
   Phase 4의 `TutorialInfo` 삭제 완료).

---

### [SOLID-T05] `IYachtRuleSet` ISP 분리 (`SelectPresetFile` 뷰 결합 추출)

#### 1. 배경 및 목적 (ISP 위반 해소)
순수 도메인 게임 규칙 인터페이스인 `IYachtRuleSet`에 애니메이션 프리셋 에셋 파일명(`dice_presets_normal_5.json`)을 반환하는 `SelectPresetFile` 메서드가 포함되어 있습니다. 이는 규칙 계층이 프레젠테이션/에셋 명명 규칙을 강제로 알게 만드는 인터페이스 분리 원칙(ISP) 위반입니다.

#### 2. 대상 파일
- [YachtGameCore.cs](../../Assets/Scripts/Games/Yacht/YachtGameCore.cs) (Line 393 ~ Line 435)
- [LocalGameAuthority.cs](../../Assets/Scripts/Games/Yacht/LocalGameAuthority.cs) (Line 184 ~ Line 185)

#### 3. 작업 지침
1. **[YachtGameCore.cs](../../Assets/Scripts/Games/Yacht/YachtGameCore.cs)**:
   - `IYachtRuleSet`에서 `string SelectPresetFile(IReadOnlyList<YachtDieState> dice);` 선언을 제거합니다.
   - `NormalYachtRuleSet` 및 `AugmentedYachtRuleSet`의 `SelectPresetFile` 메서드 구현을 제거합니다.
   - 프리셋 선택 책임을 전담하는 정적 유틸리티 클래스 `YachtPresetSelector`를 `Tessera.Games.Yacht` 네임스페이스에 선언합니다:
     ```csharp
     public static class YachtPresetSelector
     {
         public static string SelectNormalPresetFile(int diceCount) => $"dice_presets_normal_{diceCount}.json";
     }
     ```
2. **[LocalGameAuthority.cs](../../Assets/Scripts/Games/Yacht/LocalGameAuthority.cs)**:
   - Line 185의 `rules.SelectPresetFile(state.Dice)` 호출을 `YachtPresetSelector.SelectNormalPresetFile(state.Dice.Length)`로 변경합니다.

```csharp
<<<< BEFORE (YachtGameCore.cs Line 393-400)
    public interface IYachtRuleSet
    {
        YachtGameMode Mode { get; }
        YachtDieState[] CreateInitialDice(int diceCount);
        int RollValue(YachtDieState die, IRandomSource random);
        Dictionary<ScoreCategory, int> CalculateScores(IReadOnlyList<YachtDieState> dice);
        string SelectPresetFile(IReadOnlyList<YachtDieState> dice);
    }
====
>>>> AFTER
    public interface IYachtRuleSet
    {
        YachtGameMode Mode { get; }
        YachtDieState[] CreateInitialDice(int diceCount);
        int RollValue(YachtDieState die, IRandomSource random);
        Dictionary<ScoreCategory, int> CalculateScores(IReadOnlyList<YachtDieState> dice);
    }
<<<<
```

```csharp
<<<< BEFORE (LocalGameAuthority.cs Line 183-185)
                PresetFile = state.Mode == YachtGameMode.Augmented
                    ? augmentRuntime.SelectPresetFile(state.Dice, tableFlip)
                    : rules.SelectPresetFile(state.Dice),
====
>>>> AFTER
                PresetFile = state.Mode == YachtGameMode.Augmented
                    ? augmentRuntime.SelectPresetFile(state.Dice, tableFlip)
                    : YachtPresetSelector.SelectNormalPresetFile(state.Dice.Length),
<<<<
```

#### 4. 검증 절차
```powershell
dotnet build Assembly-CSharp.csproj /p:WarningLevel=5
dotnet build Assembly-CSharp-Editor.csproj /p:WarningLevel=5
```
- **성공 기준**: 컴파일 오류 0개, 경고 0개. `YachtGameRulesTests` 전체 통과.

---

### [AUG-01] 증강 핸들러 내 불필요한 과도기 `GetOrSync` 동기화 헬퍼 정리

#### 1. 배경 및 목적
증강 상태 저장소가 `AugmentStateStore`(`IAugmentState`)로 개편되는 과정에서, 개별 증강 클래스에 `GetOrSync`라는 과도기 헬퍼가 다수 생성되었습니다.
예를 들어 [PiggyBank.cs](../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Enhance/PiggyBank.cs)를 보면:
```csharp
private static PiggyBankState GetOrSync(AugmentContext context)
{
    var state = context.State<PiggyBankState>();
    if (state.Balance == 0 && context.Player.PiggyBankBalance > 0)
    {
        state.Balance = context.Player.PiggyBankBalance;
    }
    return state;
}
```
하지만 `context.Player.PiggyBankBalance`는 이미 내부에서 `States.GetOrCreate<PiggyBankState>().Balance`를 직접 반환하므로, `state`와 `context.Player.PiggyBankBalance`는 완전히 동일한 인스턴스의 동일한 필드입니다. 즉 `GetOrSync`는 불필요한 중복 코드이자 자기 대입입니다.

#### 2. 대상 파일
- [PiggyBank.cs](../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Enhance/PiggyBank.cs)
- [CautiousStraight.cs](../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Quest/CautiousStraight.cs)
- [Copycat.cs](../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Quest/Copycat.cs)
- [Doubling.cs](../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Quest/Doubling.cs)
- [EveryLittleCounts.cs](../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Quest/EveryLittleCounts.cs)
- [FastStraight.cs](../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Quest/FastStraight.cs)
- [Holdout.cs](../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Quest/Holdout.cs)
- [NoTimeToWaste.cs](../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Quest/NoTimeToWaste.cs)
- [Nozdormu.cs](../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Quest/Nozdormu.cs)
- [Prophet.cs](../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Quest/Prophet.cs)
- [StepByStep.cs](../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Quest/StepByStep.cs)
- [BountyHunter.cs](../../Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Quest/BountyHunter.cs)

#### 3. 작업 지침
각 파일에서:
1. 불필요한 `private static TState GetOrSync(AugmentContext context)` 메서드를 삭제합니다.
2. 호출부에서 `GetOrSync(context)` 대신 `context.State<TState>()`를 직접 호출하도록 교체합니다.
3. 메서드 종료 시 `context.Player.Field = state.Field;`와 같은 역방향 자기 대입문이 있다면 제거합니다 (동일 참조이므로 불필요).

```csharp
<<<< BEFORE (PiggyBank.cs Line 25-47)
        private static PiggyBankState GetOrSync(AugmentContext context)
        {
            var state = context.State<PiggyBankState>();
            if (state.Balance == 0 && context.Player.PiggyBankBalance > 0)
            {
                state.Balance = context.Player.PiggyBankBalance;
            }
            return state;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            state.Balance += context.Game.RollsRemaining * RollReward;

            while (state.Balance >= PayoutThreshold)
            {
                state.Balance -= PayoutThreshold;
                context.AddBonus(PayoutThreshold, $"저금통: +{PayoutThreshold}점");
            }

            context.Player.PiggyBankBalance = state.Balance;
        }
====
>>>> AFTER
        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = context.State<PiggyBankState>();
            state.Balance += context.Game.RollsRemaining * RollReward;

            while (state.Balance >= PayoutThreshold)
            {
                state.Balance -= PayoutThreshold;
                context.AddBonus(PayoutThreshold, $"저금통: +{PayoutThreshold}점");
            }
        }
<<<<
```

#### 4. 검증 절차
```powershell
dotnet build Assembly-CSharp.csproj /p:WarningLevel=5
```
- **성공 기준**: 컴파일 오류 0개. `YachtQuestAugmentTests`, `YachtEnhanceAugmentTests` 전체 테스트 통과.

---

### [PERF-01] `AugmentCardView` 증강 아이콘 로딩 캐시 및 GC 할당 완화 (`RES-2`)

#### 1. 배경 및 목적
[AugmentCardView.cs](../../Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentCardView.cs)의 `Bind` 메서드는 호출될 때마다 `Resources.Load<Sprite>($"AugmentIcons/{definition.Id}")`를 수행합니다. 문자열 보간(`$"..."`)으로 인해 매 바인딩 시 GC 가비지가 생성됩니다. 반면 `YachtTurnFlowPresenter.cs`는 딕셔너리로 아이콘을 캐시하고 있어 구현 일관성이 결여되어 있습니다.

#### 2. 대상 파일
- [AugmentCardView.cs](../../Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentCardView.cs) (Line 94)

#### 3. 작업 지침
`AugmentCardView` 클래스 내부에 정적 아이콘 캐시(`Dictionary<string, Sprite>`)를 두거나, 캐시 헬퍼 메서드를 통해 이미 로드된 스프라이트를 재사용하도록 개선합니다.

```csharp
// AugmentCardView.cs 내부에 정적 캐시 선언
private static readonly Dictionary<string, Sprite> IconCache = new();

public static Sprite LoadAugmentIcon(string augmentId)
{
    if (string.IsNullOrEmpty(augmentId)) return null;
    if (!IconCache.TryGetValue(augmentId, out Sprite sprite))
    {
        sprite = Resources.Load<Sprite>($"AugmentIcons/{augmentId}");
        IconCache[augmentId] = sprite;
    }
    return sprite;
}
```
`Bind()` 메서드 내부에서는:
```csharp
Sprite augmentIcon = overrideIcon ?? LoadAugmentIcon(definition.Id);
```
로 변경합니다.

#### 4. 검증 절차
```powershell
dotnet build Assembly-CSharp.csproj /p:WarningLevel=5
```
- **성공 기준**: 컴파일 오류 0개. `AugmentCardViewTests` 전체 통과.

---

### [SOLID-T07] `YachtScoreCalculator` 가변 주사위 및 눈금 범위 방어적 처리

#### 1. 배경 및 목적
[YachtGameCore.cs#L464](../../Assets/Scripts/Games/Yacht/YachtGameCore.cs#L464)의 `YachtScoreCalculator.Calculate(IReadOnlyList<int> dice)`는:
1. `dice.Count != 5`이면 `ArgumentException`을 던집니다.
2. `value < 1 || value > 6`이면 `ArgumentOutOfRangeException`을 던집니다.
증강 요트에는 8면 주사위(눈 1~8) 및 향후 가변 주사위 개수가 도입될 수 있습니다. `Calculate` 메서드가 예외로 프로세스를 중단시키지 않고 방어적으로 동작하도록 개선합니다.

#### 2. 대상 파일
- [YachtGameCore.cs](../../Assets/Scripts/Games/Yacht/YachtGameCore.cs) (Line 462 ~ Line 500)

#### 3. 작업 지침
- `dice == null`일 경우에만 빈 딕셔너리 또는 기존 예외를 반환하고, 주사위 개수가 5개가 아니더라도 가용 주사위 목록 기준으로 점수를 산출하도록 허용합니다.
- 주사위 눈금이 1~6 범위를 벗어날 경우(예: 8면체의 7, 8):
  - 카운팅 배열 크기를 확장(`int[] counts = new int[9];`)하거나 딕셔너리를 사용하여 예외 없이 총합(Choice, Yacht 등)에 안전하게 반영되도록 합니다.
  - 상단 족보(Aces~Sixes)는 1~6만 합산하고, 7 이상의 눈은 상단 카테고리에 합산되지 않으나 `Choice` 총합에는 합산되도록 방어 처리합니다.

#### 4. 검증 절차
- `YachtGameRulesTests.cs` 기존 족보 계산 테스트 전체 통과 확인.
- 8면 주사위(눈 7, 8) 및 가변 주사위(4개, 6개) 입력에 대한 신규 단위 테스트 추가 검증.

---

### [ARCH-01] Pure C# 게임 규칙 계층 어셈블리 정의(`.asmdef`) 도입 가이드

#### 1. 배경 및 목적
현재 `Tessera`는 모든 스크립트가 모놀리식 `Assembly-CSharp.dll`에 들어있어:
- 스크립트 하나 수정 시 전체 프로젝트가 재컴파일됩니다.
- 순수 C# 계층(`Tessera.Games.Yacht`)에 실수로 Unity API(`MonoBehaviour`, `Time` 등)가 침투하는 것을 컴파일 타임에 막을 수 없습니다.
- 800+개의 순수 규칙 유닛 테스트를 Unity 에디터 구동 없이 `dotnet test`로 즉시 돌리는 혜택을 누리지 못하고 있습니다.

#### 2. 권장 분리 구조
```text
Assets/Scripts/
├── Games/Yacht/                       -> [Tessera.Games.Yacht.Core.asmdef] (Unity 의존성 0)
│   ├── Logic/                         -> [Tessera.Games.AugmentedYacht.Logic.asmdef]
│   │   └── Augments/                  -> (Core만 참조, UnityEngine 미참조)
├── Tabletop/                          -> [Tessera.Tabletop.asmdef] (Core, Logic, URP 참조)
├── Presentation/                      -> [Tessera.Presentation.asmdef]
└── Rendering/                         -> [Tessera.Rendering.asmdef]
```

#### 3. 단계별 도입 절차
1. `Assets/Scripts/Games/Yacht/` 경로에 순수 C# 어셈블리 정의 생성:
   - `autoReferenced: true`, `noEngineReferences: true` 설정.
2. 컴파일을 수행하여 순수 규칙 계층에 엔진 참조가 실제로 0개인지 확인.
3. 테스트 어셈블리(`Assets/Editor/YachtTests.asmdef`)에서 해당 어셈블리만 참조하도록 구성.

---

## 4. 통합 검증 및 체크리스트 (Verification Checklist)

모든 작업 완료 후 에이전트는 다음을 반드시 검증해야 합니다:

- [ ] **빌드 경고 0개 달성**:
  ```powershell
  dotnet build Assembly-CSharp.csproj /p:WarningLevel=5
  dotnet build Assembly-CSharp-Editor.csproj /p:WarningLevel=5
  ```
- [ ] **기존 단위 테스트 100% Pass**:
  - `YachtGameRulesTests`
  - `YachtQuestAugmentTests`
  - `YachtEnhanceAugmentTests`
  - `YachtModificationAugmentTests`
  - `AugmentCardViewTests`
  - `FontFallbackTests`
- [ ] **메인 씬 무결성**:
  - `Assets/Scenes/Augmented Dice.unity` 열었을 때 `Missing (MonoBehaviour)` 또는 레퍼런스 유실이 없을 것.
