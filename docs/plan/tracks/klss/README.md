# KLSS. 외부 프롬프트 시리즈 선별 도입

> **문서 종류**: 트랙 계획 (AI 대상)
> **코드 대조 기준**: 2026-10-24 · 커밋 `d7d59cd`

## 목표

X의 @kloss_xyz가 공개한 AI 코딩 에이전트 프롬프트 시리즈(Part 1 심문, Part 2 문서 11종 생성, Part 3 실행 에이전트 규칙, Part 4 디버그 에이전트) 중 Tessera에 없는 부분만 골라 기존 체계에 보강합니다. 시리즈 전체를 이식하지 않습니다. 문서 스위트 생성, 웹앱 전제 항목, 디버그 에이전트 전체는 이미 같은 기능이 있거나 해당이 없습니다.

## 범위

- 포함
  - 교훈 기록 루프 (사용자 교정 → 「실수·원인·예방 규칙」)
  - 신규 태스크 설계 앞의 심문 단계
  - 에이전트 규칙 5건: 문서 간 충돌 보고, 2회 실패 중단의 에이전트 확장, 변경 보고 3단화, 미확정 표기·가정 금지, 작업 전 가정 목록
  - 수락 기준 ID와 게이트의 변경↔기준 대조
- 범위 밖
  - Part 2의 문서 11종 생성 (`docs/plan/`, `STATUS.md`, `decisions.md`, `reference/`가 대신함)
  - APP_FLOW, FRONTEND_GUIDELINES, BACKEND_STRUCTURE, 모바일 우선, 브라우저·ARIA 점검 (웹앱 전제)
  - Part 4 디버그 에이전트 전체 (`tessera-bug-report`, `mattpocock-skills:diagnosing-bugs`가 대신함)
  - `tasks/todo.md`, `progress.txt` (태스크 표와 `STATUS.md`가 대신함)
  - 낡은 메모리 정리 (아래 「발견」)

## 출처

- Part 1 <https://x.com/kloss_xyz/status/2018421310066741613>
- Part 2 <https://x.com/kloss_xyz/status/2018524456050987449>
- Part 3 <https://x.com/kloss_xyz/status/2018566914726060435>
- Part 4 <https://x.com/kloss_xyz/status/2018762791692423242>

## 사전 조사 결과 (2026-10-24)

후보 4건 모두 「없음」이 아니라 「부분」이 대부분이어서 신설보다 기존 파일 보강이 중심입니다.

| 후보 | 판정 | 빠진 것 |
|---|---|---|
| 교훈 루프 | 부분 | 메모리에 교정이 쌓이지만 「실수·원인·예방」 필드가 없고, `process.md` §9 재개 절차에 읽기 단계가 없음. `CLAUDE.md` 「영구 지식」 규칙은 도구 함정용이라 사용자 교정이 대상이 아님 |
| 심문 단계 | 부분 | `grilling`이 라운드 질문·확인 대기는 함. 추천 답을 붙여 수락시키는 점, 사실 조사를 서브에이전트에 시키는 점, 모호한 답 되묻기·가정 목록이 없는 점이 Part 1과 다름 |
| 충돌 시 멈춤 | 부분 | 계획↔코드 충돌만 있음(`CLAUDE.md` 오케스트레이션 규칙, `investigate-design.md`). 문서끼리 충돌은 없음 |
| 2회 실패 중단 | 있음 | `implement-verify.md`, `tessera-unity/SKILL.md`에 있음. 에이전트 정의에는 없음 |
| 3단 보고 | 부분 | `tessera-implementer.md` 반환 형식에 「건드리지 않은 파일」이 없음 |
| 미확정 표기·가정 금지 | 부분 | `BLOCKED`가 「선행 작업 대기」 뜻으로 이미 쓰여 어휘가 겹침. 「가정 금지」 조항 없음 |
| 가정 목록 | 없음 | |
| 요구사항 추적 | 부분 | 작업 ID는 있으나 수락 기준에 ID가 없고, `DONE-CRITERIA.md`에 변경 파일↔기준 대조가 없음 |

### 발견 (이 트랙 범위 밖, 기록만)

