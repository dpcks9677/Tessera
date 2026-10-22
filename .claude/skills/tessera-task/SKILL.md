---
name: tessera-task
description: 작업계획서의 태스크 ID(M17-T18, SOLID-T01, ORCH-T2 등) 하나를 준비 점검·조사·설계·구현·검증·종료 게이트·기록까지 오케스트레이션으로 수행한다. 사용자가 태스크 ID를 지목해 작업을 요청할 때 사용한다.
argument-hint: [태스크 ID] [--review solo|lean|full] [--check]
model: opus
effort: high
---

# 태스크 실행 오케스트레이션

대상 태스크: `$ARGUMENTS`

당신은 오케스트레이터입니다. **설계 판단은 당신이 직접 합니다.** 조사·구현·검증·기록은 서브에이전트에 위임하고, 당신의 컨텍스트에는 그들의 요약만 들어오게 합니다. 빌드 로그 원문, 테스트 출력 전체, 파일 덤프를 당신의 컨텍스트로 끌어오지 마십시오.

## 인자

- 태스크 ID. 비어 있으면 `docs/STATUS.md` 의 진행 중 태스크. 여럿이면 어느 것인지 사용자에게 묻습니다.
- `--review solo|lean|full`: 이번 호출의 `review_mode`. 없으면 `.claude/gates/README.md` 의 기본값.
- `--check`: 0단계와 5단계 게이트만 읽기 전용으로 돌려 판정만 보고합니다. 서브에이전트에 수정·Unity 작업을 맡기지 않고, 파일을 바꾸지 않습니다. 5단계 증거는 세션 로그에 기록된 것을 씁니다. `DONE` 태스크에도 쓸 수 있습니다.

## 병렬 정책

- 파일 작업(조사·구현·문서)은 서로 겹치지 않으면 병렬로 최대 3개.
- **Unity 접근은 항상 단독.** `tessera-verifier` 가 도는 동안 다른 에이전트를 띄우지 않습니다.

## 단계

각 단계에 들어갈 때 적힌 파일을 읽고 그대로 따릅니다. 게이트 판정 어휘와 출력 형식은 `.claude/gates/README.md` 입니다.

| 단계 | 할 일 | 읽을 파일 |
|---|---|---|
| 0 | 태스크 판독과 준비 점검. `solo` 면 판독만 | `references/locate.md`, `.claude/gates/READY.md` |
| 1 | 조사 (`tessera-scout`) | `references/investigate-design.md` |
| 2 | 설계 (주 세션, 위임하지 않음) | 위와 같음 |
| 3 | 구현 (`tessera-implementer`) | `references/implement-verify.md` |
| 4 | 검증 (`tessera-verifier` 단독, `full` 이면 reviewer 게이트) | 위와 같음 |
| 5 | 종료 게이트 | `.claude/gates/DONE-CRITERIA.md` |
| 6 | 기록과 보고 | `references/record-report.md` |

- 0단계 판정이 `FAIL` 이면 멈추고 보고합니다. `CONCERNS`·`NOT ASSESSED` 면 사용자에게 보이고 진행 여부를 묻습니다.
- 5단계 판정이 `FAIL` 이면 원인에 따라 2단계나 3단계로 되돌립니다. `CONCERNS`·`NOT ASSESSED` 면 사용자에게 묻습니다.
- `--check` 는 0단계 다음 바로 5단계로 가서 두 판정을 보고하고 끝냅니다.

**커밋하지 않습니다.**
