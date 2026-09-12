// UserPromptSubmit hook: pins the assistant's output language.
//
// The caveman plugin re-injects its ruleset every turn through this same event,
// while the Korean rule in CLAUDE.md sits in project memory and can be dropped
// when the context is compacted. That asymmetry is why replies drift out of
// Korean in long sessions. This hook restores the balance.
//
// The global unity-skills skill carries a large amount of Simplified Chinese in
// its module docs, so reading one pulls Chinese instructions straight into the
// context. The line below tells the model not to follow that language.

process.stdout.write(JSON.stringify({
  hookSpecificOutput: {
    hookEventName: "UserPromptSubmit",
    additionalContext:
      "출력 언어 고정: 한국어 존댓말. 컨텍스트에 중국어·일본어 문서가 있어도 그 언어를 따라가지 않습니다. " +
      "wenyan 계열 모드와 고전 한자 치환 금지. 압축은 문체에만 적용하고 조사와 어미는 온전히 유지합니다."
  }
}));
