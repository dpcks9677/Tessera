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
| **TEST-01** | **P1** | `FontFallbackTests.cs` 직렬화 API 버그 픽스 검증 및 커밋 | `Assets/Editor/Tests/FontFallbackTests.cs` | 최하 (테스트) | `TODO` |
| **DOC-01** | **P2** | 기술 문서 정합성 동기화 (`RollOrb` 폐기 반영 및 클린업 완료 기록) | `docs/agent/solid_refactoring_work_plan.md`, `docs/archive/plans/dead_code_cleanup_proposal.md` | 최하 (문서) | `DONE` |
| **SOLID-T05**| **P2** | `IYachtRuleSet` ISP 분리 (`SelectPresetFile` 뷰 결합 추출) | `Assets/Scripts/Games/Yacht/YachtGameCore.cs`, `LocalGameAuthority.cs` | 낮음 (구조) | `TODO` |
| **AUG-01** | **P3** | 증강 핸들러 내 불필요한 과도기 `GetOrSync` 동기화 헬퍼 정리 | `Assets/Scripts/Games/AugmentedYacht/Logic/Augments/...` (12개 파일) | 낮음 (단순화) | `TODO` |
| **PERF-01** | **P3** | `AugmentCardView` 증강 아이콘 로딩 캐시 및 GC 할당 완화 (`RES-2`) | `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentCardView.cs` | 낮음 (성능) | `TODO` |
| **SOLID-T07**| **P4** | `YachtScoreCalculator` 가변 주사위 및 범위 방어적 처리 | `Assets/Scripts/Games/Yacht/YachtGameCore.cs` | 낮음 (안정성) | `TODO` |
| **ARCH-01** | **P5** | Pure C# 게임 규칙 계층 독립 어셈블리 정의(`.asmdef`) 도입 가이드 | `Assets/Scripts/Games/Yacht/`, `AugmentedYacht/Logic/` | 중간 (구조) | `VOID` |
| **ARCH-02** | **P5** | 런타임 에셋을 기능 경계 기준으로 재편 (`Assets/GameContent/...`) | `Assets/Prefabs/`, `Assets/Resources/`, `Assets/Art/Generated/` | 중간 (구조) | `TODO` |
| **ARCH-03** | **P5** | `Resources/` 사용 축소와 Addressables 전환 검토 | `Assets/Resources/` | 중간 (구조) | `TODO` |
| **ARCH-04** | **P5** | `DicePresetCatalog`의 StreamingAssets 로드를 `UnityWebRequest` 대응으로 전환 | `Assets/Scripts/Dice/DicePresetCatalog.cs` | 낮음 (이식성) | `TODO` |
| **ARCH-05** | **P5** | asmdef 도입 (`Tessera.Core` → `Dice`/`Tabletop`/`Rendering` → `Games.Yacht` → `Games.AugmentedYacht`) | `Assets/Scripts/` 전체 | 중간 (구조) | `TODO` |
| **LOAD-01** | **P2** | 런타임 스크립트 6개의 에디터 전용 에셋 로딩(`#if UNITY_EDITOR` + `AssetDatabase.LoadAssetAtPath`)이 플레이어 빌드에서 null 반환 | `AugmentCardView.cs`, `AugmentedYachtController.cs`, `InkwellAndQuill.cs`, `ParchmentScoreSheet.cs`, `TabletopSurfaceBuilder.cs`, `YachtHudFactory.cs` | 높음 (빌드 결함) | `TODO` |
| **LOAD-02** | **P3** | `AugmentScrollModel.cs`의 죽은 `Resources.Load` 폴백 경로 정리 | `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentScrollModel.cs` | 낮음 (단순화) | `TODO` |

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
- [FontFallbackTests.cs](../../Assets/Editor/Tests/FontFallbackTests.cs) (Line 23 ~ Line 50)

#### 3. 작업 지침
1. 현재 수정 내역(`git diff Assets/Editor/Tests/FontFallbackTests.cs`)이 공개 API `importer.fontReferences`를 사용하는지 확인합니다.
2. `dotnet build Assembly-CSharp-Editor.csproj /p:WarningLevel=5`를 실행하여 컴파일 오류 및 경고 0개를 확인합니다.
3. 확인 후 해당 파일만 깔끔하게 커밋합니다 (`git add Assets/Editor/Tests/FontFallbackTests.cs`, 커밋 메시지: `fix(test): use public fontReferences API in FontFallbackTests`).

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

### [ARCH-01] Pure C# 게임 규칙 계층 어셈블리 정의(`.asmdef`) 도입 가이드 — `VOID`

**무효화: 2026-09-20.** asmdef 부분 도입을 전제로 한 항목이라 `ARCH-05`(프로젝트 전체 어셈블리 그래프 설계)로 흡수됨.

