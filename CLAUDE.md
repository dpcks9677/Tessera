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

주 세션(오케스트레이터)이 따르는 커밋·위임·토큰 예산·재사용 규칙은 `.claude/orchestrator.md` 에 있습니다. SessionStart 훅(`.claude/hooks/orchestrator-rules.py`)이 시작·재개·`/clear`·압축 때 주 세션에만 주입합니다. 주 세션 컨텍스트에 「주 세션 규칙」 주입이 보이지 않으면 그 파일을 직접 읽습니다. 서브에이전트는 그 파일을 상속하지 않고, 역할 규칙은 각 정의 파일(`.claude/agents/*.md`)에 있습니다.

모든 에이전트 공통:

- 커밋·푸시는 사용자 허가 뒤 `tessera-committer` 만 실행합니다. 다른 에이전트는 커밋하지 않습니다.
- 계획서와 실제 코드가 어긋나면 임의로 판단해 진행하지 말고 보고합니다(`D-041`).
- 공통 작업 규칙(문서 충돌 보고, 2회 실패 중단, 3단 보고, `미확정` 표기, 가정 목록)은 `docs/reference/agent_rules.md` 입니다.
