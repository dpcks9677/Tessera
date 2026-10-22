# 오케스트레이션 트랙 세션 로그

> **문서 종류**: 시점 기록
> 최신 항목이 위에 옵니다. 각 항목은 **작성 당시의 사실**이며 이후 코드 변경을 반영하지 않습니다.
> 양식은 [`process.md`](../../process.md) §5.3을 따릅니다.

<!-- 양식
### YYYY-MM-DD — <작업자> (<작업 ID> <요약>)

- 작업 ID:
- 변경:
- 검증:
- 다음 작업:
-->

---

### 2026-10-22 — Claude Opus (ORCH-T6 CLAUDE.md 다이어트와 Unity REST 함정 단일화)

- 작업 ID: `ORCH-T6`
- 변경:
  - M2. 신규 `docs/reference/unity-rest.md`(서버 탐색, 호출 형태, `job_wait` 금지, 스크린샷, Play Mode 진입). `tessera-verifier`·`tessera-unity-operator` 정의에서 중복 본문을 지우고 「먼저 이 파일을 읽는다」 한 줄과 역할 고유 절만 남김. `tessera-unity/SKILL.md` 의 해상도·Play Mode 사유 문장을 포인터로 교체. CLAUDE.md §0.1은 「언제 찍는가」 정책만 유지
  - M3. CLAUDE.md §1~4(Karpathy, 전역 CLAUDE.md 와 중복) 삭제. 신규 `docs/reference/orchestration.md` 에 역할별 재사용 근거, 토큰 예산 서사, `unity-agent-plugin` 역할 분담, 주 세션 가드 훅 동작 범위, graphify 운영 근거를 옮기고 CLAUDE.md 에는 결론과 포인터만 남김. `docs/README.md` reference 표에 두 문서 행 추가
  - L1. `korean-guard.py` 주석과 CLAUDE.md §0의 caveman 플러그인 재주입 전제 정정(플러그인 미설치 확인)
  - 게이트 5단계 `CONCERNS`를 사용자가 수락. CLAUDE.md 15,168바이트로 「10KB 안팎」 목표 미달. 더 줄이려면 집행 규칙 절(에이전트 구성 표, 커밋 메시지 형식, 오케스트레이션 규칙 서술)을 옮겨야 해서 이 상태로 수락
  - 커밋: 미실행(사용자 허가 대기)
- 검증:
  - tessera-reader 대조: 규칙 문장 누락 0건. 지적 4건(깨진 참조 2곳, §0 문구 변질 2곳, operator 어미 1곳) 수정. 수정 후 전체 재대조는 하지 않고 지적 문장만 직접 확인
  - tessera-verifier: 포트 8090, `unity-rest.md` 「서버 탐색」 절차로 정상 동작. `Tessera.Editor.Tests` EditMode 370건 중 통과 370, 실패 0, 스킵 0. C# 변경 없음
  - CLAUDE.md 측정 20,666 → 16,428(1차) → 15,168바이트(최종)
- 다음 작업: 하네스 정비와 `ORCH-T6` 변경의 커밋 허가 대기. 같은 작업 트리에 `.claude/settings.json` 의 `unity-mcp-skill` deny 규칙과 CLAUDE.md 한 줄이 섞여 있음. M17 남은 태스크 지목 대기
  - 후속(기록만, 미수정): (1) `tessera-verifier.md` 3단계 `editor_playmode_inspect` 의 「`target` 이 필수」 표현 오기. unity-skills 2.9.1 은 `target` 을 `UNKNOWN_PARAM` 으로 거부하고 `name`/`path`/`instanceId` 로 호출해야 정상 응답. (2) `docs/README.md` reference 표에 `unity-cli.md` 행 없음(ORCH-T6 이전부터 누락). (3) `unity-rest.md`·`orchestration.md` 머리의 「문서 종류: 작업 규칙 (AI 대상)」 은 `docs/README.md` 의 reference 정의(사람 대상)와 어긋남. `control_manifest.md` 도 같은 머리라 기존 불일치. (4) verifier 정의가 렌더 안 된 Game View 함정을 `unity-rest.md` 포인터로 알게 됨

---

### 2026-10-22 — Claude Opus (ORCH 게이트 수락 CONCERNS 후속 수정)

