# 게이트

태스크와 마일스톤의 진행 여부를 판정하는 점검 정의입니다. 게이트 하나가 파일 하나입니다. 스킬은 게이트 내용을 프롬프트에 복사하지 않고 **게이트 파일 경로**를 넘기며, 실행 주체가 그 파일을 직접 읽습니다.

## 판정 어휘

| 판정 | 뜻 | 다음 행동 |
|---|---|---|
| `PASS` | 점검 항목을 모두 충족 | 진행 |
| `CONCERNS` | 진행은 가능하나 확인할 문제가 있음 | 사용자에게 보여 주고, 수락하면 진행. 수락 사실을 세션 로그에 남김 |
| `NOT ASSESSED` | 판단에 필요한 입력이 없어 평가하지 못함 | 사용자에게 빠진 입력을 묻습니다. **`PASS`로 취급하지 않습니다** |
| `FAIL` | 진행하면 안 되는 문제가 있음 | 멈추고 보고. 고친 뒤 게이트를 다시 돌림 |

- 여러 게이트를 합칠 때는 가장 엄한 판정을 채택합니다. `FAIL` > `CONCERNS` > `NOT ASSESSED` > `PASS`.
- 문제와 빠진 입력이 함께 있으면 문제 쪽 판정을 내리고, 빠진 입력은 "평가하지 못한 항목"에 따로 적습니다.
- 대표적인 `NOT ASSESSED`: 테스트 실행이 `MODE_FORBIDDEN` 으로 막힘, 플레이 모드에서만 확인 가능한 항목, 점검 대상 문서나 diff 범위를 특정하지 못함.

## review_mode

**기본값: `lean`.** 스킬 인자 `--review solo|lean|full` 로 그 호출에 한해 덮어씁니다.

| 게이트 | 시점 | `solo` | `lean` | `full` |
|---|---|---|---|---|
| [`READY`](READY.md) | 태스크 착수 전 | 생략 | 주 세션 | 주 세션 |
| [`DONE-CRITERIA`](DONE-CRITERIA.md) | `DONE` 표시 전 | 주 세션 | 주 세션 | 주 세션 |
| [`CODE-REVIEW`](CODE-REVIEW.md) | 태스크 구현 후 | 생략 | 생략 | `tessera-reviewer` |
| [`TEST-COVERAGE`](TEST-COVERAGE.md) | 태스크 구현 후 | 생략 | 생략 | `tessera-reviewer` |
| [`ADR`](ADR.md) | ADR을 `Accepted` 로 바꾸기 전 | 주 세션 | 주 세션 | `tessera-reviewer` |
| [`MILESTONE`](MILESTONE.md) | 마일스톤 종료 시 | 주 세션 | 주 세션 + `CODE-REVIEW`·`TEST-COVERAGE` reviewer | 같음 |

`DONE-CRITERIA` 는 `docs/plan/process.md` §6이 정한 완료 조건이라 어느 모드에서도 생략하지 않습니다.

## 실행 방법

**주 세션이 실행할 때**: 게이트 파일을 읽고 점검 항목을 하나씩 대조한 뒤 아래 출력 형식으로 판정을 사용자에게 보입니다.

**`tessera-reviewer` 에 넘길 때**: 프롬프트에 다음만 싣습니다. 게이트 본문은 싣지 않습니다.

```
게이트 파일: .claude/gates/<이름>.md  (먼저 읽고 그 기준으로만 판정)
대상: <파일 목록 또는 커밋 범위>
맥락: <게이트 파일 "넘길 맥락" 절이 요구하는 항목>
```

## 출력 형식

```
게이트: <이름>
판정: PASS | CONCERNS | NOT ASSESSED | FAIL
근거:
- <위치> — <무엇이 어떤 기준에 어긋나는지>
평가하지 못한 항목:
- <항목> — <빠진 입력>
```

## 기록

- `NOT ASSESSED` 는 세션 로그 검증 항목에 `NOT ASSESSED YYYY-MM-DD — <빠진 입력>` 으로 남깁니다.
- 사용자가 수락한 `CONCERNS` 는 `CONCERNS 수락 YYYY-MM-DD — <요지>` 로 남깁니다.
