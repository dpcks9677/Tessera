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

커밋 또는 푸시를 진행할 때:

- 메시지는 Conventional Commits 헤더로 시작합니다: `type(scope): 한국어 요약` (type: feat/fix/chore/style/refactor/docs/test 등).
- 헤더 아래 빈 줄 뒤에 **불릿포인트(`- `)** 로 수정 사항을 한국어로 요약 설명합니다. 파일 나열이 아니라 "무엇을 왜 바꿨는지" 단위로 적습니다.
- 기존 커밋 로그(`git log`)의 패턴을 따릅니다.
- 커밋 메시지 끝에 다음 줄을 넣습니다:
  `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`
- 커밋·푸시는 사용자가 요청할 때만 수행합니다. 기본 브랜치면 먼저 브랜치를 만듭니다.

## Unity Editor 자동화 (unity-skills)

- Unity Editor 조작(스크립트·씬·프리팹·에셋·머티리얼·테스트 실행 등)은 **`unity-skills` 스킬**로 수행합니다.
- 이 스킬의 원본은 <https://github.com/Besty0728/Unity-Skills> 입니다. 갱신·재설치 시 이 저장소를 기준으로 합니다.
- 개념 질문만이고 Editor 상태를 건드리지 않으면 스킬 없이 `skills/` 하위 해당 문서만 읽습니다.
