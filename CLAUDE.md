# CLAUDE.md

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 0. Response Style

- Caveman 압축 모드는 이 저장소에서 **항상 활성**입니다 (레벨 full).
- 단, 답변 말투는 **한국어 존댓말**(`-습니다` / `-요`)로 작성합니다.
- Caveman 규칙(관사·군더더기·인사말·헤징 제거, 단편 문장 허용, 짧은 동의어, 도구 호출 나레이션 금지)은 그대로 적용하고 **말투만 존댓말**로 바꿉니다.
- 코드·주석·커밋·문서·PR 본문 등 채팅 밖 산출물은 기존 규칙대로 일반 문체로 작성합니다.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

---

> This project also has an `AGENTS.md` with project-specific rules (module architecture, art direction). Follow both: `AGENTS.md` for Tessera-specific conventions, this file for general coding discipline.

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
- graphify 결과는 스코프된 서브그래프라 raw grep 결과나 GRAPH_REPORT.md 통독보다 훨씬 작습니다.
- graphify가 충분한 컨텍스트를 못 주는 경우에만 직접 Read/Grep 으로 내려갑니다.
- `graphify-out/graph.json` 이 없으면 `graphify update .` 로 먼저 생성합니다. 산출물은 git 추적 대상이 아니라 각자 생성합니다.
- 예외로 `graphify-out/.graphify_labels.json` 과 같은 이름의 `.sig` 는 추적합니다. 노드·엣지·커뮤니티 분할은 AST에서 결정돼 같은 커밋이면 어디서든 똑같이 재생성되지만, 커뮤니티 이름은 LLM으로만 만들 수 있어 재생성이 불가능합니다. 이 두 파일 덕분에 새 환경에서 `graphify update .` 만 돌려도 같은 이름이 붙습니다.
- **`PYTHONHASHSEED` 는 반드시 `0` 으로 고정된 상태에서 graphify를 돌립니다.** 커뮤니티 검출이 문자열 키 집합의 순회 순서에 의존하는데 그 순서가 프로세스마다 무작위라, 시드를 고정하지 않으면 같은 코드에서도 커뮤니티 경계가 흔들립니다. 그러면 저장된 이름이 대량으로 무효화돼 라벨 파일이 매번 수백 줄씩 바뀌고 기기 간 충돌이 납니다. `post-commit` 훅과 `.claude/settings.json` 이 이 값을 고정하므로 이 저장소에서 작업할 때는 신경 쓸 필요가 없지만, 다른 터미널에서 직접 실행할 때는 `PYTHONHASHSEED=0 graphify update .` 로 돌립니다.

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
- 커밋 메시지 끝에 하네스가 지정한 `Co-Authored-By:` 라인을 넣습니다 (모델명은 세션마다 다르므로 여기에 고정하지 않습니다).

## Unity Editor 자동화 (unity-skills)

- Unity Editor 조작(스크립트·씬·프리팹·에셋·머티리얼·테스트 실행 등)은 **`unity-skills` 스킬**로 수행합니다.
- 이 스킬의 원본은 <https://github.com/Besty0728/Unity-Skills> 입니다. 갱신·재설치 시 이 저장소를 기준으로 합니다.
- 개념 질문만이고 Editor 상태를 건드리지 않으면 스킬 없이 `skills/` 하위 해당 문서만 읽습니다.

## 오케스트레이션

설계 판단은 Opus high가 하고, 실행은 Sonnet medium 서브에이전트가 맡습니다. 무거운 파일 읽기·빌드 로그·테스트 출력은 서브에이전트 컨텍스트에서 소비되고, 설계자에게는 요약만 올라옵니다.

### 구성

에이전트는 `.claude/agents/` 에 있습니다. 전부 `model: sonnet`, `effort: medium` 입니다.

| 에이전트 | 역할 | Unity 접근 |
|---|---|---|
| `tessera-scout` | graphify 기반 조사. 읽기 전용 | 없음 |
| `tessera-implementer` | 확정된 설계대로 C# 수정 | 없음 |
| `tessera-verifier` | 컴파일·EditMode 테스트 실행과 판정 | 있음 |
| `tessera-scribe` | 계획서 상태표·세션 로그 갱신. `docs/` 한정 | 없음 |
| `tessera-reviewer` | 지정된 렌즈 하나로 감사. 읽기 전용 | 없음 |
| `tessera-unity-operator` | 씬·프리팹·머티리얼·에셋 조작 | 있음 |

스킬은 `.claude/skills/tessera-*` 에 있습니다. 전부 `model: opus`, `effort: high` 입니다.

| 스킬 | 용도 |
|---|---|
| `/tessera-task <ID>` | 작업계획서 태스크 하나를 조사·설계·구현·검증·기록까지 수행 |
| `/tessera-audit <대상>` | 프로젝트 고유 규칙 렌즈로 병렬 감사 |
| `/tessera-unity <작업>` | Unity 씬·프리팹 작업을 조사부터 시각 확인까지 수행 |
| `/tessera-plan-sync` | 계획서 상태와 실제 코드·커밋의 불일치 교정 |

### 규칙

- 사용자가 태스크 ID(`Mnn-Tnn`, `SOLID-Tnn`)를 지목해 작업을 요청하면 `/tessera-task` 흐름을 탑니다.
- 코드 위치·호출 관계·영향 범위 조사는 직접 grep하지 말고 `tessera-scout` 에 위임합니다.
- **Unity 접근 에이전트(`tessera-verifier`, `tessera-unity-operator`)는 동시에 둘 이상 실행하지 않습니다.** Unity Editor는 REST 서버 하나에 붙은 단일 인스턴스라 동시 조작 시 씬이 깨집니다. 나머지 네 에이전트는 Unity 도구를 갖지 않아 구조적으로 충돌하지 않습니다.
- 파일 작업(조사·구현·문서)은 대상 파일 집합이 겹치지 않을 때만 병렬로 최대 3개까지 돌립니다.
- 서브에이전트는 대화 이력을 물려받지 않습니다. 필요한 맥락을 매 호출 프롬프트에 전부 실어야 합니다. `CLAUDE.md`, `AGENTS.md`, PreToolUse 훅은 상속됩니다.
- **서브에이전트는 커밋하지 않습니다.** 커밋·푸시는 위 「커밋 & 푸시」 규칙대로 매번 사용자 허가를 받습니다.
- 계획서와 실제 코드가 어긋나면 임의로 판단해 진행하지 말고 사용자에게 확인합니다. 실제로 어긋난 사례가 있습니다(`D-041`).

### 새 에이전트·스킬 등록 시점

에이전트 정의 파일을 새로 만들면 **사용자 턴 경계에서만 인식됩니다.** 만든 직후 같은 턴에 `Agent` 로 띄우면 `not found` 로 실패합니다. 스킬은 같은 턴 안에서도 잠시 뒤 인식됩니다.
