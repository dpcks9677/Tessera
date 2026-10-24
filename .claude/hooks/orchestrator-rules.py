# SessionStart hook: injects the main-session-only rules.
#
# Transcript logs (2026-10-25) showed every subagent starting from a 20-23k
# token base, and about 60% of the project CLAUDE.md (commit rules, delegation,
# token budget, reuse policy) applied only to the orchestrator. CLAUDE.md is
# inherited by every subagent, so those rules moved to .claude/orchestrator.md
# and this hook injects them. SessionStart fires only in the main session (on
# startup, resume, clear and compact), never in subagents.
#
# Hook output above roughly 10,000 characters is not injected inline, so a file
# that outgrows the cap is replaced by a pointer telling the session to Read it.
# On any error it prints a pointer as well.

import json
import os
import sys
from pathlib import Path

MAX_CHARS = 9500
POINTER = (
    "주 세션 규칙 주입 실패. 커밋·위임·토큰 예산·재사용 규칙은 .claude/orchestrator.md 에 있으니 "
    "작업을 시작하기 전에 그 파일을 Read로 읽으십시오."
)

sys.stdin.buffer.read()

try:
    root = Path(os.environ.get("CLAUDE_PROJECT_DIR") or Path(__file__).resolve().parents[2])
    text = (root / ".claude" / "orchestrator.md").read_text(encoding="utf-8")
    if len(text) > MAX_CHARS:
        text = POINTER
except Exception:
    text = POINTER

out = {"hookSpecificOutput": {"hookEventName": "SessionStart", "additionalContext": text}}
sys.stdout.buffer.write(json.dumps(out, ensure_ascii=False).encode("utf-8"))
