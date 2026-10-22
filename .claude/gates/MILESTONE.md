# MILESTONE — 마일스톤 종료 점검

> 실행 주체: 주 세션 + `tessera-reviewer`(`lean`·`full`) | 모델 티어: opus / sonnet

**트리거**: 마일스톤·트랙의 마지막 태스크가 끝나고 커밋 허가를 요청하기 전. `/tessera-gate` 가 이 게이트를 실행합니다.

**넘길 맥락**: 마일스톤 `README.md`(태스크 표, 완료 게이트), 마일스톤 세션 로그, 마일스톤 범위 커밋 범위 또는 미커밋 변경 목록.

## 점검 항목

1. **태스크 상태** — `docs/plan/process.md` §8 1항대로 모든 행이 `DONE`, `DEFERRED`, `DROPPED`, `VOID` 중 하나입니다. `DEFERRED`·`DROPPED` 에는 사유가 있습니다.
2. **완료 게이트** — README 완료 게이트의 항목을 하나씩 대조하고 증거를 붙입니다.
3. **기록** — `DONE` 태스크마다 세션 로그 항목이 있고, 기록된 `NOT ASSESSED` 가 해소됐거나 사용자가 수락했습니다.
4. **계획과 코드의 일치** — 태스크 표의 상태와 실제 코드·커밋이 맞습니다. 어긋남이 의심되면 `/tessera-plan-sync` 로 확인합니다.
5. **하위 게이트** — `lean`·`full` 이면 마일스톤 범위 전체로 `CODE-REVIEW`·`TEST-COVERAGE` 를 `tessera-reviewer` 에 병렬로 맡기고 판정을 합칩니다. `solo` 는 생략하고 그 사실을 적습니다.
6. **커밋 범위** — 이 마일스톤과 무관한 변경 파일이 커밋 대상에 섞이지 않습니다.

## 판정

- `FAIL`: 상태가 정리되지 않은 태스크가 있음, 완료 게이트 미충족, 하위 게이트 `FAIL`.
- `CONCERNS`: 사유 없는 `DEFERRED`·`DROPPED`, 하위 게이트 `CONCERNS`, 무관한 변경 파일이 섞임.
- `NOT ASSESSED`: 해소되지 않은 `NOT ASSESSED` 기록이 남음.
- `PASS`: 위 항목을 모두 충족. 이어서 `CLAUDE.md` 「커밋 & 푸시」대로 사용자에게 커밋 허가를 요청합니다.
