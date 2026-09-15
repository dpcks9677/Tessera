# PreToolUse hook (Bash|PowerShell): nudges the main session to delegate.
#
# Transcript logs showed the main Opus session running hundreds of small shell
# turns (Unity REST curl polling, grep/cat exploration), each one re-reading a
# 200k+ token context. Those calls belong in subagents, so this hook adds a
# reminder when the main session makes them. It never blocks.
#
# Hook input carries agent_id only inside a subagent, so subagents are exempt.

import json
import re
import sys

data = json.loads(sys.stdin.buffer.read().decode("utf-8") or "{}")
if data.get("agent_id"):
    sys.exit(0)

cmd = str((data.get("tool_input") or {}).get("command", ""))

UNITY_REST = re.compile(r"127\.0\.0\.1:\d+/(skill|skills|permission)\b")
SHELL_EXPLORE = re.compile(
    r"(^|[|;&(]\s*)(grep|rg|find|cat|head|tail|sed|awk|Select-String|Get-Content)\b"
)
EXEMPT = re.compile(r"^\s*(cd\s+[^;&]+[;&]+\s*)?(\w+=\S*\s+)*(git|graphify)\b")

message = None
if UNITY_REST.search(cmd):
    message = (
        "주 세션에서 Unity REST를 직접 호출하고 있습니다. 컴파일·테스트·폴링은 "
        "tessera-verifier, 씬·프리팹 조작은 tessera-unity-operator에 위임하고 판정만 받습니다."
    )
elif SHELL_EXPLORE.search(cmd) and not EXEMPT.search(cmd):
    message = (
        "주 세션에서 셸로 코드를 탐색하고 있습니다. 위치·호출 관계 조사는 tessera-scout에 "
        "위임하거나 graphify query를 먼저 씁니다. 특정 줄만 필요하면 Read(offset/limit)를 씁니다."
    )

if message:
    out = {"hookSpecificOutput": {"hookEventName": "PreToolUse", "additionalContext": message}}
    sys.stdout.buffer.write(json.dumps(out, ensure_ascii=False).encode("utf-8"))
