---
paths:
  - "Assets/Editor/Tests/**/*.cs"
---

# EditMode 테스트

- 테스트 정책(우선 검증 대상, 플레이 모드 확인 목록, 회귀 원칙)의 원본은 `docs/plan/process.md` §7입니다.
- 네임스페이스는 `Tessera.Editor.Tests` 하나로 통일돼 있습니다. 검증 담당이 이 네임스페이스로 필터를 걸어 실행하므로, 다른 네임스페이스에 둔 테스트는 실행되지 않습니다.
- 새 규칙에는 정상·경계·거부 사례 테스트를 함께 둡니다(`process.md` §6).
- `[Ignore]` 와 `Assert.Ignore` 를 쓰지 않습니다. 현재 저장소에 한 건도 없으며, Tessera 소속 테스트는 실패 0·스킵 0이 기준입니다.
- 증강 기능을 바꾸면 일반 요트 테스트도 함께 돌립니다(`process.md` §7.4).
