# ADR — 아키텍처 결정 승인 점검

> 실행 주체: 주 세션(`full` 이면 `tessera-reviewer`) | 모델 티어: opus / sonnet

**트리거**: ADR 상태를 `Proposed` 에서 `Accepted` 로 바꾸기 전.

**넘길 맥락**: 대상 ADR 번호와 `docs/reference/architecture_decisions.md` 경로, 이 결정을 낳은 태스크 ID.

ADR 필드 형식과 상태 수명주기의 원본은 `docs/reference/architecture_decisions.md` 「ADR 양식」 절입니다. 아래 항목은 그 필드를 점검하는 기준입니다.

## 점검 항목

1. **맥락과 결정** — 무엇이 문제였고 무엇을 정했는지가 각각 한 문단 이상으로 드러납니다.
2. **검토한 대안** — 버린 대안이 하나 이상 있고, 버린 이유가 적혀 있습니다.
3. **영향 범위** — 「레이어」 칸이 채워져 있고, `docs/reference/control_manifest.md` 의 다른 레이어 규칙과 충돌하지 않습니다.
4. **기존 결정과의 관계** — 기존 ADR과 모순되지 않습니다. 대체한다면 「대체」 칸에 번호가 있고, 승인 시 대체되는 ADR이 `Superseded by ADR-nnn` 이 됩니다.
5. **통제 매니페스트** — 결정이 레이어별 해야 할 것·하지 말 것을 바꾸면 추가·삭제할 매니페스트 규칙이 절별로 정해져 있습니다.

## 판정

- `FAIL`: 기존 `Accepted` ADR과 모순되는데 대체 표기가 없음, 모듈 경계 규칙과 충돌.
- `CONCERNS`: 검토한 대안이 없음, 영향 범위가 비었음, 매니페스트 갱신 누락.
- `NOT ASSESSED`: 대상 ADR을 찾지 못함.
- `PASS`: 위 항목을 모두 충족.