- 작업 ID: ORCH 트랙 종료 게이트 수락 잔여 항목 (a)~(d) (태스크 ID 없음)
- 변경:
  - (a) 모듈 목록·네임스페이스 사본 정리. `AGENTS.md` 모듈 아키텍처 항목을 `architecture_overview.md` §3과 `control_manifest.md` 머리말 포인터로 교체. `.claude/rules/module-boundary.md` 의 폴더↔네임스페이스 예시와 `Tessera.Network` 문장을 제거하고 「새 파일 네임스페이스는 같은 폴더의 기존 파일을 따름」 한 줄로 축소. `architecture_overview.md` §3 인용 블록의 `.asmdef` 설명을 `control_manifest.md` 포인터로 바꾸고 파일 개수(61개·26개) 제거
  - (b) Unity 단독 실행 사유의 사본 4곳(`tessera-unity/SKILL.md`, `tessera-verifier.md`, `tessera-unity-operator.md`, `tessera-reviewer.md`)을 `CLAUDE.md` 「오케스트레이션 › 규칙」 포인터로 교체
  - (c) `.claude/scripts/tessera_query.py` 에 `in_plan()` 헬퍼 추출, 빈 `--state` 값을 argparse 오류(종료 코드 2)로 처리. 회귀 테스트 `.claude/scripts/test_tessera_query.py` 신설. `.gitignore` 에 `__pycache__/` 추가
  - (d) `architecture_decisions.md` 「ADR 양식」 필드 절에 근거 절 첫 줄 대조 기준 줄 규칙, 문서 머리 기준 불변, 새 본문은 문서 끝에 `---` 로 구분, 제목 앵커 링크 규칙 추가. ADR-001 근거 절 첫 줄에 「대조 기준: 2026-09-14 · 커밋 `0a2f770`」 추가. `tessera-adr/SKILL.md` new 3단계에 양식 절 포인터 추가
- 검증:
  - `python -m unittest discover -s .claude/scripts -p "test_*.py"` → 7개 통과. 테스트 범위: 헤딩 내 `#` 보존, 닫는 `#` 제거, BOM 첫 헤딩, 펜스 무시, BOM 표, 경로 구간 일치, 빈 `--state`
- 커밋: 미실행(사용자 허가 대기)
- 다음 작업: M17 남은 태스크 지목 대기

---

### 2026-10-22 — Claude Opus (ORCH 트랙 종료 게이트 `/tessera-gate orch`)

- 작업 ID: ORCH 트랙 종료 (태스크 ID 없음, 트랙 게이트)
- 변경:
  - 1차 판정 `FAIL`. 완료 게이트 「같은 규칙이 두 곳에 본문으로 존재하지 않음」 미충족, `NOT ASSESSED` 2건(`/tessera-adr new` 실행, 서브에이전트 경로별 규칙 자동 로드)
  - 조치: 규칙 본문 사본을 원본 포인터로 교체(`tessera-implementer`·`tessera-verifier`·`tessera-audit`·`tessera-gate`·`AGENTS.md`·`module-boundary.md`·`skill-authoring.md`). `CLAUDE.md` 는 사용자가 범위에 포함하도록 허락했으나, 항상 로드되는 쪽을 원본으로 두는 판단으로 `skill-authoring.md` 를 포인터로 바꾸고 `CLAUDE.md` 는 결과적으로 무변경
  - `tessera_query.py`(헤딩 정규식, `--plan` 경로 구간 비교, utf-8-sig) 와 `session-start.py`(브랜치 줄 순서) 수정
- 검증:
  - 서브에이전트에서 범위 파일 `Read` 시 경로별 규칙 주입 확인(.cs → module-boundary·serialization, .unity → art-direction)
  - `skill-authoring.md` 의 「가정하지 않음」 문장을 실측 사실로 교정
  - `/tessera-adr new` 를 스크래치패드 사본으로 드라이런(ADR-002 `Proposed` + D-044, 매니페스트 무변경, 저장소 흔적 없음)
  - 재판정 `CONCERNS`, 사용자 수락(2026-10-22)
