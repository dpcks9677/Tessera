---
name: tessera-adr
description: Tessera의 아키텍처 결정 기록(ADR)을 새로 쓰거나 Proposed ADR을 Accepted로 승인한다. 승인 시 ADR 게이트를 돌리고 사용자 확인을 받은 뒤 통제 매니페스트를 함께 갱신한다.
argument-hint: new <제목> | accept ADR-nnn [--review solo|lean|full]
model: opus
effort: high
---

# ADR 작성과 승인

인자: `$ARGUMENTS`

ADR은 설계 판단입니다. **본문은 주 세션이 직접 씁니다.** 코드 사실 확인은 `tessera-scout` 에 맡기고, 요약만 받습니다.

## 원본

- ADR 필드 형식·상태 수명주기: `docs/reference/architecture_decisions.md` 「ADR 양식」 절. 쓰기 전에 그 절을 읽습니다.
- 레이어별 해야 할 것·하지 말 것: `docs/reference/control_manifest.md`
- 승인 점검 기준: `.claude/gates/ADR.md`. 판정 어휘와 출력 형식은 `.claude/gates/README.md`

## `new <제목>`

1. 목록 표에서 마지막 번호를 확인하고 다음 번호를 씁니다. 번호는 재사용하지 않습니다.
2. 맥락이 코드에 기대면 `tessera-scout` 에 사실 확인을 맡깁니다. 근거 절의 코드 위치는 확인된 것만 씁니다.
3. 양식 순서대로 씁니다. 상태는 항상 `Proposed` 입니다. 검토한 대안이 하나도 없으면 사용자에게 묻습니다.
4. 목록 표에 행을 추가합니다. 짧은 요지가 필요하면 `docs/decisions.md` 에 `D-nnn` 한 줄을 함께 두고 서로 링크합니다.
5. 매니페스트는 바꾸지 않습니다. `Proposed` 는 아직 규칙이 아닙니다.

## `accept ADR-nnn`

1. 대상 ADR의 상태를 읽습니다.
   - 이미 `Accepted` → 중단하고 알립니다.
   - `Deprecated`·`Superseded` → 거부합니다. 새 ADR이 필요하다고 알립니다.
   - 대상이 없으면 `NOT ASSESSED` 로 보고합니다.
2. `ADR` 게이트를 돌립니다. `review_mode` 기본값은 `lean`(주 세션), `full` 이면 `tessera-reviewer` 에 게이트 파일 경로를 넘깁니다. `FAIL` 이면 멈춥니다.
3. 판정과 함께 바뀔 내용(상태, 대체 대상, 매니페스트 절별 추가·삭제 규칙)을 보이고 **사용자 확인을 받습니다.** 확인 없이 `Accepted` 로 바꾸지 않습니다.
4. 확인되면 반영합니다.
   - 대상 ADR 상태를 `Accepted`, 날짜를 오늘로. 목록 표도 같이 고칩니다.
   - 「대체」 칸의 ADR은 상태를 `Superseded by ADR-nnn` 으로 바꿉니다.
   - 매니페스트 해당 레이어 절에 규칙을 넣고 출처를 이 ADR로 답니다. 대체된 ADR을 출처로 한 규칙은 지우거나 출처를 옮깁니다. 머리말 「반영 ADR」과 「코드 대조 기준」을 갱신합니다.
   - 대상 ADR의 「매니페스트 반영」 칸을 채웁니다.
5. 이 ADR에 기대던 태스크가 있으면 그 태스크의 준비 점검을 다시 돌려야 한다고 보고합니다.

## 기록

세션 로그 항목은 `tessera-scribe` 에 맡깁니다. 수락한 `CONCERNS` 는 `.claude/gates/README.md` 「기록」 절 형식으로 남깁니다.

**커밋하지 않습니다.**
