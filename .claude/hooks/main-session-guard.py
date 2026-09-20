# PreToolUse hook (Bash|PowerShell): nudges the main session to delegate.
#
# Transcript logs showed the main Opus session running hundreds of small shell
# turns (Unity REST curl polling, grep/cat exploration), each one re-reading a
# 200k+ token context. Those calls belong in subagents, so this hook adds a
# reminder when the main session makes them. It never blocks.
#
# Hook input carries agent_id (and agent_type) only inside a subagent, so
# subagents are exempt. Confirmed 2026-09-20 by dumping the raw hook input of a
# main-session Bash call and of a tessera-scout Bash call and comparing keys.
#
# The reminder is about searching the source tree. Piping another command's
# output through head/tail, and reading docs/ or .claude/, are the main
# session's own work and stay silent.

import json
import re
import sys

data = json.loads(sys.stdin.buffer.read().decode("utf-8") or "{}")
if data.get("agent_id"):
    sys.exit(0)

cmd = str((data.get("tool_input") or {}).get("command", ""))

UNITY_REST = re.compile(r"127\.0\.0\.1:\d+/(skill|skills|permission)\b")

EXPLORE = {
    "grep",
    "rg",
    "find",
    "cat",
    "head",
    "tail",
    "sed",
    "awk",
    "select-string",
    "get-content",
}
# These walk the working directory when no path is given.
TREE_BY_DEFAULT = {"rg", "find"}
# Documentation and agent configuration are the main session's own material.
DOC_PATH = re.compile(r"(^|[/\\])(docs|\.claude)[/\\]|\.md$", re.IGNORECASE)


def split_segment(segment):
    tokens = [t.strip("'\"()") for t in segment.split()]
    tokens = [t for t in tokens if t]
    while tokens and re.match(r"^\w+=", tokens[0]):
        tokens.pop(0)
    if not tokens:
        return "", []
    return tokens[0].lower(), tokens[1:]


def searches_tree(command):
    parts = re.split(r"(\|\||&&|[|;&])", command)
    for index, part in enumerate(parts):
        if index % 2:
            continue
        name, args = split_segment(part)
        if name not in EXPLORE:
            continue
        paths = [a for a in args if not a.startswith("-") and ("/" in a or "\\" in a or a.endswith(".md"))]
        if paths:
            if not all(DOC_PATH.search(p) for p in paths):
                return True
        elif name in TREE_BY_DEFAULT:
            return True
    return False


message = None
if UNITY_REST.search(cmd):
    message = (
        "주 세션에서 Unity REST를 직접 호출하고 있습니다. 컴파일·테스트·폴링은 "
        "tessera-verifier, 씬·프리팹 조작은 tessera-unity-operator에 위임하고 판정만 받습니다."
    )
elif searches_tree(cmd):
    message = (
        "주 세션에서 셸로 코드를 탐색하고 있습니다. 위치·호출 관계 조사는 tessera-scout에 "
        "위임하거나 graphify query를 먼저 씁니다. 특정 줄만 필요하면 Read(offset/limit)를 씁니다."
    )

if message:
    out = {"hookSpecificOutput": {"hookEventName": "PreToolUse", "additionalContext": message}}
    sys.stdout.buffer.write(json.dumps(out, ensure_ascii=False).encode("utf-8"))