- 수락한 잔여 항목(사용자 후속 수정 예정):
  - (a) `AGENTS.md:15-17` 모듈 목록 ↔ `architecture_overview.md` §3, `module-boundary.md:9-10` 폴더↔네임스페이스 예외 기술의 사본 잔존
  - (b) Unity 동시 실행 금지 상기 문장 4곳 반복
  - (c) `tessera_query.py` 자동 테스트 부재, 빈 `--state` 가 필터를 끔
  - (d) `/tessera-adr` 가 ADR 본문 삽입 위치와 문서 머리 「코드 대조 기준」 갱신 여부를 명시하지 않음
- 커밋: 사용자 허가로 커밋·푸시. 커밋 해시는 적지 않음
- 다음 작업: 수락 CONCERNS 4건 후속 수정(사용자 진행). 이후 M17 남은 태스크 지목 대기

---

### 2026-10-22 — Claude Opus (ORCH-T5 부가: 조회 스크립트, 시작 주입, 읽기 가드, 도메인 스킬)

- 작업 ID: `ORCH-T5`
- 변경:
  - `.claude/scripts/tessera_query.py` 신규: 계획서 태스크 표 결정적 조회 `task <ID>`, `tasks [--plan] [--state]`, `sections <path>`. `tessera-task/references/locate.md`, `tessera-unity/SKILL.md`, `tessera-gate/SKILL.md`, `tessera-reader.md` 의 Grep 안내를 이 스크립트 포인터로 교체
  - `.claude/hooks/session-start.py` 와 `.claude/settings.json` SessionStart 훅: 시작·/clear·압축 뒤 STATUS.md 표, 브랜치, 미커밋 변경 수 주입
  - `.claude/hooks/read-guard.py` 와 `.claude/settings.json` PreToolUse(Read) 훅: 40KB 초과 `.md`를 offset/limit 없이 통째로 읽으면 차단하고 헤딩 목차 반환. tessera-reader 면제, 오류 시 통과(fail-open)
  - 도메인 스킬 3개 신설: `.claude/skills/tessera-balance-check/`, `.claude/skills/tessera-playtest-report/`, `.claude/skills/tessera-bug-report/` (opus·high)
  - `docs/qa/README.md` 신설(기록 규칙 원본). `docs/README.md` 에 `qa/` 절 포인터 추가. 트랙 범위 밖이나 사용자 결정으로 포함
