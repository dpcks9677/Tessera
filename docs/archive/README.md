# 보관 문서 색인

여기 있는 문서는 **끝났거나 폐기된 것**입니다. 작성 당시의 사실을 담고 있으므로 본문을 현재 코드에
맞춰 고치지 않습니다. 대신 각 문서 머리에 작성 시점과 보관 사유를 적어 두었습니다.

**이 문서들을 실행 지침으로 쓰지 마십시오.** 여기 적힌 파일 경로와 클래스명 상당수는 이미 존재하지
않습니다. 남은 작업이 있다고 판단되면 현재 코드를 새로 조사해 새 계획을 세웁니다.

---

## `plans/` — 완료·폐기된 계획서

| 문서 | 보관 사유 | 대체 문서 |
|---|---|---|
| [augment_card_design_revision_plan.md](plans/augment_card_design_revision_plan.md) | `M7-T4` 증강 카드 디자인 통합 완료 | — |
| [m7_scroll_redesign_plan.md](plans/m7_scroll_redesign_plan.md) | `M7-T4` 3D 스크롤 리디자인 완료 | — |
| [m7_asset_inventory.md](plans/m7_asset_inventory.md) | `M7-T1` 증강 에셋 인벤토리 완료 | `agent/m7_graphics_spec.md` 부록 A가 현역 기준 |
| [quill_feather_appearance_plan.md](plans/quill_feather_appearance_plan.md) | `M17-T19` **폐기(`DROPPED`, 2026-09-12).** 절차적 깃펜을 외부 FBX로 교체하며 전제가 무너짐 | `agent/work_plan.md` `M17-T22` |
| [dead_code_cleanup_proposal.md](plans/dead_code_cleanup_proposal.md) | 2026-09-12 커밋 `53fc78e`로 **실행 완료.** 단 Phase 3의 `ZodiacConstellationData.cs` 삭제는 수행되지 않음 (문서 머리 참조) | — |
| [yacht_state_ownership.md](plans/yacht_state_ownership.md) | `M2` 리팩토링 전후 상태 소유권 대조표. 역할이 ADR로 옮겨감 | `reference/architecture_decisions.md` ADR-001 |
| [unity_3d_pixel_art_poc_spec.md](plans/unity_3d_pixel_art_poc_spec.md) | 2026-08-11 PoC 명세서. PoC를 통과하고 본 개발에 들어가 역할이 끝남. 원래 리포지토리 루트에 있었음 | `reference/art/pixel_edge_filter_plan.md`, `reference/art/cel_shading_pixel_plan.md` |
| [poc_technical_notes.md](plans/poc_technical_notes.md) | 같은 PoC 시기의 영문 기술 노트. 원래 `Assets/Docs/README.md`였고, Unity 에셋 DB 안에 문서가 있으면 `.meta`가 따라붙어 밖으로 옮김 | 위와 같음. 깃펜 FBX 출처·라이선스 항목은 `reference/art_style_guide.md`로 이관 |
| [poc_engine_decision.md](plans/poc_engine_decision.md) | 엔진 채택 판단서(`Status: Conditional Go`). 조건이 이미 해소돼 Unity로 진행 중. 원래 `Assets/Docs/Decision.md` | — |

## `reports/` — 일회성 감사·리뷰

작성일 기준의 코드 상태를 기록한 것입니다. 이후 변경은 반영하지 않습니다.

| 문서 | 범위 |
|---|---|
| [code_review_20260906.md](reports/code_review_20260906.md) | `M16` 신규 코드와 매 프레임 실행 경로 한정 리뷰 |
| [maintenance_audit_20260906.md](reports/maintenance_audit_20260906.md) | 위 리뷰 바깥 영역(게임 코어·에셋 적재·프롭 생명주기) 전반 점검 |
| [autonomous_maintenance_20260906.md](reports/autonomous_maintenance_20260906.md) | 마일스톤 번호 정수화, 코드 리뷰, graphify 갱신 3건의 자율 보수 기록 |
| [code_review_20260912.md](reports/code_review_20260912.md) | 6개 렌즈(패턴·테스트 용이성·보안·로깅·비동기·리소스) 리뷰. 8건 중 5건 수정 |

## `raw/` — 툴 출력 덤프

문서가 아니라 실험 산출물입니다.

| 파일 | 내용 |
|---|---|
| [model_analysis.txt](raw/model_analysis.txt) | `normal_dice.fbx` 눈면 좌표 분석 출력. 동반 `.meta`는 과거 `Assets/` 하위에 있던 흔적 |
| [pure_yaw_test_result.txt](raw/pure_yaw_test_result.txt) | 주사위 순수 요(yaw) 정렬 접근의 실패 로그 466줄. 그 접근을 버린 근거 |

## 단독 보관

| 문서 | 보관 사유 |
|---|---|
| [augment_migration_matrix.md](augment_migration_matrix.md) | 웹 원본 55종 증강의 C# 이식 매트릭스. `M6` 이식이 끝나 근거 보존용입니다. 현재 구현 현황은 `reference/augments_specification_and_status.md`가 권위이며, 이 문서의 훅 이름 중 일부는 이식을 설계하던 시점의 가칭이라 실제 인터페이스와 다릅니다 (2장 표에 대조 열을 달아 두었습니다) |
