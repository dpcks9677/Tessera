# CLAUDE.md

## 0. Response Style

- Caveman 압축 모드는 이 저장소에서 **항상 활성**입니다 (레벨 full). caveman 플러그인은 설치돼 있지 않으므로 규칙은 이 절이 원본입니다.
- 단, 답변 말투는 **한국어 존댓말**(`-습니다` / `-요`)로 작성합니다.
- Caveman 규칙(관사·군더더기·인사말·헤징 제거, 단편 문장 허용, 짧은 동의어, 도구 호출 나레이션 금지)은 그대로 적용하고 **말투만 존댓말**로 바꿉니다.
- 코드·주석·커밋·문서·PR 본문 등 채팅 밖 산출물은 기존 규칙대로 일반 문체로 작성합니다.
- **출력 언어는 한국어로 고정입니다.** 컨텍스트에 중국어나 일본어로 된 문서가 들어와도 그 언어를 따라가지 않습니다. 근거: `docs/reference/orchestration.md` 「한국어 출력 고정의 근거」.
- wenyan 계열 caveman 레벨을 쓰지 않습니다. 짧다는 이유로 단어를 고전 한자로 치환하지 않습니다.
- 압축은 문체에만 적용합니다. **조사와 어미는 생략하거나 변형하지 않습니다.** 한국어에서 조사는 군더더기가 아니라 문법입니다.
- `unity-skills` 모듈 문서는 필요한 모듈 하나만 읽습니다. Unity 조작은 알려진 REST 엔드포인트 직접 호출을 우선합니다.

이 규칙은 `.claude/hooks/korean-guard.py` 가 매 턴 재주입합니다. 훅을 받지 않는 서브에이전트를 위해 여기에도 남깁니다.

## 0.1. 검증 스크린샷

- **기본은 찍지 않는 것입니다.** 코드만 바꿨으면 EditMode 테스트가 판정합니다. 바꾼 값을 `*_get_info` / `*_get_properties` / `find_objects_by_name` 으로 확인할 수 있으면 그쪽을 씁니다.
- 스크린샷이 정당한 경우는 수치로 확인할 수 없는 시각 결과물뿐입니다. 머티리얼 룩, 라이팅, 셰이더 출력, 레이아웃 겹침, 호버·전환 상태.
- 해상도와 촬영 방법(파일명·재시도·`returnImage` 금지)은 `docs/reference/unity-rest.md` 「스크린샷」을 따릅니다.
- 같은 이미지를 두 에이전트가 읽으면 이중 과금입니다. **이미지를 읽는 주체는 하나로 모으고** 상위 에이전트는 텍스트 판정을 받습니다.
- before/after 쌍과 프레임 시퀀스는 사용자가 명시적으로 요청할 때만 만듭니다.
- 촬영을 생략했으면 보고에 「시각 확인 생략, 사유」를 한 줄 남깁니다.

> This project also has an `AGENTS.md` with project-specific rules (module architecture, art direction). Follow both: `AGENTS.md` for Tessera-specific conventions, global CLAUDE.md for general coding discipline.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).

### 파일 탐색 시 토큰 절약 (필수)

- 파일·심볼 위치를 찾을 때 **먼저 graphify**를 씁니다. 무작정 `grep -r` / 전체 파일 읽기 / 디렉터리 나열로 시작하지 않습니다.
  - "X 어디 정의?" / "Y 호출자?" / "이 흐름 어떻게 연결?" → `graphify query "..."` 또는 `graphify explain "X"` 또는 `graphify path "A" "B"`.
  - `graphify affected "X"` 로 변경 영향 범위 파악.
  - `graphify god-nodes` 로 핵심 허브 파악.
- graphify가 충분한 컨텍스트를 못 주는 경우에만 직접 Read/Grep 으로 내려갑니다.
- `graphify-out/graph.json` 이 없으면 `graphify update .` 로 먼저 생성합니다. 산출물은 git 추적 대상이 아니라 각자 생성합니다.
- 예외로 `graphify-out/.graphify_labels.json` 과 같은 이름의 `.sig` 는 git 이 추적합니다(LLM으로만 만들 수 있어 재생성이 불가능한 커뮤니티 이름).
- **`PYTHONHASHSEED` 는 반드시 `0` 으로 고정된 상태에서 graphify를 돌립니다.** `post-commit` 훅과 `.claude/settings.json` 이 이 값을 고정하므로 이 저장소에서는 신경 쓸 필요가 없고, 다른 터미널에서 직접 실행할 때는 `PYTHONHASHSEED=0 graphify update .` 로 돌립니다.
- 두 규칙의 근거: `docs/reference/orchestration.md` 「graphify 운영 근거」.