- 검증:
  - 조회 스크립트: `ORCH-T5`, `SOLID-T01`, `BUILD-01`, `M17-T1`, `M0-T1`, 소문자 `m17-t7` 조회 성공. `NOPE-T99` exit 1. 펜스 안 헤딩 무시. SOLID 12/TODO 7, ORCH 5/TODO 1 이 STATUS와 일치
  - 읽기 가드: 주 세션과 tessera-scout 의 `T09-vfx.md`(70KB) 통째 읽기 차단, 목차 반환. tessera-reader 는 같은 파일 읽기 성공(면제 확인). offset 있는 호출, 40KB 이하 파일, `.md` 아닌 파일은 통과
  - SessionStart: 스크립트 직접 시험과 실제 세션 압축 뒤 `source=compact` 주입 확인
  - balance-check: 저장소에서 실제 실행, 결과 `docs/qa/balance/2026-10-22-reverse-choice.md`. BALANCE 판정 `CONCERNS`(GEV +16.03, A 등급, 기회비용 미반영으로 과대평가)
  - bug-report·playtest-report: 스크래치패드에서 드라이런. 환경 수집, 번호 결정, `tasks --plan m17 --state TODO,DOING` 연결, 양식 출력, OPEN→verify NOT ASSESSED→close 전이 확인. AskUserQuestion 입력 경로는 실제로 거치지 않음
  - READY: `CONCERNS`, 사용자 수락(2026-10-22). DONE-CRITERIA: `PASS`(C#·Unity 항목 해당 없음)
- 설계와의 차이:
  - 버그 상태 `FIXED` 를 없애고 OPEN/CLOSED 둘만 둠(설정할 주체가 없었기 때문)
  - 사용자 결정 4개(2026-10-22): 읽기 가드는 차단 후 목차 반환, 가드는 전체 적용하되 tessera-reader만 면제, 버그·플레이테스트 기록은 `docs/qa/` 파일, 도메인 스킬 모델은 opus. `D-043` 으로 한 항목에 묶어 기록
- 남은 지적(보고만, 문서 미수정): improvement 트랙 표에 `SOLID-T05`·`SOLID-T07` 중복. 스크립트 집계 15/10, STATUS 13/8
- 다음 작업: `/tessera-gate orch` 로 ORCH 트랙 종료 판정 후 커밋 허가 요청

---

### 2026-10-22 — Claude Opus (ORCH-T4 tessera-gate 마일스톤 게이트)

- 작업 ID: `ORCH-T4`
- 변경:
  - `.claude/skills/tessera-gate/SKILL.md` 신규
  - `.claude/gates/MILESTONE.md` 트리거 줄에 스킬 포인터
  - `.claude/skills/tessera-task/references/record-report.md` 보고 절의 마일스톤 커밋 안내를 `/tessera-gate` 포인터로 교체
- 검증:
  - `/tessera-gate M15 --check --review solo` 시험. 판정 `PASS`
    - 태스크 6개 모두 `DONE`
    - 완료 조건 4개를 보관 문서 실행 기록으로 대조: 20파일×20클립, 탈락 게이트, EditMode 793개 중 프로젝트 테스트 전부 통과, `Verify Pure Yaw Alignment` 600/600, `Run Physics And Keep Validation` PASSED
    - 세션 로그 항목 존재(옛 ID `M10.9`)
    - 커밋 `319cec3`의 베이커 파일 5개가 현재 `Assets/Editor/Dice/`·`Assets/Editor/Tests/Dice/`에 존재(`7b395e1` 이동)
    - 프리셋 JSON 21개 = 20종 + `index.json`
  - 하위 게이트는 solo라 생략. 게이트 6항 해당 없음 — 이미 커밋됨
  - PASS 뒤 「시험 — 실제 요청 아님」 커밋 허가 요청 초안 표시까지 확인
  - 준비 점검: `CONCERNS` 수락 2026-10-22 — 완료 마일스톤은 이미 커밋돼 있어 커밋 요청은 초안 표시로 시험, 시험 대상 `M15 --check --review solo` 사용자 선택
  - 설계와의 차이: 시험 중 보관 마일스톤이 `5375a2c` 이전 소수점 번호(`M10.9`)로 커밋돼 `--grep "M15-T"` 가 비는 문제 발견, 스킬 2단계에 옛 ID 재검색 안내 추가
  - 종료 게이트: `DONE-CRITERIA` `PASS`
  - 남은 일: `CLAUDE.md` 오케스트레이션 스킬 표에 `/tessera-adr`·`/tessera-gate` 미등재(트랙 범위 밖 파일, 사용자 확인 필요). 진행 중 마일스톤·트랙 대상 실사용과 `lean` 하위 게이트 경로는 트랙 종료 시 ORCH 트랙 자체로 확인 예정. M15 판 뒤집기 연출 강도 플레이 재확인(옛 로그의 남은 문제)은 완료 조건 밖이라 판정 미반영
- 다음 작업: `ORCH-T5`

---

### 2026-10-22 — Claude Opus (ORCH-T3 ADR 양식과 통제 매니페스트)

- 작업 ID: `ORCH-T3`
- 변경:
  - `docs/reference/architecture_decisions.md`: 「ADR 양식」 절(상태 수명주기 표, 메타 필드 상태·날짜·레이어·관련 결정·대체·매니페스트 반영, 본문 맥락·결정·검토한 대안·근거·결과와 후속 작업)과 「목록」 표(레이어 열 추가) 신설. `ADR-001` 을 메타 표와 새 절 순서로 재배치, 본문 문장 보존. 상태 `채택` → `Accepted`. 검토한 대안 표(정적 싱글턴 기각, 합성 루트 채택 방향, 매치 스코프 앰비언트 조건부 허용) 추가. 코드 대조 기준은 본문 사실이 바뀌지 않아 `0a2f770` 유지
  - `docs/reference/control_manifest.md` 신설: 레이어 공통(`Core`·`Dice`·`Tabletop`·`Rendering`), 게임 규칙(`Games/Yacht`·`AugmentedYacht/Logic`), 프레젠테이션(`AugmentedYacht/Presentation`). 절마다 적용 폴더, 해야 할 것, 하지 말 것, 규칙별 출처. 레이어 개념은 기존 문서에 없어 새로 정의
  - `.claude/skills/tessera-adr/SKILL.md` 신설: `new <제목>`(항상 `Proposed`, 매니페스트 불변), `accept ADR-nnn`(상태 확인 → `ADR` 게이트 → 사용자 확인 → 상태 전이·대체 표기·매니페스트 갱신)
  - 원본 이관: `.claude/rules/module-boundary.md` 의 레이어 규칙 3줄과 `docs/plan/process.md` §6의 게임 코어·UI 항목 2줄을 매니페스트로 옮기고 포인터로 교체
  - 연결: `tessera-task/references/implement-verify.md` 3단계에 대상 레이어 절을 지시서 끝 `## 통제 매니페스트` 구간으로 붙이는 절차, `tessera-implementer` 에 그 구간 처리 규칙. `.claude/gates/ADR.md` 필드 원본 포인터와 점검 항목 3~5 갱신, `DONE-CRITERIA.md` 증거 출처, `tessera-audit` 렌즈 D 기준, `AGENTS.md` §1, `docs/README.md` 문서 목록 2곳
- 검증:
  - `READY`: `CONCERNS` — 수락 기준의 "새 필드 형식"이 미정의, 레이어 개념 부재, 레이어 규칙 원본 중복 위험
  - CONCERNS 수락 2026-10-22 — 매니페스트를 레이어 규칙 원본으로 두고 기존 본문은 포인터로 교체
  - 코드 실측(`tessera-scout`): 게임 규칙 레이어에 `UnityEngine` using·`MonoBehaviour`·가변 static 0건, 프레젠테이션의 상태 직접 쓰기 0건. 매니페스트 규칙이 현재 코드와 충돌하지 않음
  - 구간 추출: 게임 규칙 절(35~51행)을 줄 번호로 잘라 단독으로 읽힘을 확인
  - 중복: 이관한 규칙 문장이 `docs/archive/` 밖 문서와 `.claude/` 에 매니페스트 외에는 없음을 검색으로 확인. 수정 문서의 상대 링크 정상
  - `DONE-CRITERIA`: `CONCERNS` — 트랙 범위 밖 포인터 2곳(`AGENTS.md`, `docs/README.md`), 설계 외 추가 수정 2곳(`DONE-CRITERIA.md`, `tessera-audit`). 테스트·플레이 모드·회귀는 해당 없음(C#·Unity 변경 없음)
  - CONCERNS 수락 2026-10-22 — 범위 밖 포인터 유지, 트랙 README 범위에 추가
  - NOT ASSESSED 2026-10-22 — `/tessera-adr` 실제 실행. 수락 기준 밖이며 `accept ADR-001` 은 1단계 중단 경로라 절차 확인만 함
- 설계와의 차이: 매니페스트 규칙의 출처를 `process.md` §6 대신 `ADR-001` 로 둠(§6이 매니페스트를 가리키게 되어 순환 방지). 공통 레이어 「해야 할 것」은 출처 있는 규칙이 없어 "현재 없음"
- 남은 일:
  - `Assets/Scripts/Tabletop/HourglassTimer.cs:4` 의 미사용으로 보이는 `using Tessera.Games.AugmentedYacht;` 가 공통 레이어 「하지 말 것」 위반(C#이라 ORCH 범위 밖, 미수정) → 후속 정리에서 처리
  - `ADR-001` 합성 루트 그림은 `AugmentedYachtController` 가 세션을 만든다고 하나 실제 생성은 `YachtTurnFlowPresenter.cs:263` → 후속 정리에서 처리(본문에 `[현재 코드, 2026-10-22]` 주석 기존 존재 확인)
  - `YachtGameCommandResult.State` 가 가변 `YachtGameState` 를 public 필드로 노출 → 후속 정리에서 변경 없음으로 결정
  - `docs/plan/process.md` 의 예시 링크 2개(`T25-bank-keep-zone.md`, `README.md`)가 상대 경로로 해석되지 않음(기존) → 후속 정리에서 미처리(이번 작업 이전부터 존재)
- ORCH-T3 후속 정리 (2026-10-22):
  - 공통→게임 역방향 의존 해소: `ITurnDelaySource` 를 `Games/AugmentedYacht/Presentation/` (`Tessera.Games.AugmentedYacht`) 에서 `Tabletop/` (`Tessera.Tabletop`) 으로 이동(`.meta` 함께 이동, GUID 유지). `HourglassTimer.cs` 의 `using Tessera.Games.AugmentedYacht;` 제거. 원래 '미사용 using' 으로 보고했으나 실제로는 `HourglassTimer` 가 Presentation 소속 인터페이스를 구현하던 진짜 의존이었음. 인터페이스가 게임 타입을 참조하지 않아 이동만으로 해소. 사용처 `YachtTurnFlowPresenter.cs` 는 이미 `using Tessera.Tabletop;` 보유
  - ADR-001 합성 루트 그림과 실제 코드 차이: `architecture_decisions.md` ADR-001에 「[현재 코드, 2026-10-22]」 주석 추가. 세션 생성은 `YachtTurnFlowPresenter.cs:263`, 권위 생성은 `YachtGameSession` 생성자(`LocalGameAuthority.cs:742`)
  - `YachtGameCommandResult.State` (가변 `YachtGameState` 공개 필드): 변경 없음으로 결정. 값이 항상 `state.Clone()` 이라 권위 상태 오염 경로 없음, 프로덕션·테스트 모두 읽기만 함, 프레젠테이션은 미사용. `IReadOnlyYachtGameState` 로 바꾸면 테스트 4곳(`NoTimeFailed`·`NoTimeRemaining`·`lowerBaseScores` 등 인터페이스 미노출 멤버)이 깨져 인터페이스 확장이 필요하므로 별도 태스크 후보로만 남김
  - 검증: Unity 컴파일 오류 없음, EditMode `Tessera.Editor.Tests` 370/370 통과. 콘솔의 기존 SVG 임포트 실패(`Assets/Art/Source/Parchment/Icons/*.svg`, `Unity.VectorGraphics` `NotImplementedException`)는 이번 변경과 무관한 기존 문제
  - 미처리: `docs/plan/process.md` 의 기존 예시 링크 2개(`T25-bank-keep-zone.md`, `README.md`) 깨짐, 이번 작업 이전부터 존재
  - 커밋 없음. 파일 이동 2건은 `git mv` 로 스테이징 상태
- 다음 작업: `ORCH-T4` `tessera-gate` 마일스톤 게이트

### 2026-10-22 — Claude Opus (ORCH-T2 tessera-task 수명주기 재구성, 아트 값 문서 정합화)

- 작업 ID: `ORCH-T2`. 아트 값 정합화는 `ORCH-T1` 완료 게이트(규칙 본문 중복 금지)의 후속으로 같은 세션에서 수행
- 변경 ①(`ORCH-T2`): `tessera-task/SKILL.md` 를 인자·병렬 정책·단계 표(0~6)만 남기고, 단계별 절차를 `references/locate.md`, `investigate-design.md`, `implement-verify.md`, `record-report.md` 로 분리. 0단계에 `READY`, 5단계에 `DONE-CRITERIA` 게이트 연결. `--review solo|lean|full`, 읽기 전용 `--check` 인자 추가. `ORCH-Tnn` 계획서 매핑 추가
- 변경 ②(`ORCH-T2`): 시험에서 드러난 공백 보강. `DONE-CRITERIA.md` 에 태스크 성격상 적용되지 않는 항목을 `해당 없음 — <사유>` 로 빼는 규칙 추가
- 변경 ③(아트 값): 사용자 결정 "구현된 쪽으로". `tessera-scout` 실측 기준으로 `docs/reference/art_style_guide.md` §1~§5 정정. 조명은 `YachtLightingRig` 프리셋이 원본(기본 `Soft Neutral` `#ffe0bd` 1.35, 키 75%·필 25% 같은 색, 쿨 림 라이트·색온도 없음), 카메라 배경 `#0f0b0a`/`#0f0d12`, 촛불 Point 라이트 `#ff9933`. 목재·러너·골드는 §4 토큰 값과 구현 Smoothness/Metallic 단일값으로 교체. §4 표에 토큰이 `TesseraPixelPalette` 양자화용이며 조명·카메라 토큰은 실값과 다르다는 설명 추가. §5 "웜+쿨 2중 조명 유지" 지침을 "조명은 리그에서 조정"으로 교체
- 변경 ④(아트 값): `AGENTS.md` §3의 값 사본 삭제, 원본 링크와 규칙 파일 포인터만 남김. `.claude/rules/art-direction.md` 를 새 원본에 맞추고 `paths` 에 `YachtLightingRig`·`YachtCameraRig`·`TabletopSurfaceBuilder` 추가
- 검증:
  - `/tessera-task M17-T7 --check` 절차를 주 세션이 읽기 전용으로 수행. 파일 변경 없음
  - `READY`: `CONCERNS` — 수락 기준 "여러 증강 조합으로 완주함"에 수량이 없음(구현에서 60판·10종 이상으로 구체화됨). 나머지 항목 충족
  - `DONE-CRITERIA`: `CONCERNS` — 수락 기준은 `YachtRandomFullGamePlaythroughTests` 2개와 EditMode 369/369로 충족, 플레이 모드·정상/경계/거부 사례는 해당 없음(테스트 전용 태스크, 새 규칙 없음), 회귀는 전체 실행에 포함. 설계 요약 기록이 없어 "설계와의 차이" 대조 불가
  - 아트 값: 저장소 문서(`docs/archive/` 제외)에 옛 범위값·`AGENTS.md §2` 참조가 남지 않음을 검색으로 확인
- 변경 ⑤(아트 값, 사용자 지시로 범위 밖 C# 주석만 수정): `WoodPlankTextureGenerator.cs:193` 의 옛 범위 `#633e26~#7d4d2e` → 램프 단(×0.35, ×1.45)과 토큰 `#6e432a`·`#825033`, `CozyCandleStand.cs:243` 의 `2800K` → `#ff9933`, `TesseraPixelPalette.cs:8` 문서 경로 → `docs/reference/art_style_guide.md`. 주석 전용이라 컴파일·테스트 미실행
- 다음 작업: `ORCH-T3` `tessera-adr`와 통제 매니페스트

### 2026-10-22 — Claude Opus (ORCH-T1 경로별 규칙과 게이트 정의)

- 작업 ID: `ORCH-T1`
- 변경:
  - `.claude/rules/` 신설. `module-boundary.md`(`Assets/Scripts/**/*.cs`), `serialization.md`(`.cs`, `.unity`, `.prefab`), `art-direction.md`(씬·프리팹·머티리얼·셰이더, `Assets/Rendering/**`, `Assets/Scripts/Rendering/**`), `editor-tests.md`(`Assets/Editor/Tests/**/*.cs`), `skill-authoring.md`(`.claude/` 하위 스킬·에이전트·게이트·규칙). 본문은 원본 문서 경로와 요지만 둠
  - `.claude/gates/` 신설. `README.md`(판정 어휘 4개, 병합 규칙, `review_mode` 표, 호출·출력·기록 형식), `READY`, `DONE-CRITERIA`, `CODE-REVIEW`, `TEST-COVERAGE`, `ADR`, `MILESTONE`. 각 파일은 실행 주체, 트리거, 넘길 맥락, 점검 항목, 판정 절로 구성
  - `tessera-implementer`: 낡은 모듈 목록(`Tessera.Network`, `Tessera.Games.AugmentedYacht` 단독) 삭제, 규칙 파일 3개를 읽으라는 지시로 교체
  - `tessera-unity-operator`: 아트 값 사본 삭제, `art-direction.md`·`serialization.md` 포인터로 교체
  - `tessera-reviewer`: 규칙 파일·게이트 파일을 받았을 때의 절차와 판정 줄 출력 추가
  - `tessera-audit`: 렌즈 B·C·D·E 본문을 규칙·게이트 파일 포인터로 교체. 낡은 `AGENTS.md §2` 참조와 테스트 개수(26개 클래스, 213개) 삭제
- 검증:
  - 주 세션에서 `.cs` 파일을 읽을 때 `module-boundary.md`·`serialization.md`, `.mat` 파일을 읽을 때 `art-direction.md`, `.claude/rules/` 쓰기 시 `skill-authoring.md` 가 자동 로드됨을 확인. 중괄호 glob 동작 확인
  - `.claude/` 아래 아트 hex 값, `AGENTS.md` §2 참조, `Tessera.Network` 모듈 표기, 테스트 개수 사본이 남지 않음을 검색으로 확인
  - NOT ASSESSED 2026-10-22 — 서브에이전트에서의 경로별 자동 로드. 설계상 정의 파일이 규칙 파일을 직접 읽게 해 결과에 의존하지 않음
- 다음 작업: `ORCH-T2` `tessera-task` 수명주기 재구성
