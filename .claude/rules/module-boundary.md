---
paths:
  - "Assets/02.Scripts/**/*.cs"
---

# 모듈 경계

- 모듈 목록과 폴더↔네임스페이스 대응의 원본은 `docs/reference/architecture_overview.md` 모듈 트리입니다. 목록을 다른 문서에 다시 적지 않습니다.
- 새 파일의 네임스페이스는 폴더 이름이 아니라 같은 폴더의 기존 파일을 따릅니다.
- 레이어별 해야 할 것·하지 말 것의 원본은 `docs/reference/control_manifest.md` 입니다. 수정할 파일이 속한 레이어 절을 읽고 따릅니다. 빌드 통과는 경계 준수의 근거가 아닙니다(사유는 매니페스트 머리말).
- 상태 소유권 판단 근거는 `docs/reference/architecture_decisions.md` 의 ADR입니다.