- 메모리 `commit-and-push-use-sonnet-low.md`는 「Sonnet low」라고 쓰는데 `tessera-committer.md`는 `haiku`입니다. 메모리가 낡았습니다.
- `MEMORY.md` 색인이 메모리 13개 중 4개만 가리킵니다.
- `tessera-task/references/locate.md` 접두사 매핑에 `LAYOUT`이 이미 빠져 있습니다. `KLSS` 등록 때 함께 고칩니다.
- 세션 시작 훅(`.claude/hooks/session-start.py`)은 읽기 목록이 아니라 `STATUS.md` 표 행만 주입합니다(상한 3,000자). 교훈 파일은 주입하지 않고 `process.md` §9 읽기 단계로 넣습니다.

## 확정 결정 (2026-10-24, 사용자 수락)

| 항목 | 결정 |
|---|---|
| 트랙명 | `KLSS` |
| 심문 단계 | 새 스킬을 만들지 않고 `tessera-task/references/investigate-design.md`를 보강합니다. `grilling`을 호출하되 추천 답 금지·모호한 답 되묻기·요약 후 누락 확인·가정 목록으로 덮어씁니다 |
| 규칙 배치 | 본문은 `docs/reference/` 한 파일에 두고 `CLAUDE.md`에는 포인터만 둡니다. `CLAUDE.md`는 ORCH-T6에서 줄인 직후라 늘리지 않습니다 |
| 미확정 표기 | `BLOCKED`와 겹치지 않도록 `미확정`을 씁니다 |
| ADR | 불필요. [`control_manifest.md`](../../../reference/control_manifest.md)는 C# 레이어 경계만 다루고 이 트랙은 그 규칙을 바꾸지 않습니다 |
| 요구사항 추적 | 가장 무거워 마지막에 합니다. `KLSS-T1`~`T3`의 효과를 본 뒤 착수 여부를 정합니다 |
| 커밋 | 트랙이 끝날 때 한 번 합니다. 매번 사용자 허가를 받습니다 |

## 완료 게이트

- 모든 태스크가 `DONE`이거나, 사유가 적힌 `DROPPED`/`DEFERRED`
- 새로 만들거나 고친 문서에서 깨진 링크 0 (`tessera-reader` 점검)
- `CLAUDE.md` 줄 수 증가 2줄 이하 (착수 전 156줄)
- 새 규칙이 기존 규칙과 모순 없음 (`tessera-reader` 대조). 특히 `process.md` §3의 `BLOCKED` 정의와 충돌하지 않음
- `.claude/scripts/tessera_query.py`가 `KLSS` 행을 정상 조회함

## 태스크

| ID | 작업 | 상태 | 수락 기준 | 문서 |
|---|---|---|---|---|
| `KLSS-T1` | 교훈 루프 | `DONE` | `docs/reference/lessons.md`에 항목 양식(실수/원인/예방 규칙)과 경계 규칙(도구 함정은 `.claude/agents/`, 행동 교정은 lessons, 개인 선호는 메모리)이 있고, `process.md` §9 재개 절차에 읽기 단계가 있음 | — |
| `KLSS-T2` | 심문 단계 | `DONE` | `investigate-design.md`에 심문 단계 절이 있고, 발동 조건(수락 기준 모호·열린 질문·동작 미기술·설계 멈춤 조건)과 `grilling` 대비 덮어쓰기 4가지(추천 답 금지, 모호한 답 되묻기, 질문만 하기, 요약 후 누락 확인)가 명시됨 | — |
| `KLSS-T3` | 에이전트 규칙 5건 편입 | `DONE` | 5건 각각의 배치가 정해져 본문이 `docs/reference/` 한 파일에 있고, `tessera-implementer` 반환 형식에 「건드리지 않은 파일」이 있으며, `CLAUDE.md` 증가는 2줄 이하 | — |
| `KLSS-T4` | 수락 기준 ID와 게이트 대조 | `DONE` | `process.md` 양식에 수락 기준 ID(`AC-n`)가 반영되고, `DONE-CRITERIA.md`에 변경↔기준 대조 항목이 있으며, 시범 대조가 한 번 수행됨 | — |

- 순서는 T1 → T2 → T3 → T4입니다. T4는 T1~T3 결과를 보고 `DEFERRED`로 돌릴 수 있습니다.
- T1은 `docs/`만, T2·T3·T4는 `.claude/`를 건드립니다. 대상 파일이 겹치지 않으면 T2·T3을 병렬로 둘 수 있습니다.
- 트랙 등록(`STATUS.md` 트랙 줄, `process.md` §1 트랙 목록, `D-055`, `locate.md` 접두사, `tessera-gate/SKILL.md` 예시)은 2026-10-24에 합니다.