#### 1. 배경 및 목적 (원 명세, 참고용)
현재 `Tessera`는 모든 스크립트가 모놀리식 `Assembly-CSharp.dll`에 들어있어:
- 스크립트 하나 수정 시 전체 프로젝트가 재컴파일됩니다.
- 순수 C# 계층(`Tessera.Games.Yacht`)에 실수로 Unity API(`MonoBehaviour`, `Time` 등)가 침투하는 것을 컴파일 타임에 막을 수 없습니다.
- 800+개의 순수 규칙 유닛 테스트를 Unity 에디터 구동 없이 `dotnet test`로 즉시 돌리는 혜택을 누리지 못하고 있습니다.

#### 2. 권장 분리 구조 (원 명세, 참고용)
```text
Assets/Scripts/
├── Games/Yacht/                       -> [Tessera.Games.Yacht.Core.asmdef] (Unity 의존성 0)
│   ├── Logic/                         -> [Tessera.Games.AugmentedYacht.Logic.asmdef]
│   │   └── Augments/                  -> (Core만 참조, UnityEngine 미참조)
├── Tabletop/                          -> [Tessera.Tabletop.asmdef] (Core, Logic, URP 참조)
├── Presentation/                      -> [Tessera.Presentation.asmdef]
└── Rendering/                         -> [Tessera.Rendering.asmdef]
```

#### 3. 단계별 도입 절차 (원 명세, 참고용)
1. `Assets/Scripts/Games/Yacht/` 경로에 순수 C# 어셈블리 정의 생성:
   - `autoReferenced: true`, `noEngineReferences: true` 설정.
2. 컴파일을 수행하여 순수 규칙 계층에 엔진 참조가 실제로 0개인지 확인.
3. 테스트 어셈블리(`Assets/Editor/YachtTests.asmdef`)에서 해당 어셈블리만 참조하도록 구성.

---

### [ARCH-02] 런타임 에셋을 기능 경계 기준으로 재편

#### 1. 배경 및 목적
2026-09-19 아트·오디오 디렉터리 재편(커밋 `a2acacb`)과 2026-09-20 에디터 스크립트 기능별 폴더 분리에 이어지는 후속 후보입니다. 현재 `Assets/Prefabs`, `Assets/Resources`, `Assets/Art/Generated`는 타입(프리팹/리소스/생성물) 기준으로 나뉘어 있어, 특정 기능(예: 증강 요트)에 속한 에셋을 한눈에 찾기 어렵습니다.

#### 2. 목표 구조
```text
Assets/GameContent/
├── AugmentedYacht/
│   ├── Prefabs/
│   ├── Icons/
│   ├── Vfx/
│   └── Materials/
└── Shared/
    ├── Dice/
    └── Tabletop/
```

#### 3. 착수 조건
한 번에 옮기지 않습니다. 해당 기능을 수정할 때 그 기능의 에셋만 함께 옮기는 점진적 정리로 진행합니다. 번들링이나 Addressables 도입을 검토할 때(→ `ARCH-03`) 본격화합니다.

#### 4. 현재 상태
`Assets/Prefabs`, `Assets/Resources`, `Assets/Art/Generated`가 타입 기준으로 나뉘어 있습니다.

---

### [ARCH-03] `Resources/` 사용 축소와 Addressables 전환 검토

#### 1. 배경 및 목적
`Resources/`에 넣은 모든 에셋과 그 의존성은 실제 참조 여부와 무관하게 빌드에 포함되고, 문자열 경로 오탈자나 경로 누락은 컴파일 타임에 드러나지 않고 런타임에만 드러납니다.

#### 2. 착수 조건
현재는 아이콘 45개와 VFX 3개 규모로 허용 범위 안에 있습니다. 대형 프리팹이나 씬별 전용 에셋이 `Resources/`에 들어가기 시작하면 착수합니다.

#### 3. 대상 경로
- `Assets/Resources/`

---

### [ARCH-04] `DicePresetCatalog`의 StreamingAssets 로드를 `UnityWebRequest` 대응으로 전환

#### 1. 배경 및 목적
[DicePresetCatalog.cs](../../Assets/Scripts/Dice/DicePresetCatalog.cs)는 `Path.Combine(Application.streamingAssetsPath, "WebSource", "presets", ...)` 경로를 일반 파일 IO(`File.ReadAllText` 등)로 직접 읽습니다. Android와 WebGL에서는 StreamingAssets가 일반 파일 API로 읽을 수 없는 경로(APK 내부 압축, 웹 서버 경로)라 이 방식이 실패합니다.

#### 2. 착수 조건
Android나 WebGL을 목표 플랫폼에 넣을 때 착수합니다.

#### 3. 참고
효과음(Sfx)은 2026-09-19 재편에서 StreamingAssets 밖으로 이동해 이 문제에서 벗어났습니다. `DicePresetCatalog`의 프리셋 JSON만 남은 대상입니다.

---

### [ARCH-05] asmdef 도입

