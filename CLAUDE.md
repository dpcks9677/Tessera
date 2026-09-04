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
- `graphify-out/graph.json` 이 없으면 `graphify update .` 로 먼저 생성합니다. `graphify-out/` 은 git 추적 대상 아님(각자 생성).

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