## 완료 기록

### `KLSS-T1` 교훈 루프 — 2026-10-24
- `docs/reference/lessons.md` 신설. 경계 표(도구 함정은 `.claude/agents/`, 행동 교정은 lessons, 프로젝트 규칙은 원본 문서, 개인 선호는 메모리), 기록 시점·ID(`L-nnn`)·정리 규칙, 항목 양식(실수/원인/예방 규칙/원본)을 정함
- 시작 항목 5건(`L-001`~`L-005`)은 사용자 메모리의 행동 교정 중 원본 규칙이 이미 저장소에 있는 것만 골랐음. 규칙 본문은 복사하지 않고 원본 위치만 가리킴. 원인이 본문에 없는 2건은 `미확정`으로 적음
- `process.md` §9 1단계에 lessons 읽기와 「교정을 받았다면 다음 작업 전에 기록」을 추가. 단계 번호는 바꾸지 않음(단계 번호를 인용하는 문서 없음 확인)
- `docs/README.md` reference 표와 「AI 에이전트로 작업한다면」 목록에 lessons 추가
- 검증(tessera-reader): 링크 7개 깨짐 0. 「원본」 절 대조에서 `AGENTS.md` 절 이름과 `L-004`의 「매번 새로」 역할 예외 누락을 찾아 고침
- 게이트: READY PASS, DONE-CRITERIA PASS (코드·테스트·플레이 항목은 해당 없음, 문서만 변경)
- 후속: 교정을 lessons에 적으라는 지시가 아직 `CLAUDE.md`에 없음. `KLSS-T3`의 포인터 한 줄에 포함함

### `KLSS-T2` 심문 단계 — 2026-10-24
- `.claude/skills/tessera-task/references/investigate-design.md`에 「1.5. 심문 (조건부)」 절 추가. 조사 뒤·설계 앞에 위치함
- 발동 조건: 수락 기준 모호(`READY` `CONCERNS`), 열린 질문 존재, 새 기능·규칙인데 동작이 한 줄뿐, 설계 멈춤 조건 해당. 해당이 없으면 건너뜀. 계획서가 충분한 태스크에서 형식적으로 묻지 않기 위함
- `grilling` 대비 덮어쓰기 4가지: 추천 답(`➡️`) 금지, 모호한 답 되묻기, 심문 중에는 질문만(사실 조사는 scout·reader), 끝에 확정·가정·범위 밖으로 나눠 요약하고 누락 확인. 스킬 호출 여부와 무관하게 이 규칙이 우선함
- 2단계 멈춤 조건 문장에 「1.5의 방식으로 묻습니다」 연결
- 수락 기준 문구를 실제 내용(발동 조건 4종, 덮어쓰기 4가지)에 맞춰 고침. 착수 전 문구는 「덮어쓰기 3가지」였음
- 검증: 링크를 추가하지 않아 reader 점검 생략. `process.md` §3·`CLAUDE.md` 규칙과 모순 없음을 직접 대조함(트랙 게이트에서 reader가 다시 대조)
- 트레이드오프: 심문은 사용자 확인 전 설계로 넘어가지 않으므로, 사용자가 「멈추지 말고 계속」을 지시한 세션에서도 발동 조건에 걸리면 멈춤. 이는 의도된 동작

