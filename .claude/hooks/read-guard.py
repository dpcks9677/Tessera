# PreToolUse hook (Read): blocks whole-file reads of large markdown documents.
#
# Reading a 40KB+ plan or reference doc in one go dumps tens of thousands of
# tokens into the context, and every later turn re-reads them. The hook denies
# the read and returns the heading outline so the caller can Read just the
# section it needs with offset/limit. Modeled on CCGS validate-read.
#
# tessera-reader is exempt: bulk document reading is its job. Any error fails
# open (the read is allowed).

import json
import os
import sys

LIMIT_BYTES = 40 * 1024
MAX_HEADINGS = 60

try:
    sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "scripts"))
    import tessera_query

    data = json.loads(sys.stdin.buffer.read().decode("utf-8") or "{}")
    if data.get("agent_type") == "tessera-reader":
        sys.exit(0)

    tool_input = data.get("tool_input") or {}
    path = str(tool_input.get("file_path", ""))
    if not path.lower().endswith(".md"):
        sys.exit(0)
    if tool_input.get("offset") is not None or tool_input.get("limit") is not None:
        sys.exit(0)

    size = os.path.getsize(path)
    if size <= LIMIT_BYTES:
        sys.exit(0)

    headings = tessera_query.sections(path)
    lines = [
        f"{os.path.basename(path)}은 {size // 1024}KB로 40KB를 넘어 통째로 읽지 않습니다. "
        "아래 목차에서 필요한 절의 줄 번호를 골라 Read의 offset/limit으로 그 절만 읽으십시오."
    ]
    for number, level, text in headings[:MAX_HEADINGS]:
        lines.append(f"L{number} {'#' * level} {text}")
    if len(headings) > MAX_HEADINGS:
        lines.append(
            f"(헤딩 {len(headings)}개 중 {MAX_HEADINGS}개만 표시. "
            f"나머지는 python .claude/scripts/tessera_query.py sections {path})"
        )
    out = {
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "deny",
            "permissionDecisionReason": "\n".join(lines),
        }
    }
    sys.stdout.buffer.write(json.dumps(out, ensure_ascii=False).encode("utf-8"))
except SystemExit:
    raise
except Exception:
    sys.exit(0)
