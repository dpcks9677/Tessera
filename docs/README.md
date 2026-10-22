# Tessera 문서 색인

이 폴더의 문서는 **독자 기준으로 세 갈래**입니다. 무엇을 읽어야 할지는 지금 하려는 일로 정해집니다.

| 디렉터리 | 독자 | 성격 |
|---|---|---|
| [`reference/`](reference/) | 사람 | 프로젝트의 현재 모습을 설명하는 기준 문서. 코드가 바뀌면 함께 고칩니다. |
| [`plan/`](plan/) | AI 에이전트 | 무엇을 다음에 할지 지시하는 작업 계획서. 상태 표기로 진행을 추적합니다. 지금 위치는 [`STATUS.md`](STATUS.md), 결정 기록은 [`decisions.md`](decisions.md)에 있습니다. |
| [`archive/`](archive/) | 참고 | 끝났거나 폐기된 것. 이력 보존용이며 실행 지침으로 쓰지 않습니다. |

---

## 무엇부터 읽을까

**이 프로젝트에 처음 왔다면**
1. 루트 [`README.md`](../README.md) — 게임이 무엇인지, 어떻게 실행하는지
2. [`reference/architecture_overview.md`](reference/architecture_overview.md) — 디렉터리 구조, 씬 계층, 데이터 흐름, 자주 밟는 함정
3. [`reference/augments_specification_and_status.md`](reference/augments_specification_and_status.md) — 게임의 핵심인 증강 시스템. 디자인 패턴마다 실제 코드가 실려 있습니다

**코드를 고치기 전에**
- [`reference/coding_conventions.md`](reference/coding_conventions.md) — C# 규약과 Unity 직렬화 예외
- [`reference/architecture_decisions.md`](reference/architecture_decisions.md) — 구조 판단의 ADR
- [`reference/control_manifest.md`](reference/control_manifest.md) — 레이어별 해야 할 것·하지 말 것
- [`reference/art_style_guide.md`](reference/art_style_guide.md) — 색·조명·톤앤매너

**AI 에이전트로 작업한다면**
- [`STATUS.md`](STATUS.md)부터 읽습니다. 지금 무엇을 하는 중이고 다음 행동이 무엇인지 말해 줍니다
- [`decisions.md`](decisions.md)와 각 폴더의 `log.md`는 **필요할 때만** 엽니다. 과거 기록이라 컨텍스트를 크게 먹습니다
- 태스크 ID로 문서를 찾습니다: `Mnn-Tnn` → `plan/mNN/README.md`, `SOLID-Tnn` → `plan/tracks/solid/README.md`, `BUILD-nn`·`TEST-nn`·`AUG-nn`·`PERF-nn`·`ARCH-nn`·`DOC-nn`·`LOAD-nn` → `plan/tracks/improvement/README.md`

---

## 문서 작성 원칙

이 색인이 있는 이유의 절반은 아래 원칙입니다. 2026-09 정리 이전의 문서에는 존재하지 않는 타입,
없는 메서드, 실제와 하나도 겹치지 않는 씬 오브젝트 이름, 전부 틀린 에디터 메뉴 경로가 현재형으로
적혀 있었습니다. 그런 문서는 사람을 잘못 이끄는 데 그치지 않습니다. AI 에이전트가 그것을 사실로 읽고
존재하지 않는 API를 호출하는 코드를 씁니다.

1. **실재를 확인한 뒤에 적습니다.** 문서에 쓰는 클래스명, 메서드 시그니처, 파일 경로, 에셋 경로, 메뉴
   경로, 수치는 코드에서 직접 확인한 것만 적습니다. 기억이나 추론으로 채우지 않습니다. 확인하지 못한
   것은 그럴듯하게 적는 대신 `미확인`으로 남기고 그 사실을 보고합니다.
2. **계획과 구현을 구분합니다.** 아직 없는 것을 현재형으로 서술하지 않습니다. 계획 항목에는 상태
   표기(`TODO` / `DOING` / `DONE` / `DEFERRED` / `DROPPED` / `VOID`)를 붙이고, 계획서의 "신규 파일"은
   그 파일이 아직 없다는 뜻임을 문장으로 드러냅니다.
3. **무효가 된 항목은 지우지 않고 표기합니다.** 전제가 사라진 계획 항목은 조용히 삭제하지 말고
   `VOID`와 사유·날짜를 남깁니다. 왜 없어졌는지 추적할 수 있어야 합니다.
4. **시점 기록은 고치지 않습니다.** 감사 보고서, 코드 리뷰, 세션 로그는 작성 당시의 사실입니다. 본문을
   현재 코드에 맞춰 고치는 대신 헤더에 기준 시점을 밝혀 현재 상태와 혼동되지 않게 합니다.

계획 문서의 구조, 상태 어휘, ID 규칙, 양식은 [`plan/process.md`](plan/process.md)에 있습니다.

### 문서 헤더 표준 양식

`reference/`와 `plan/`의 모든 문서는 제목 바로 아래에 다음을 답니다.

```markdown
> **문서 종류**: 기준 문서 (사람 대상)
> **코드 대조 기준**: 2026-09-13 · 커밋 `8cc0273`
> 이 문서는 위 시점의 코드에서 확인된 것만 기술합니다. 계획 항목은 상태 표기로 구분합니다.
```

AI 에이전트가 작업 중 따르는 규칙·근거 문서(`reference/control_manifest.md`, `unity-rest.md`, `orchestration.md`, `plan/process.md`)는 첫 줄을 `> **문서 종류**: 작업 규칙 (AI 대상)` 으로 답니다. 위 표의 `reference/` 사람 대상 정의에서 벗어나는 의도된 예외입니다.

