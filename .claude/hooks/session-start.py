# SessionStart hook: injects the current status pointer into the context.
#
# Every session began by reading docs/STATUS.md and running git status as its
# first turns. This hook injects the STATUS.md tables plus branch and dirty
# count on startup, resume, clear and compact, so that first read is unneeded.
# On any error it prints nothing.

import json
import os
import subprocess
import sys
from pathlib import Path

MAX_CHARS = 3000
UNKNOWN = "알 수 없음"


def git(root, *args):
    return subprocess.run(
        ["git", *args], cwd=root, capture_output=True, timeout=3, check=True
    ).stdout.decode("utf-8", errors="replace")


try:
    data = json.loads(sys.stdin.buffer.read().decode("utf-8") or "{}")
    source = data.get("source") or "unknown"
    root = Path(os.environ.get("CLAUDE_PROJECT_DIR") or Path(__file__).resolve().parents[2])

    status = (root / "docs" / "STATUS.md").read_text(encoding="utf-8")
    table = [line for line in status.splitlines() if line.startswith("|")]

    try:
        branch = git(root, "rev-parse", "--abbrev-ref", "HEAD").strip()
    except Exception:
        branch = UNKNOWN
    try:
        dirty = str(len([l for l in git(root, "status", "--porcelain").splitlines() if l.strip()]))
    except Exception:
        dirty = UNKNOWN

    text = "\n".join(
        [
            f"[세션 시작 자동 주입 · source={source}] docs/STATUS.md 표와 작업 트리 상태입니다. STATUS.md를 다시 읽을 필요는 없습니다.",
            f"브랜치: {branch} · 커밋 안 된 변경: {dirty}개",
            *table,
        ]
    )[:MAX_CHARS]
    out = {"hookSpecificOutput": {"hookEventName": "SessionStart", "additionalContext": text}}
    sys.stdout.buffer.write(json.dumps(out, ensure_ascii=False).encode("utf-8"))
except Exception:
    sys.exit(0)
