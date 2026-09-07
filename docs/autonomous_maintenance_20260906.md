# 자율 보수 작업 기록 (2026-09-06)

이 문서는 사용자가 지시한 자율 탐색·보수 작업의 결과다. **여기 적힌 변경은 커밋하지 않았다.**
채택 여부는 사용자가 정한다. 되돌리려면 `git checkout -- docs/`와 `docs/archive/`의 이동 파일 복구로 충분하다.

작성 시점 기준 커밋된 마지막 작업은 `26953db feat: 재료 단계 셀 셰이딩 전환과 저해상도 실렌더 도입` (당시 표기 `M10.8`, 아래 재번호 후 `M16`)이다.

## 진행 상태

| 항목 | 상태 |
|---|---|
| 1. 마일스톤 번호 정수화 + M7 문서 통합 | 완료 |
| 2. 코드 리뷰 (`docs/code_review_20260906.md`) | 완료 |
| 3. graphify 갱신 | 완료 |

---

## 항목 1. 마일스톤 번호 정수화와 M7 문서 통합

### 1.1 마일스톤 번호 정수화

소수점 마일스톤(`M9.5`, `M10.5`~`M10.9`)을 없애고 전부 정수로 다시 매겼다. `D-035` 결정 기록을 추가했고, `D-033`(그래픽 트랙만 소수점 연장)을 대체한다.

| 이전 | 이후 | 제목 |
|---|---|---|
| `M9.5` | `M10` | 족보 표·증강 카드 UI 월드 스페이스 전환 |
| `M10` | `M11` | 프레젠테이션 계층 리팩토링 |
| `M10.5` | `M12` | 픽셀 필터 엣지 검출 도입 |
| `M10.6` | `M13` | 픽셀 필터 색 양자화 |
| `M10.7` | `M14` | 트레이 확대와 보드 좌표 상수 단일화 |
| `M10.9` | `M15` | Unity 물리 기반 주사위 프리셋 베이커 |
| `M10.8` | `M16` | 셀 셰이딩 전환과 픽셀 격자 정합 |
| `M11` | `M17` | 증강 요트 로컬 핫시트 완성 |
| `M12` | `M18` | 네트워크 준비 검증 |
| `M13` | `M19` | EOS/Steam 호스트 온라인 |
| `M14` | `M20` | 원격 권위 서버와 레이팅 |

`M10.9`가 `M10.8`보다 먼저 끝났으므로 완료 순서를 따라 `M15`/`M16`을 배정했다. 요약 표의 행 순서와 §7 본문 블록 순서도 번호 순으로 맞바꿨다.

하위 작업 ID는 함께 이동한다(`M10.6-T4` → `M13-T4`).

**바꾸지 않은 곳.** `D-029`가 세운 선례를 따른다.

- `§11 결정 기록`과 `§13 작업 세션 로그`의 과거 항목. 그 시점의 ID가 곧 기록이다.
- 코드 주석의 마일스톤 표기(`(M10.8)` 등). 커밋 시점을 가리키는 역사 기록이므로 손대지 않았다. 대신 재번호 매핑을 `§6` 표 아래 각주와 `D-035`에 남겨 추적할 수 있게 했다.

**작업 중 잡은 오류 두 건.**

1. 처음 쓴 정규식이 `\bM10\.6\b`였는데, `M10.6이`처럼 한글 조사가 붙으면 `6` 뒤에 단어 경계가 없어 매칭이 실패한다. 그러면 안쪽의 `M10`이 대신 잡혀 `M11.6이`가 된다. 앞은 `(?<![A-Za-z0-9])`, 뒤는 `(?![0-9])(?!\.[0-9])`로 바꿔 해결했다. 뒤에 점 자체는 막지 않아야 `## M9.5.` 같은 제목도 잡힌다.
2. `augment_migration_matrix.md`의 "M9/M10의 어댑터 참고" 줄은 문서 작성 시점(2026-08-24, `D-029` 이전) 번호였다. 그때 `M9`는 네트워크 준비 검증, `M10`은 EOS/Steam이었으므로 현재 번호로는 `M18`/`M19`다. 기계적 치환이 이를 `M9/M11`로 잘못 바꿔 놓아 손으로 고치고 당시 번호를 괄호로 남겼다.

변경 파일: `docs/augmented_yacht_work_plan.md`, `docs/architecture_decisions.md`, `docs/art_style_guide.md`, `docs/augment_migration_matrix.md`, `docs/cel_shading_pixel_plan.md`, `docs/pixel_edge_filter_plan.md`, `docs/augmented_yacht_m7_graphics_plan.md`

### 1.2 M7 문서 통합

M7 관련 문서가 넷으로 흩어져 있었다. `D-018`은 이미 `augmented_yacht_m7_graphics_plan.md`를 단일 기준으로 정해 두었는데, 나머지 셋이 별도 파일로 남아 무엇이 기준인지 찾기 어려웠다.

셋을 `augmented_yacht_m7_graphics_plan.md` `§6 부록`으로 합치고 원본은 `docs/archive/`로 옮겼다.