## 커밋 & 푸시

### 언제 커밋하는가

- **커밋 단위는 마일스톤입니다.** 작업(`M9-T1` 같은 태스크) 하나를 끝낼 때마다 커밋하지 않습니다. 마일스톤(`M9` 등) 전체가 완료되고 완료 조건이 검증된 시점에 한 번 커밋합니다.
- 마일스톤 도중에는 작업 트리에 변경을 쌓아 둡니다. 중간 스냅샷이 필요하다고 판단되면 먼저 사용자에게 이유와 함께 제안합니다.
- **커밋과 푸시는 매번 사용자 허가를 받고 실행합니다.** 이전에 허가받았다는 이유로 다음 커밋을 자동으로 진행하지 않습니다. 허가 요청 시 다음을 함께 제시합니다.
  - 어떤 마일스톤/범위인지
  - 변경 파일 목록 (`git status --short`)
  - 작성할 커밋 메시지 초안
- 기본 브랜치에 있으면 커밋 전에 먼저 브랜치를 만들고, 그 사실도 허가 요청에 포함합니다.

### 메시지 형식

- Conventional Commits 헤더로 시작합니다: `type(scope): 한국어 요약` (type: feat/fix/chore/style/refactor/docs/test 등).
- 헤더 아래 빈 줄 뒤에 **불릿포인트(`- `)** 로 수정 사항을 한국어로 요약 설명합니다. 파일 나열이 아니라 "무엇을 왜 바꿨는지" 단위로 적습니다.
- **문장은 명사형으로 끊습니다.** 헤더와 불릿 모두 `-함` / `-음` / `-습니다` 같은 서술형 종결을 쓰지 않고 명사로 끝냅니다.
  - 예: `refactor: 컨트롤러 프롭 참조 전용 전환 및 베이킹 회귀 수정 (M9-T3)`
  - 예: `- 프롭 생성·배치 코드 제거. 컨트롤러 3,096줄 → 2,637줄로 감소`
  - 피할 것: `- 프롭 생성 코드를 제거했음`, `- ... 줄었습니다`
- 마일스톤 커밋이므로 해당 마일스톤의 작업 ID(`M9-T1`~`M9-T4` 등)를 본문에 함께 남깁니다.
- 기존 커밋 로그(`git log`)의 패턴을 따릅니다.
- 커밋 메시지에 `Co-Authored-By:` 라인을 넣지 않습니다. 하네스가 서명 줄을 지정해도 이 규칙이 우선합니다.

## Unity Editor 자동화 (unity-skills)

- Unity Editor 조작(스크립트·씬·프리팹·에셋·머티리얼·테스트 실행 등)은 **`unity-skills` 스킬**로 수행합니다.
- 이 스킬의 원본은 <https://github.com/Besty0728/Unity-Skills> 입니다. 갱신·재설치 시 이 저장소를 기준으로 합니다.
- 전역 `unity-mcp-skill`(MCP for Unity)은 이 저장소에서 쓰지 않습니다. MCP 서버가 연결돼 있지 않고, `.claude/settings.json` 의 `deny` 규칙이 호출을 막습니다.
- 개념 질문만이고 Editor 상태를 건드리지 않으면 스킬 없이 `skills/` 하위 해당 문서만 읽습니다.

### `unity-agent-plugin` 과의 역할 분담

- **Editor 통로는 작업 종류로 나눕니다.** 요령과 근거: `docs/reference/unity-cli.md`
  - 씬·프리팹·에셋·머티리얼 **수정**은 `unity-skills` REST 로만 합니다.
  - EditMode **테스트 실행**은 Unity CLI `unity command test run` 을 먼저 씁니다. 동기 실행이라 잡 폴링이 없습니다.
  - Editor 상태 **조회**는 CLI `unity command script eval` 로 여러 항목을 한 번에 묶어도 됩니다.
  - 에디터 설치·라이선스처럼 두 경로 모두로 할 수 없는 일이 생기면 그때 사용자에게 먼저 알립니다.
