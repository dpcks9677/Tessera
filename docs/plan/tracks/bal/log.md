# BAL 세션 로그

> 최신 항목이 맨 위로 오는 역순입니다. 양식은 `docs/plan/process.md` §5.3을 따릅니다.

### 2026-10-25 — 주 세션 (원격 통합과 결정 재번호)
- 작업 ID: BAL 등록 커밋 정리
- 변경: 원격 브랜치의 PIXEL 결정(`D-047`~`D-054`)과 번호가 겹쳐, 푸시 전에 KLSS `D-047`을 `D-055`로, BAL `D-048`~`D-051`을 `D-056`~`D-059`로 바꿈. 이 로그와 트랙 문서의 결정 번호도 새 번호로 바꿈
- 검증: 원격 위에 KLSS·BAL 변경을 다시 적용하고 `STATUS.md`·`process.md`·`decisions.md` 충돌을 양쪽 내용을 살려 해결. graphify 라벨은 원격 것 유지. 코드 변경 없음
- 다음 작업: 권장 시작점은 `BAL-T1`

### 2026-10-25 — 주 세션 (섹션 이름·순서, Hills 기권, 태스크 우선순위)
- 작업 ID: `BAL-T3`·`BAL-T4`·`BAL-T5` 수락 기준 갱신 (`D-059`), 태스크 우선순위 정렬과 재번호
- 변경: `decisions.md` `D-059`, 이 트랙 README(태스크 표 순서·수락 기준, 우선순위 목록, 관련 결정), `implementation.md`(섹션 용어, 시간 초과 X, T5 X 기입, T4 행 배치, 착수 때 정할 것)
- 검증: 사용자 지시로 확정(Hands → Combinations, Numbers·Hills·Combinations 순서, 시간 초과로 Hills에 들어간 기입은 X 기권). 우선순위는 주 세션 판단, 재번호는 사용자 수락. ID가 커밋 전이라 이 로그와 `D-056`~`D-059`의 옛 ID도 새 번호로 바꿈(대응표는 `roadmap.md`). 코드 변경 없음
- 다음 작업: 권장 시작점은 `BAL-T1`. 착수 때 첫 드래프트(4라운드) 순서 규칙부터 정함

### 2026-10-25 — 주 세션 (단계 분류와 구현 태스크 등록)
- 작업 ID: `BAL-T1`~`BAL-T6` 등록 (`D-058`)
- 변경: `decisions.md` `D-058`, `phases.md`·`implementation.md` 신설, 이 트랙 README(목표, 범위, 완료 게이트, 태스크 표, 순서, 관련 결정), `STATUS.md` 트랙 줄
- 검증: 단계 분류는 사용자와 질의응답으로 확정(Timeless 신설, Yacht 칸 변형과 `random-box`는 Timeless, `fast-straight`는 Middlegame 전 마감). 코드 사실은 `tessera-scout` 두 번(드래프트·절대 턴 상수, 구현 영향 범위)으로 확인. 코드 변경 없음
- 다음 작업: 사용자가 `BAL-Tnn`을 지목하면 착수. 권장 시작점은 `BAL-T1`

### 2026-10-25 — 주 세션 (기본 규칙 변경 결정)
- 작업 ID: BAL 범위 영향 (`D-057`)
- 변경: `decisions.md` `D-057`, 이 트랙 README 「관련 결정」
- 검증: 사용자와 질의응답으로 확정. Hills 확률은 한 칸 DP(최적 재굴림, 3회 굴림)로 계산함. Top 합 25 이상 41.7%, Bottom 합 10 이하 41.7%(Top과 대칭), Halfway 정확히 15는 35.1%, 14~16은 77.8%. 코드 변경 없음
- 다음 작업: 기존 증강의 단계(Opening·Middlegame·Endgame) 분류, `BAL-T7` 착수 전 범위 재검토, 게임 구현 계획 등록 여부 결정

### 2026-10-24 — 주 세션 (BAL 트랙 등록)
- 작업 ID: BAL 등록
- 변경: `docs/plan/tracks/bal/README.md`·`log.md` 신설, `STATUS.md` 트랙 줄, `process.md` §1 트랙 목록, `decisions.md` `D-056`, `tessera-task/references/locate.md` 접두사, `tessera-gate/SKILL.md` 트랙명 예시
- 검증: 평가 기준은 사용자와 질의응답으로 확정(닌텐도판 기본 규칙, 판정 규칙, 분류별 지표, 도구 방식). 코드 변경 없음
- 다음 작업: 사용자가 `BAL-Tnn`을 지목하면 착수. 권장 시작점은 `BAL-T7` 컴파일 가능성 확인
