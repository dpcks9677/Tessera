# 마일스톤 로드맵

> **문서 종류**: 작업 지시서 (AI 대상)
> **코드 대조 기준**: 2026-10-22 · 커밋 `6527034` (내용은 2026-09-20 기준 `work_plan.md`에서 옮김)
> 마일스톤 상태의 원본입니다. 태스크 상태는 각 마일스톤 폴더의 `README.md`에 있습니다.

| ID | 마일스톤 | 상태 | 선행 작업 | 핵심 결과물 | 문서 |
|---|---|---|---|---|---|
| `M0` | 작업 기준선과 계획서 | `DONE` | 없음 | 이 문서 | [completed](../archive/milestones/completed.md) |
| `M1` | 기존 증강 조사 및 이식 명세 | `DONE` | `M0` | 55개 증강 이식표, 테스트 기준선 | [completed](../archive/milestones/completed.md) |
| `M2` | 권위 독립형 기본 게임 코어 | `DONE` | `M1` | 상태·명령·결과·난수·로컬 권위 | [completed](../archive/milestones/completed.md) |
| `M3` | 일반 요트 로컬 핫시트 | `DONE` | `M2` | 일반 게임 전체 완주 가능 | [completed](../archive/milestones/completed.md) |
| `M4` | 일반/증강 모드 분리 | `DONE` | `M3` | 공유 씬 모드 전환 | [completed](../archive/milestones/completed.md) |
| `M5` | 대표 증강 수직 구현 | `DONE` | `M4` | 서로 다른 유형의 증강 5종 | [completed](../archive/milestones/completed.md) |
| `M6` | 증강 55개 전체 이식 및 명시적 제외 처리 | `DONE` | `M5` | 45개 구현·6개 삭제·4개 미구현 확정과 회귀 테스트 | [completed](../archive/milestones/completed.md) |
| `M7` | 증강 시스템 그래픽 요소 추가 | `DONE` | `M6` | 증강 카드 공통 비주얼·스크롤 4종·상태 표현·트레이 배치 (T1~T4). T5~T12(특수 주사위·VFX·상태 표시·폴백·검증)는 `M17`로 이동 (`D-038`) | [completed](../archive/milestones/completed.md) |
| `M8` | 증강 시스템 구조 리팩토링 | `DONE` | `M6` | 발동 시점 인터페이스, 증강별 핸들러·상태 분리, 증강별 회귀 테스트 | [completed](../archive/milestones/completed.md) |
| `M9` | 테이블 오브젝트 프리팹 이관 | `DONE` | `M8` | 프롭 프리팹화, 씬 저작 전환, 코딩 규약·ADR 문서 | [completed](../archive/milestones/completed.md) |
| `M10` | 족보 표·증강 카드 UI 월드 스페이스 전환 | `DONE` | `M9` | 계층 기반 배치, 픽셀 필터 우회 전용 카메라 | [completed](../archive/milestones/completed.md) |
| `M11` | 프레젠테이션 계층 리팩토링 | `DONE` | `M9` | 컨트롤러 분해, 프레젠테이션 상태 통합 | [completed](../archive/milestones/completed.md) |
| `M12` | 픽셀 필터 엣지 검출 도입 | `DONE` | `M11` | 뎁스 외곽선·노멀 하이라이트, 기존 필터와 A/B 전환 | [completed](../archive/milestones/completed.md) |
| `M13` | 픽셀 필터 색 양자화 | `DONE` | `M12` | sRGB 포스터라이즈·아트 가이드 팔레트·Bayer 디더, 3단 모드 전환 | [completed](../archive/milestones/completed.md) |
| `M14` | 트레이 확대와 보드 좌표 상수 단일화 | `DONE` | `M13` | 트레이 배율 0.06, 좌표 상수가 `DiceBoardMetrics` 한 곳에서만 정의됨 | [completed](../archive/milestones/completed.md) |
| `M15` | Unity 물리 기반 주사위 프리셋 베이커 | `DONE` | `M14` | 프리셋을 Unity PhysX로 굽고 분포·인식도·판정 시간으로 평가 | [completed](../archive/milestones/completed.md) |
| `M16` | 셀 셰이딩 전환과 픽셀 격자 정합 | `DEFERRED` | `M14` | Baseline/Cel A/B·셀 셰이더·측정 도구 인프라 완료. 완료 조건 미충족·전제 오류로 채택 보류, 기본값 Baseline 유지 (`D-037`) | [completed](../archive/milestones/completed.md) |
| `M17` | 증강 요트 로컬 핫시트 완성 | `DOING` | `M11` | 전체 증강 게임 완주 + 완주에 필요한 그래픽(특수 주사위·VFX·상태 표시). 구 `M7-T5~T12` 흡수 (`D-038`). 일부 태스크 진행 중 | [m17](m17/README.md) |
| `M18` | 네트워크 준비 검증 | `TODO` | `M17` | 지연·중복·재동기화 대응 | [m18](m18/README.md) |
| `M19` | EOS/Steam 호스트 온라인 | `TODO` | `M18` | 비레이팅 온라인 매치 | [m19](m19/README.md) |
| `M20` | 원격 권위/레이팅 | `DEFERRED` | 별도 기획 | 필요할 때만 진행 | [m20](m20/README.md) |

마일스톤 번호는 두 번 재조정했다.

- `D-029`(2026-09-04): 기존 `M7.5`→`M8`, 기존 `M8`→`M11`, `M9`→`M12`, `M10`→`M13`, `M11`→`M14`로 밀렸고 `M9`·`M10`이 신설됐다.
- `D-035`(2026-09-06): 소수점 마일스톤을 없애고 전부 정수로 다시 매겼다. `M9.5`→`M10`, `M10`→`M11`, `M10.5`→`M12`, `M10.6`→`M13`, `M10.7`→`M14`, `M10.9`→`M15`, `M10.8`→`M16`, `M11`→`M17`, `M12`→`M18`, `M13`→`M19`, `M14`→`M20`. 하위 작업 ID도 함께 이동했다(`M10.6-T4`→`M13-T4` 등).

결정 기록(`docs/decisions.md`)과 세션 로그의 과거 항목은 기록 보존을 위해 원래 ID를 그대로 둔다. 코드 주석의 마일스톤 표기도 커밋 시점을 가리키는 역사 기록이므로 바꾸지 않는다.