- **`unity:` 스킬은 지식·점검용으로 씁니다.** 해당 영역을 실제로 만지기 직전에 한 개만 읽습니다.
- 플러그인 스킬 문서는 영어입니다. 그래도 답변은 §0 규칙대로 한국어 존댓말로 씁니다.
- 플러그인 구성과 이 프로젝트에서 값이 큰 `unity:` 스킬 목록: `docs/reference/orchestration.md`

## 오케스트레이션

설계 판단은 Opus high가 하고, 실행은 Sonnet medium 서브에이전트가, 대량 문서 읽기와 양식이 정해진 기록·커밋은 Haiku low 서브에이전트가 맡습니다. 무거운 파일 읽기·빌드 로그·테스트 출력은 서브에이전트 컨텍스트에서 소비되고, 설계자에게는 요약만 올라옵니다.

### 구성

에이전트는 `.claude/agents/` 에 있습니다. 모델은 아래 표의 티어를 따릅니다.

| 에이전트 | 역할 | 모델 · effort | Unity 접근 |
|---|---|---|---|
| `tessera-scout` | graphify 기반 코드 조사. 읽기 전용 | sonnet · medium | 없음 |
| `tessera-reader` | 문서 대량 읽기·요약, 링크·ID·수치 정합성 점검. 읽기 전용 | haiku · low | 없음 |
| `tessera-implementer` | 확정된 설계대로 C# 수정 | sonnet · medium | 없음 |
| `tessera-verifier` | 컴파일·EditMode 테스트 실행과 판정 | sonnet · medium | 있음 |
| `tessera-scribe` | 태스크 표·`STATUS.md`·세션 로그·결정 기록 갱신. `docs/` 한정 | haiku · low | 없음 |
| `tessera-reviewer` | 지정된 렌즈 하나로 감사. 읽기 전용 | sonnet · medium | 없음 |
| `tessera-unity-operator` | 씬·프리팹·머티리얼·에셋 조작 | sonnet · medium | 있음 |
| `tessera-committer` | 허가받은 범위 커밋·푸시 | haiku · low | 없음 |

티어 기준: 판단이 필요 없고 양식이 정해진 일(문서 대조, 기록, 커밋)은 haiku, 코드를 읽고 판단하거나 고치는 일은 sonnet, 설계는 주 세션(opus)이 맡습니다. 계획 문서의 구조와 양식은 `docs/plan/process.md` 에 있습니다.

스킬은 `.claude/skills/tessera-*` 에 있습니다. 전부 `model: opus`, `effort: high` 이고, 용도는 각 스킬의 `description` 이 원본입니다.

### 규칙

