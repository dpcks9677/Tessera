# ORCH. 오케스트레이션 스킬 재구성

> **문서 종류**: 트랙 계획 (AI 대상)
> **코드 대조 기준**: 2026-10-22 · 커밋 `f949af9`

## 목표

CCGS(`donchitos/claude-code-game-studios`)의 작업 방식(게이트 판정, 태스크 수명주기, ADR과 통제 매니페스트, 경로별 규칙, 지연 로드)을 기존 `tessera-*` 스킬·에이전트 체계 위에 얹습니다. 오케스트레이션의 뼈대(opus 주 세션, sonnet·haiku 서브에이전트, Unity 단독 접근)는 그대로 둡니다.

## 범위

- 포함: `.claude/rules/`, `.claude/gates/`, `.claude/skills/tessera-*`, `.claude/agents/tessera-*`, `.claude/hooks/`, `.claude/settings.json`, `docs/plan/process.md`, `docs/reference/architecture_decisions.md`, 신규 통제 매니페스트 문서, `AGENTS.md`, `docs/README.md`(포인터 한정. 사용자가 2026-10-22 수락)
- 범위 밖: C# 코드, Unity 씬·프리팹·에셋, CCGS 에이전트 49개 계층 구조, CCGS bash 설정 도구(`yaml-helper.sh`, `project.yaml`)

## 확정 결정 (2026-10-22)

| 항목 | 결정 |
|---|---|
| 판정 어휘 | `PASS` / `CONCERNS` / `NOT ASSESSED` / `FAIL`. 여러 게이트를 합칠 때는 가장 엄한 판정을 채택합니다(FAIL > CONCERNS > NOT ASSESSED > PASS) |
| `review_mode` 기본값 | `lean`. 준비 점검과 종료 기준 대조는 주 세션이 하고, reviewer 게이트는 마일스톤 게이트에서만 돌립니다. `--review solo|lean|full`로 한 번만 덮어씁니다 |
| ADR | 기존 ADR 체계를 확장(상태 수명주기·검토한 대안·레이어)하고 모듈별 통제 매니페스트를 도입합니다 |
| 부가 항목 | 결정적 조회 스크립트, SessionStart `STATUS.md` 주입, 큰 문서 통째 읽기 가드, 도메인 스킬 3개(balance-check·playtest-report·bug-report) 모두 진행합니다 |
| 커밋 | 트랙이 끝날 때 한 번. 매번 사용자 허가를 받습니다 |

## 완료 게이트

- 모든 태스크가 `DONE` 또는 사유가 적힌 `DROPPED`/`DEFERRED`
- 기존 `/tessera-task`, `/tessera-audit`, `/tessera-unity`, `/tessera-plan-sync` 호출이 깨지지 않음
- 같은 규칙이 두 곳에 본문으로 존재하지 않음(원본 하나, 나머지는 링크)

## 태스크

| ID | 작업 | 상태 | 수락 기준 | 문서 |
|---|---|---|---|---|
| `ORCH-T1` | 기반: 경로별 규칙, 게이트 정의, 판정 어휘 | `DONE` | 규칙 파일이 `paths` 범위 파일을 열 때 로드되고, 게이트 파일마다 판정 어휘와 입력이 정의됨 | [`.claude/rules/`](../../../../.claude/rules/), [`.claude/gates/`](../../../../.claude/gates/README.md) |
| `ORCH-T2` | `tessera-task` 수명주기 재구성 | `DONE` | `SKILL.md`에는 단계 목록만 있고, 0단계 준비 점검과 5단계 종료 게이트가 `DONE` 태스크 하나로 읽기 전용 시험을 통과함 | [`tessera-task/`](../../../../.claude/skills/tessera-task/SKILL.md) |
| `ORCH-T3` | `tessera-adr`와 통제 매니페스트 | `DONE` | 기존 ADR이 새 필드 형식으로 변환되고, 모듈별 해야 할 것·하지 말 것 구간을 implementer 프롬프트에 따로 실을 수 있음 | [`control_manifest.md`](../../../reference/control_manifest.md), [`tessera-adr/`](../../../../.claude/skills/tessera-adr/SKILL.md) |
| `ORCH-T4` | `tessera-gate` 마일스톤 게이트 | `DONE` | 완료된 마일스톤 하나에 시험 판정이 나오고, `PASS`이면 커밋 허가 요청으로 이어짐 | [`tessera-gate/`](../../../../.claude/skills/tessera-gate/SKILL.md) |
| `ORCH-T5` | 부가: 조회 스크립트, 시작 주입, 읽기 가드, 도메인 스킬 | `DONE` | 네 항목이 각각 개별 시험을 통과함 | — |

## 완료 기록

- `ORCH-T1` (2026-10-22): 규칙 5개, 게이트 6개와 `README.md`. 중복 본문을 implementer·reviewer·unity-operator·`tessera-audit` 에서 규칙 파일 포인터로 교체
- `ORCH-T2` (2026-10-22): `tessera-task` 를 단계 표와 `references/` 4개로 분리, 0단계 `READY`·5단계 `DONE-CRITERIA` 연결, `--review`·`--check` 인자. `M17-T7 --check` 시험에서 두 게이트 모두 `CONCERNS`(수락 기준 수량 없음, 설계 기록 없음). 같은 세션에서 아트 값 문서를 구현값에 맞추고 `AGENTS.md` 사본 삭제
- `ORCH-T3` (2026-10-22): `architecture_decisions.md` 머리에 「ADR 양식」(상태 `Proposed`/`Accepted`/`Deprecated`/`Superseded by ADR-nnn`, 메타 6필드, 본문 5절) 신설, `ADR-001` 변환. `docs/reference/control_manifest.md` 신설(레이어 공통·게임 규칙·프레젠테이션, 절마다 해야 할 것·하지 말 것과 출처). `/tessera-adr new|accept` 스킬. `module-boundary.md`·`process.md` §6의 레이어 규칙 본문을 매니페스트로 이관하고 포인터로 교체
- `ORCH-T4` (2026-10-22): `/tessera-gate <Mnn|트랙> [--review] [--check]` 스킬 신설. 대상 판독(진행 중 마일스톤·트랙·보관 마일스톤), 범위 확정, `MILESTONE` 게이트 판정, PASS 시 커밋 허가 요청, 허가 뒤 `process.md` §8 순서(roadmap → committer → 보관은 별도 확인). `MILESTONE.md` 트리거와 `tessera-task` 6단계 보고에 포인터. `M15 --check --review solo` 시험 판정 `PASS`, 커밋 허가 요청 초안까지 표시
- `ORCH-T5` (2026-10-22): `.claude/scripts/tessera_query.py`(`task`·`tasks`·`sections`) 신설, SessionStart 훅(`session-start.py`)으로 STATUS 표·브랜치·미커밋 수 주입, PreToolUse(Read) 훅(`read-guard.py`)으로 40KB 초과 `.md`의 전체 읽기 차단과 목차 반환(tessera-reader 면제, 오류 시 통과). 도메인 스킬 `tessera-balance-check`·`tessera-playtest-report`·`tessera-bug-report`(opus·high), `docs/qa/README.md` 신설. 결정은 `D-043`

## 관련 결정

- CCGS 조사와 이식 범위 판단은 2026-10-22 세션 기록에 있습니다. CCGS 원본은 설치하지 않고 참고 자료로만 씁니다.