| 원본 | 이후 |
|---|---|
| `docs/augmented_yacht_m7_asset_inventory.md` (162줄) | 부록 A. 증강 그래픽 에셋 인벤토리 |
| `docs/augmented_yacht_augment_card_design_revision_plan.md` (213줄) | 부록 B. 증강 카드 디자인 수정 사양 |
| `docs/augmented_yacht_m7_scroll_redesign_plan.md` (210줄) | 부록 C. 증강 카드 3D 스크롤 리디자인 |

합친 문서는 285줄에서 879줄이 됐다. 부록의 머리글은 한 단계씩 낮춰 목차가 깨지지 않게 했다.

살아 있는 상호 참조는 부록 앵커로 바꿨다.

- `augmented_yacht_m7_graphics_plan.md` 안의 세 참조
- `pixel_edge_filter_plan.md` 의 카드 디자인 문서 참조
- `augmented_yacht_work_plan.md` §7 M7 절에 부록 위치 안내 한 문장 추가

`§13 작업 세션 로그`의 "변경 파일" 목록에 남은 옛 경로는 그대로 뒀다. 그 시점에 실제로 바꾼 파일 이름이 기록이다.

---

## 항목 2. 코드 리뷰

결과는 별도 파일 [`docs/code_review_20260906.md`](code_review_20260906.md)에 있다. `CodeReviewGuide.md` 기준을 따랐다.

범위는 전수가 아니다. `M16`에서 새로 쓴 코드(자기 리뷰), 매 프레임 도는 입력·프레젠테이션 경로, 주사위 판정 로직, `M15` 프리셋 베이커를 봤다. Unity 컴파일도 테스트도 돌리지 않았으므로 전부 정적 읽기 기반이다.

### 발견 요약

| ID | 중요도 | 요지 | 조치 |
|---|---|---|---|
| `M16-1` | High | SSAO 피처를 런타임에 끄면 직렬화 필드라 렌더러 에셋에 그대로 남는다. Baseline으로 되돌려도 SSAO가 돌아오지 않아 M16의 롤백 계약이 깨진다 | 원래 상태를 기억하고 `OnDisable`에서 되돌리도록 수정 |
| `M16-2` | Medium | 검증 도구가 매 에디터 틱마다 1920x1080 텍스처를 새로 만들고 동기 `ReadPixels`를 한다. 하필 이 도구가 재는 지표가 프레임 간 변화율이라 측정이 측정 대상을 흔들고, Baseline 쪽 편향이 더 크다 | 텍스처 재사용 + 0.1초 고정 간격 캡처로 수정 |
| `M16-3` | Low | `CelStyleSwitcher`가 만든 재질을 아무도 파괴하지 않는다 | `Dispose()` 추가, 컨트롤러 `OnDestroy`에서 호출 |
| `M16-4` | Low | 셀 전환 이후 생성된 렌더러는 변환되지 않아 한 화면에 두 셰이딩이 섞인다 | 문서에 한계로 기록만 |
| `INPUT-1` | Medium | `Physics.RaycastAll`이 매 프레임 배열을 할당한다. 같은 파일에 비할당 패턴이 이미 있다. 겹친 카드에서 뒤쪽이 잡히는 문제도 함께 있음 | 이후 사용자 지시로 수정. 고정 버퍼 + `RaycastNonAlloc` + 최근접 선택 |
| `BAKE-1` | Low | 프리셋 초기 회전이 오일러 균등이라 SO(3) 균등이 아니다. 공정성에는 영향 없고 궤적 다양성에만 관여 | 미수정 (근거 약함) |
| `DOC-1` | Low | 요약 표의 `M12`·`M13`이 `TODO`인데 하위 작업은 전부 완료·커밋됨 | 미수정 (마일스톤 상태는 사용자 판단) |

### 수정한 파일

- `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtLightingRig.cs` (`M16-1`)
- `Assets/Editor/RunPixelReadabilityValidation.cs` (`M16-2`)
- `Assets/Scripts/Rendering/CelStyleSwitcher.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentedYachtController.cs` (`M16-3`)
- `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtInputRouter.cs` (`INPUT-1`, 사용자 지시로 추가 수정)

네 수정 모두 Unity 컴파일로 확인하지 않았다.

---

## 항목 3. graphify 갱신

`graphify update .` 실행 완료.

| 항목 | 결과 |
|---|---|
| AST 추출 | 79/79 파일 (캐시 미적중 100%) |
| 그래프 | 3,505 노드, 7,121 엣지, 228 커뮤니티 (프리뷰 동기화와 `INPUT-1` 수정까지 반영한 최종값) |
| 백업 | 이전 큐레이션 그래프 4개 파일을 `graphify-out/2026-09-06/`로 백업 |
| 경고 | 소스 28개가 노드를 만들지 않음(`settings.json`, `augments.json` 등 데이터·설정 파일). 재실행 시 자동 재시도 |
| 남은 작업 | 커뮤니티 집합이 바뀌어(저장된 라벨 190개 / 현재 커뮤니티 206개) 76개가 허브 이름으로 임시 개명됐다. 이름을 제대로 되살리려면 `graphify label`을 돌려야 하는데 LLM 호출 비용이 들어 실행하지 않았다 |

`graphify-out/`은 git 추적 대상이 아니므로 커밋과 무관하다.