#### 1. 배경 및 목적
프로젝트에 `.asmdef`가 하나도 없어 런타임 스크립트 전부가 `Assembly-CSharp`, `Assets/Editor` 전부가 `Assembly-CSharp-Editor`로 컴파일됩니다.
- 스크립트 하나 수정 시 전체 프로젝트가 재컴파일됩니다.
- 순수 C# 계층(`Tessera.Games.Yacht`)에 실수로 Unity API(`MonoBehaviour`, `Time` 등)가 침투하는 것을 컴파일 타임에 막을 수 없습니다.
- 800+개의 순수 규칙 유닛 테스트를 Unity 에디터 구동 없이 `dotnet test`로 즉시 돌리는 혜택을 누리지 못하고 있습니다.

`ARCH-01`(게임 규칙 계층 한 겹만 다루는 asmdef 도입 가이드)을 이 태스크로 흡수했습니다. asmdef는 부분 도입하면 참조 방향이 꼬이기 쉬워, 프로젝트 전체 어셈블리 그래프를 한 번에 설계하는 것이 평가 권고입니다.

#### 2. 목표 의존 순서
```text
Tessera.Core → Dice / Tabletop / Rendering → Games.Yacht → Games.AugmentedYacht
```

`ARCH-01`이 제시했던 게임 규칙 계층 내부 분리 구조는 위 순서 중 `Games.Yacht` / `Games.AugmentedYacht` 계층의 세부안으로 다음과 같이 참고합니다:
```text
Assets/Scripts/
├── Games/Yacht/                       -> [Tessera.Games.Yacht.Core.asmdef] (Unity 의존성 0)
│   ├── Logic/                         -> [Tessera.Games.AugmentedYacht.Logic.asmdef]
│   │   └── Augments/                  -> (Core만 참조, UnityEngine 미참조)
├── Tabletop/                          -> [Tessera.Tabletop.asmdef] (Core, Logic, URP 참조)
├── Presentation/                      -> [Tessera.Presentation.asmdef]
└── Rendering/                         -> [Tessera.Rendering.asmdef]
```

#### 3. 착수 조건
컴파일 대기가 체감되거나 참여 인원이 늘 때 착수합니다. 부분 도입하지 않고 테스트·에디터 어셈블리까지 한 번에 설계합니다.

#### 4. 현재 상태
프로젝트에 `.asmdef`가 하나도 없고, 런타임 스크립트 전부가 `Assembly-CSharp`, `Assets/Editor` 전부가 `Assembly-CSharp-Editor`입니다.

---

### [LOAD-01] 런타임 스크립트 6개의 에디터 전용 에셋 로딩이 플레이어 빌드에서 null 반환

#### 1. 배경 및 목적
아래 6개 런타임 스크립트가 텍스처·폰트·모델을 `#if UNITY_EDITOR` 블록 안 `AssetDatabase.LoadAssetAtPath`로만 로드합니다. `AssetDatabase`는 에디터 전용 API라 플레이어 빌드에서는 해당 블록 자체가 컴파일되지 않거나 호출부가 null을 반환해, 실제 빌드에서 이 에셋들이 전부 null이 됩니다. 2026-09-19 재편 작업 중 발견됐습니다.

#### 2. 대상 파일
- `AugmentCardView.cs`
- `AugmentedYachtController.cs`
- `InkwellAndQuill.cs`
- `ParchmentScoreSheet.cs`
- `TabletopSurfaceBuilder.cs`
- `YachtHudFactory.cs`

#### 3. 작업 지침
직렬화 필드(`[SerializeField]`)로 에셋 참조를 노출하거나, `Resources.Load` 등 빌드에 포함되는 다른 로딩 경로로 전환이 필요합니다. 6개 파일 각각의 현재 로딩 지점과 대체 방식은 착수 시 개별 조사가 필요합니다 (미확인).

#### 4. 검증 절차
- 플레이어 빌드(또는 빌드에 준하는 환경)에서 해당 에셋들이 null이 아닌지 확인.

---

### [LOAD-02] `AugmentScrollModel.cs`의 죽은 `Resources.Load` 폴백 경로 정리

#### 1. 배경 및 목적
`Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentScrollModel.cs:438`의 `Resources.Load<Texture2D>("Parchment/parchment_base")` 폴백은 해당 경로가 `Resources/` 밑에 존재하지 않아 항상 null을 반환하는 죽은 코드입니다.

#### 2. 대상 파일
- [AugmentScrollModel.cs](../../Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentScrollModel.cs) (Line 438)

#### 3. 작업 지침
폴백 경로를 제거하거나, 의도한 실제 에셋 경로로 교정합니다. 어느 쪽이 맞는지는 착수 시 확인 필요(미확인).

#### 4. 검증 절차
```powershell
dotnet build Assembly-CSharp.csproj /p:WarningLevel=5
```
- **성공 기준**: 컴파일 오류 0개.

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