`archive/` 문서는 `코드 대조 기준` 대신 작성 시점과 보관 사유를 적습니다.

### 코드 스니펫 출처 표기

문서에 싣는 코드 블록은 첫 줄에 출처를 답니다. 나중에 코드가 바뀌었을 때 어디를 다시 봐야 하는지가
문서 안에 남습니다.

```csharp
// Assets/02.Scripts/Games/Yacht/Augments/Framework/IAugmentHandler.cs:9-27
```

---

## 문서 목록

### `reference/` — 기준 문서

| 문서 | 내용 |
|---|---|
| [architecture_overview.md](reference/architecture_overview.md) | 신입 온보딩. 디렉터리·씬 계층·데이터 흐름·화면↔스크립트 치트시트·함정 5선 |
| [augments_specification_and_status.md](reference/augments_specification_and_status.md) | 증강 시스템 8대 디자인 패턴(코드 포함), 턴 훅 8단계, 점수 파이프라인, 55종 분류 현황 |
| [augment_balance_analysis.md](reference/augment_balance_analysis.md) | 증강 밸런스 평가 기준(GEV 모델)과 현행 55종 진단 |
| [architecture_decisions.md](reference/architecture_decisions.md) | 구조 판단 ADR, ADR 양식과 상태 수명주기 |
| [control_manifest.md](reference/control_manifest.md) | 레이어별 해야 할 것·하지 말 것. 구현 지시서에 절 단위로 싣는 원본 |
| [coding_conventions.md](reference/coding_conventions.md) | C# 규약, Unity 직렬화 예외, 식별자 영문 규칙 |
| [art_style_guide.md](reference/art_style_guide.md) | 아트 톤앤매너, 조명, 팔레트, 외부 에셋 출처 |
| [unity-cli.md](reference/unity-cli.md) | Unity CLI 사용 요령과 함정. REST와의 역할 분담 근거, 테스트 실행, `script eval` 조회 |
| [unity-rest.md](reference/unity-rest.md) | unity-skills REST 공통 요령·함정. 서버 탐색, 호출 형태, `job_wait` 금지, 스크린샷, Play Mode 진입 |
| [orchestration.md](reference/orchestration.md) | `CLAUDE.md` 오케스트레이션·Unity 자동화 규칙의 근거와 사례 |
| [art/pixel_edge_filter_plan.md](reference/art/pixel_edge_filter_plan.md) | `M12` 엣지 검출 셰이더 기술 레퍼런스 |
| [art/cel_shading_pixel_plan.md](reference/art/cel_shading_pixel_plan.md) | `M16` 셀 셰이딩 기술 레퍼런스 (채택 보류, 인프라만 유지) |
| [build_pdf.py](reference/build_pdf.py) | 위 두 가이드를 PDF로 굽는 스크립트. PDF 자체는 git 추적하지 않습니다 |

### `plan/` — 작업 지시서

| 문서 | 내용 |
|---|---|
| [STATUS.md](STATUS.md) | **진행 포인터.** 현재 마일스톤, 진행 중 태스크, 다음 행동 |
| [decisions.md](decisions.md) | 결정 기록 `D-nnn`과 미결정 사항 `Q-nnn` |
| [plan/process.md](plan/process.md) | 계획 문서 구조, 상태 어휘, ID 규칙, 양식, 공통 완료 기준, 작업 재개 절차 |
| [plan/roadmap.md](plan/roadmap.md) | 마일스톤 상태표와 재번호 이력 |
| [plan/direction.md](plan/direction.md) | 확정된 방향, 기준선, 목표 아키텍처, 위험 요소 |
| [plan/m17/](plan/m17/README.md) | `M17` 증강 요트 로컬 핫시트 완성. 태스크 문서 `T09`·`T18`·`T23`·`T25` 포함 |
| [plan/m18/](plan/m18/README.md) ~ [m20/](plan/m20/README.md) | 미착수 마일스톤 |
| [plan/tracks/solid/](plan/tracks/solid/README.md) | SOLID 위반 리팩토링 `SOLID-Tnn` |
| [plan/tracks/improvement/](plan/tracks/improvement/README.md) | 컴파일 경고·테스트·성능 등 개별 개선 태스크 (`BUILD-01`, `TEST-01`, `AUG-01` 등) |
| [plan/augment_design_backlog.md](plan/augment_design_backlog.md) | 미검토 증강 기획안 (상단 칸 변형 52종, 신규 34종) |

세션 로그는 각 마일스톤·트랙 폴더의 `log.md`에, 작업 ID가 없는 항목은 [`plan/log.md`](plan/log.md)에 있습니다. `agent/`에는 옛 경로를 찾아온 독자를 위한 안내문만 남아 있습니다.

### `qa/` — QA 기록

버그(`BUG-nnnn`), 플레이테스트, 밸런스 점검 기록입니다. 폴더·번호·상태·심각도 규칙은 [`qa/README.md`](qa/README.md)에 있습니다.

### `archive/` — 보관

보관 사유는 [`archive/README.md`](archive/README.md)에 문서별로 적혀 있습니다. 게이트를 통과한 마일스톤은 [`archive/milestones/`](archive/milestones/)에 있습니다(`M0`~`M16` 완료 기록, 구 `M7` 그래픽 사양, 그 시기 세션 로그).

---

## 관련 파일 (docs 바깥)

| 파일 | 내용 |
|---|---|
| [`../AGENTS.md`](../AGENTS.md) | 모든 AI 에이전트가 상속하는 공통 규칙 |
| [`../CLAUDE.md`](../CLAUDE.md) | Claude Code 전용 작업 규율, graphify 사용법, 서브에이전트 구성 |
| [`../.editorconfig`](../.editorconfig) | 코딩 규약의 기계 강제분 |
