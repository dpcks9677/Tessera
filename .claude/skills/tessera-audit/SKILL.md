---
name: tessera-audit
description: 대상 파일·모듈·변경분을 Tessera 고유 규칙 렌즈로 병렬 감사한다. SOLID 준수, 아트 규칙, 직렬화 안전성, 아키텍처 경계, 테스트 공백을 본다. 코드는 수정하지 않고 보고만 한다.
argument-hint: [대상 - 파일/모듈/diff]
model: opus
effort: high
---

# 감사 오케스트레이션

감사 대상: `$ARGUMENTS`

당신은 오케스트레이터입니다. 렌즈별로 `tessera-reviewer` 를 **병렬로** 띄우고, 돌아온 지적을 중복 제거·정렬해 보고합니다. **코드를 고치지 않습니다.**

일반적인 정확성·단순화 리뷰는 이 스킬이 아니라 기존 `/code-review` 가 담당합니다. 이 스킬은 **이 프로젝트 고유 규칙 위반**만 봅니다. 두 개는 겹치지 않으니, 사용자가 범용 리뷰를 원하면 `/code-review` 를 안내하십시오.

## 1. 대상 확정

인자가 비어 있으면 변경분(`git status --short` 과 `git diff --stat`)을 대상으로 삼고 그 사실을 알립니다.

인자 형태별 해석:
- 파일 경로 → 그 파일
- 디렉터리 → 그 하위 전체
- `diff` 또는 `변경분` → 미커밋 변경 + 현재 브랜치가 `main` 대비 추가한 커밋
- 모듈명(`Tessera.Dice` 등) → 해당 네임스페이스 소속 파일

대상이 넓으면 좁히라고 요구하지 말고, 렌즈별 프롬프트에서 각자 우선순위를 두게 하십시오.

## 2. 렌즈 선택

대상에 **해당하는 렌즈만** 고릅니다. 셰이더만 바뀐 변경에 테스트 공백 렌즈를 돌리는 것은 낭비입니다. 보통 2~4개가 적당하고, 5개를 다 돌리는 경우는 드뭅니다.

선택한 렌즈와 그 이유를 사용자에게 한 줄로 알린 뒤 실행하십시오.

## 3. 렌즈 카탈로그

아래 명세를 **그대로 복사해** 각 `tessera-reviewer` 프롬프트에 실으십시오. 서브에이전트는 대화 이력을 물려받지 않습니다.

### 렌즈 A — SOLID 준수

`docs/plan/tracks/solid/README.md` 가 목표 구조를 정의합니다. 증강 시스템은 `IAugmentHandler` 와 발동 시점 인터페이스(`IOnAugmentSelected`, `IOnTurnStarted`, `IAfterScoreCommit`, `IOnTurnEnded`, `IBeforeScorePreview`, `IDiceCountModifier`, `IDiceLayoutProvider`, `IScoringDiceFilter`, `IScoreEnhancementModifier`, `ITurnDurationModifier`, `IManualActionAugment`)로 확장하게 돼 있습니다.

찾을 것: 증강 ID를 하드코딩한 `if`/`switch` 사다리, 신규 증강 추가 시 핸들러 밖 파일까지 고쳐야 하는 구조, 핸들러가 아닌 곳에 흩어진 증강별 분기, 단일 클래스가 규칙·표현·상태를 함께 들고 있는 지점.

주의: `YachtAugmentRuntime` 의 `Modification` 카테고리 공통 처리(`ResetFilledTarget`)는 **의도된 설계**입니다. `ModificationAugment` 의 XML 주석이 근거입니다. 이걸 위반으로 보고하지 마십시오.

### 렌즈 B — 아트 규칙

기준은 `.claude/rules/art-direction.md` 입니다. 먼저 읽고, 그 파일이 가리키는 원본(`docs/reference/art_style_guide.md`)의 값과 코드나 머티리얼에 박힌 색·강도 값을 대조합니다.

찾을 것: 팔레트 밖 하드코딩 색상, 규정 범위를 벗어난 라이트 강도, 그 규칙이 금지한 셰이더·머티리얼 설정.

### 렌즈 C — 직렬화 안전성

기준은 `.claude/rules/serialization.md` 입니다. 먼저 읽으십시오.

찾을 것: 그 규칙을 어긴 흔적, 그리고 프리팹 인스턴스의 오버라이드를 무효화할 수 있는 변경.

### 렌즈 D — 아키텍처 경계

기준은 `.claude/rules/module-boundary.md` 와 `docs/reference/control_manifest.md` 입니다. 먼저 읽으십시오. 이 경계는 컴파일러가 막지 못해 사람이 봐야만 잡힙니다(사유는 매니페스트 머리말).

찾을 것: 그 규칙을 어긴 의존, `Tessera.Core` 가 `Tessera.Rendering` 이나 Unity 표현 계층에 묶이는 지점, 폴더와 네임스페이스가 규칙의 대응과 어긋난 파일.

### 렌즈 E — 테스트 공백

기준은 `.claude/gates/TEST-COVERAGE.md` 의 점검 항목입니다. 먼저 읽고, 그 파일이 지목하는 `.claude/rules/editor-tests.md` 와 `docs/plan/process.md` §7도 읽으십시오. 감사에서는 판정 줄 없이 지적만 돌려줍니다.

## 4. 병렬 실행

선택한 렌즈 수만큼 `tessera-reviewer` 를 **한 번에** 띄웁니다. 각 프롬프트에 담을 것: 렌즈 명세 전문, 감사 대상의 구체적 파일 목록이나 범위, 이 변경이 무엇을 하려는 것인지의 배경.

**Unity REST를 쓰는 에이전트는 함께 띄우지 않습니다.** 리뷰어는 Unity에 접근하지 않으므로 리뷰어끼리는 안전합니다.

## 5. 통합

- **중복 제거.** 여러 렌즈가 같은 줄을 지적하면 하나로 합치고, 어느 렌즈들이 걸었는지 함께 적습니다. 두 렌즈가 동시에 잡은 지점은 신뢰도가 높습니다.
- **근거 검증.** 근거나 시나리오가 부실한 지적은 버립니다. 통과시키지 마십시오. 리뷰어가 규칙을 오독한 경우가 있으니, 판단이 서지 않으면 해당 파일의 그 구간만 직접 읽어 확인하십시오.
- **의도된 설계 걸러내기.** `.claude/gates/CODE-REVIEW.md` 「점검 항목」 끝 문장을 따릅니다.
- **정렬.** 치명 → 높음 → 중간 → 낮음.

## 6. 보고

한국어로 보고합니다.

- 어떤 대상을 어떤 렌즈로 봤는지 한 줄.
- 지적을 심각도 순으로. 각 항목에 위치, 근거, 시나리오.
- 여러 렌즈가 동시에 잡은 항목은 그 사실을 표시.
- 리뷰어가 확인하지 못한 영역.

**고치지 않습니다.** 사용자가 수정을 원하면 `/tessera-task` 로 넘기거나, 별도 지시를 받아 진행하십시오. 커밋하지 않습니다.