### `KLSS-T3` 에이전트 규칙 5건 편입 — 2026-10-24
- `docs/reference/agent_rules.md` 신설. 규칙 5건의 본문과 적용 대상 표: 문서 충돌 보고, 2회 실패 중단, 3단 보고, `미확정` 표기와 추정 금지, 가정 목록. `미확정`·`미확인`·`BLOCKED`의 의미 구분을 명시함
- 배치: `tessera-implementer`(충돌·2회·`미확정` 한 줄 + 반환 형식에 「건드리지 않은 파일」·「가정」 추가), `tessera-verifier`·`tessera-unity-operator`(2회 실패 중단·`미확정`/추정 금지 한 줄), `tessera-reader`·`tessera-scribe`(문서 충돌 시 판정하지 않고 두 위치 병기), `CLAUDE.md`(포인터 한 줄. 교정은 같은 세션에 `lessons.md`에 기록하라는 지시 포함, T1 후속 해소), `docs/README.md` 색인
- `CLAUDE.md` 156줄 → 157줄(+1, 게이트 한도 +2 이내)
- 검증(tessera-reader): 링크 깨짐 0, 인용 6건 중 5건 일치. 지적 반영: 「3단」과 implementer 5항목 불일치 → §3에 5항목 구성 설명, `BLOCKED` 정의 문구를 `process.md` §3 원문에 맞춤, 적용 대상 표에서 reader·scribe의 자체 표기 명시, `CLAUDE.md` 포인터에 「3단 보고」 추가
- 미점검: `.claude/rules/` 중 `skill-authoring.md` 외 4개 본문 대조(트랙 게이트에서 다시 점검)
- 결정: `scout`·`reviewer`·`committer`에는 포인터를 넣지 않음. 읽기 전용 조사·감사·커밋 실행이라 해당 규칙이 거의 걸리지 않음

### `KLSS-T4` 수락 기준 ID와 게이트 대조 — 2026-10-24
- `process.md` §5.2 태스크 문서 양식의 수락 기준에 `AC-1` 형식을 반영하고, `AC-n` 부여 규칙(문서 안 연번, 번호 재사용 금지, 하위 작업·완료 기록에서 `AC-n` 인용)을 한 불릿으로 추가함. 태스크 표의 한 줄 수락 기준 칸에는 붙이지 않음(칸 하나에 기준이 하나라 ID가 정보를 더하지 않음)
- `DONE-CRITERIA.md` 점검 1번에 「변경↔기준 대응」 추가. 변경 파일마다 대응 기준(또는 기록·색인 같은 부수 갱신)을 적고, 대응 없는 변경은 「설계와의 차이」로 올림
- 범위를 줄인 이유: 기존 점검 1번이 이미 기준을 문장 단위로 하나씩 대조하게 하고 있어, 빠진 것은 변경→기준 방향의 대조뿐이었음. 요구사항 추적 매트릭스와 별도 ID 체계는 만들지 않음
- 시범: 새로 만든 태스크 문서가 없어 `KLSS-T3`에 소급 대조(모의 실행). 기준 3건은 모두 증거가 있었고, `docs/README.md` 색인 행은 대응 기준이 없어 부수 갱신으로 분류됨. 양식이 빈틈을 드러내는 것까지 확인했음. 실제 태스크 문서에서의 첫 사용은 다음 신규 태스크 문서 작성 때이며 그때까지 효과는 검증되지 않음
- 기존 태스크 문서(`M17-T25` 등)의 수락 기준은 소급해 번호를 달지 않음
- 검증: 링크 추가 없음

### 트랙 종료 게이트 — 2026-10-24
- 판정 `CONCERNS`. 태스크 4건 모두 `DONE`, 완료 게이트 5항목 충족(깨진 링크 0, `CLAUDE.md` 157줄로 +1, `tessera_query.py`가 KLSS 4행 조회, `BLOCKED` 정의와 충돌 없음)
- tessera-reader 전수 점검 지적 6건을 고침: 2회 실패 세는 단위 명시, 3단 보고 표의 잘못된 주장, 심문 절의 `READY` 중복 질문과 `미결 질문` 갱신 시점, 다른 심문 스킬(`grill-me` 등)과의 추천 답 충돌 해소 문구, operator의 `미확정` 표기
- 하위 게이트 `CODE-REVIEW`·`TEST-COVERAGE`는 해당 없음. C#·테스트 변경이 없고 문서·`.claude/` 설정만 바뀜
- `CONCERNS` 사유: 작업 트리에 이 트랙과 무관한 `Assets/07.Settings/Mobile_RPAsset.asset`, `Claude outputs/`가 있음. 커밋 대상에서 뺌
- 남은 한계: 규칙 효과는 실제 태스크에서 써 봐야 검증됨. `AC-n` 첫 사용과 심문 단계 발동은 아직 없음

## 관련 결정

- `D-055`: 위 「확정 결정」. 시리즈 전체 이식이 아니라 선별 도입으로 정한 근거와 `KLSS` 트랙 등록
