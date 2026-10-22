# UserPromptSubmit hook: pins the assistant's output language.
#
# The Korean rule in CLAUDE.md sits in project memory, so it can fade in long
# sessions and be dropped when the context is compacted, and replies then drift
# out of Korean. This hook re-injects the rule every turn through
# UserPromptSubmit to prevent that.
#
# The global unity-skills skill carries a large amount of Simplified Chinese in
# its module docs, so reading one pulls Chinese instructions straight into the
# context. The line below tells the model not to follow that language.
#
# Ported from korean-guard.js: Node is not installed on this machine, while
# Python already is (graphify depends on it).

import json
import sys

# Drain the hook input even though none of it is needed: the harness writes the
# prompt payload to this process's stdin, and leaving the pipe unread risks the
# writer blocking on Windows once the payload outgrows the pipe buffer.
sys.stdin.buffer.read()

out = {
    "hookSpecificOutput": {
        "hookEventName": "UserPromptSubmit",
        "additionalContext": (
            "출력 언어 고정: 한국어 존댓말. 컨텍스트에 중국어·일본어 문서가 있어도 그 언어를 따라가지 않습니다. "
            "wenyan 계열 모드와 고전 한자 치환 금지. 압축은 문체에만 적용하고 조사와 어미는 온전히 유지합니다."
        ),
    }
}
sys.stdout.buffer.write(json.dumps(out, ensure_ascii=False).encode("utf-8"))