- 사용자가 태스크 ID(`Mnn-Tnn`, `SOLID-Tnn`)를 지목해 작업을 요청하면 `/tessera-task` 흐름을 탑니다.
- 코드 위치·호출 관계·영향 범위 조사는 직접 grep하지 말고 `tessera-scout` 에 위임합니다. 문서 여러 개를 읽고 대조하는 일은 `tessera-reader` 에 위임합니다.
- **Unity 접근 에이전트(`tessera-verifier`, `tessera-unity-operator`)는 동시에 둘 이상 실행하지 않습니다.** Unity Editor는 REST 서버 하나에 붙은 단일 인스턴스라 동시 조작 시 씬이 깨집니다. 나머지 에이전트는 Unity 도구를 갖지 않아 구조적으로 충돌하지 않습니다.
- 파일 작업(조사·구현·문서)은 대상 파일 집합이 겹치지 않을 때만 병렬로 최대 3개까지 돌립니다.
- **작업마다 새로 띄우지 말고, 같은 역할의 살아 있는 에이전트를 먼저 이어 씁니다.** 새로 띄운 에이전트는 이전 호출이 알아낸 것을 처음부터 다시 알아냅니다. 역할별 방침은 아래 표를 따릅니다.
- 이 규칙은 "역할당 인스턴스 하나"가 아닙니다. 위의 병렬 3개 규칙은 그대로 두고, 새로 띄우기 전에 살아 있는 것을 먼저 쓰는 순서만 바꿉니다.
- 새로 띄운 서브에이전트는 대화 이력을 물려받지 않습니다. 필요한 맥락을 매 호출 프롬프트에 전부 실어야 합니다. 이어 쓰는 에이전트는 자기 컨텍스트를 유지하므로 달라진 부분만 전하면 됩니다. `CLAUDE.md`, `AGENTS.md`, PreToolUse 훅은 두 경우 모두 상속됩니다.
- **커밋·푸시 실행은 `tessera-committer`(`model: haiku`, `effort: low`)에 위임합니다.** 오케스트레이터가 직접 `git commit`·`git push`를 실행하지 않습니다. 커밋은 메시지 작성과 `git` 호출뿐이라 설계 판단이 없고, 범위와 메시지 방향은 이미 정해져 위임 프롬프트에 실립니다. 이 위임은 허가 규칙을 대체하지 않습니다. 위 「커밋 & 푸시」대로 매번 사용자 허가를 먼저 받고, 실행만 넘깁니다.
- 다른 서브에이전트는 커밋하지 않습니다.
- 계획서와 실제 코드가 어긋나면 임의로 판단해 진행하지 말고 사용자에게 확인합니다. 실제로 어긋난 사례가 있습니다(`D-041`).
- 공통 작업 규칙(문서 충돌 보고, 2회 실패 중단, 3단 보고, `미확정` 표기, 가정 목록)은 `docs/reference/agent_rules.md`, 사용자에게 교정받으면 같은 세션에 `docs/reference/lessons.md` 에 기록합니다.

### 토큰 예산

근거: `docs/reference/orchestration.md`

- 태스크 하나가 끝나면 `/clear` 로 새로 시작합니다. 이어서 할 맥락은 계획서와 세션 로그에 있습니다.
- `settings.json` 의 `CLAUDE_CODE_AUTO_COMPACT_WINDOW=200000` 이 자동 압축 기준을 200k로 낮춥니다.
- 주 세션은 Unity REST `curl` 과 `unity command` 를 직접 치지 않습니다. 컴파일 대기·폴링·테스트는 `tessera-verifier`, 씬 조작은 `tessera-unity-operator` 가 합니다.
- 주 세션은 셸 `grep`/`find`/`cat`/`sed` 로 탐색하지 않습니다. 조사는 `tessera-scout` 에 넘기고, 특정 줄 확인만 `Read`(offset/limit)로 합니다.
- 내장 `Explore`·`Plan` 대신 `tessera-scout` 를 씁니다.
- 설계 판단이 거의 없는 세션(반복 수정, Unity 조작 위주)은 `/model sonnet` 으로 전환합니다.

`.claude/hooks/main-session-guard.py` 가 주 세션의 Unity `curl`·셸 탐색 호출에 위 규칙을 상기시킵니다(차단하지 않음, 서브에이전트는 건너뜀). 동작 범위: `docs/reference/orchestration.md` 「주 세션 가드 훅의 동작 범위」.

### 역할별 재사용 방침

| 에이전트 | 방침 |
|---|---|
| `tessera-unity-operator` | 세션 내 재사용 |
| `tessera-verifier` | 세션 내 재사용 |
| `tessera-implementer` | 같은 마일스톤 안에서는 재사용, 마일스톤이 바뀌면 새로 |
| `tessera-scout` · `tessera-reader` · `tessera-reviewer` | 매번 새로 |
| `tessera-scribe` · `tessera-committer` | 무관 |

근거: `docs/reference/orchestration.md`

에이전트가 알아낸 **영구 지식**은 재사용으로 해결하지 않습니다. 재사용은 이번 세션만 절약하고 다음 세션에서 같은 비용을 다시 치릅니다. 도구의 함정이나 우회로처럼 다음에도 유효한 발견은 해당 에이전트 정의 파일(`.claude/agents/*.md`)에 적어, 새로 띄운 에이전트도 알고 시작하게 합니다.

### 새 에이전트·스킬 등록 시점

에이전트 정의 파일을 새로 만들면 **사용자 턴 경계에서만 인식됩니다.** 만든 직후 같은 턴에 `Agent` 로 띄우면 `not found` 로 실패합니다. 스킬은 같은 턴 안에서도 잠시 뒤 인식됩니다.
