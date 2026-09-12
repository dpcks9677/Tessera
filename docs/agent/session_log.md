# 작업 세션 로그

> **문서 종류**: 시점 기록 (보관)
> **출처**: `docs/agent/work_plan.md` §13에서 분리
> 최신 항목이 위에 옵니다. 각 항목은 **작성 당시의 사실**이며 이후 코드 변경을 반영하지 않습니다.
> 현재 상태는 `work_plan.md` §2 진행 포인터와 §6 마일스톤 요약표가 권위입니다.
>
> 과거 항목의 마일스톤 ID는 작성 당시 번호이며 현재 번호와 다를 수 있습니다.

---


### 2026-09-12 — Claude (`M17-T22` 깃펜 외부 모델 교체, `M17-T19` 폐기)

- 작업 ID: `M17-T22` (신규, 현재 `DOING`) · `M17-T19`를 `DROPPED`로 전환
- 시작 상태: 깃펜의 닙·칼라 5개·깃대·깃판을 `InkwellAndQuill`이 전부 절차적으로 만들고 있었다. 깃털 실루엣과 깃가지 틈을 코드 상수로 맞추는 왕복이 계속됐는데, 화면이 480x270 가상 격자로 필터링되는 탓에 그렇게 맞춘 디테일이 대부분 화면에 남지 않았다. 사용자가 외부 로우폴리 모델을 제공하며 방향 전환을 지시했고, 외형에 대한 시각 확인은 사용자가 직접 하기로 정했다
- 완료 내용: 절차적 깃펜을 `Assets/Art/Reference/quill_pen_low.fbx`(메시 셋 합계 285 버텍스, 단일 머티리얼) 인스턴스 하나로 교체. `InkwellAndQuill`은 잉크통 원통 5개만 만들고, 깃펜은 직렬화된 모델·머티리얼 참조를 `Quill Pen Root` 아래에 붙인다. 텍스처는 원본 2048² 6장(11.2 MB) 대신 128² 한 장(24 KB)만 쓴다. 알파는 실루엣 전용으로 이진화하고, 깃가지 질감은 `Mixed_AO`·`Roughness`의 결을 알베도에 섞어 색으로 낸다. FBX 임포터의 `Use File Scale`은 꺼야 한다(켜면 0.01이 적용돼 깃펜이 점 하나로 줄어든다)
- 확인한 사실 1 — **닙 정렬**: Unity는 FBX를 읽으면서 메시마다 원점을 자기 바운드 중심으로 옮기고 그 이동량을 노드 Transform에 넣는다. FBX 파일의 원본 정점 좌표와 Unity 노드 위치를 섞어 계산하면 값이 어긋나며, 실제로 두 번 틀렸다(월드에서 각각 2.78, 1.41 어긋남). 올바른 기준은 각 노드의 `localPosition`과 `MeshRenderer.localBounds`이고, 가장 아래 정점은 닙 장식이 아니라 **깃대 메시**의 `(0.117, -5.817, -0.230)`이다. 깃대가 펜촉부터 깃털 끝까지 관통해 위아래 끝이 모두 그 메시에 있다. 배율 `0.3412`(= 3.97 / 11.634)와 오프셋 `(-0.040, 1.985, 0.079)`로 확정했다
- 확인한 사실 2 — **씬이 프리팹 인스턴스가 아니다**: 씬의 깃펜은 프리팹 인스턴스가 아니라 씬이 직접 소유하는 오브젝트라, 프리팹만 고치면 씬에 전달되지 않는다. 게다가 Play 모드 중 씬에 가한 변경은 Play를 끄면 사라진다. 그래서 `EnsureGeometry`가 `RealignQuillModel`로 배치와 머티리얼을 코드 상수에서 다시 강제하도록 만들어 씬 저장 여부와 무관하게 같은 자세가 나오게 했다
- 확인한 사실 3 — **픽셀 격자**: 화면의 깃펜은 `DicePixelUpscale.shader`가 1920x1080 렌더에서 480x270 격자의 셀 중심 한 점만 뽑은 결과다. 안티앨리어싱이 없어 알파 경계가 프레임마다 켜졌다 꺼졌다 한다. 텍스처 필터로는 고칠 수 없어 알파를 이진화하고(중간 알파 46.5% → 0%) 깃펜의 화면 위치를 같은 격자에 스냅했다. 같은 이유로 `CrispUiDepthMask`도 풀해상도로 깊이를 쓰면 실루엣이 한 셀만큼 어긋나므로, 알파를 읽을 자리를 같은 격자의 셀 중심으로 옮기게 했다
- 변경 파일: `Assets/Scripts/Tabletop/InkwellAndQuill.cs`(834줄 → 277줄), `Assets/Prefabs/Tabletop/3D Inkwell and Quill Decoration.prefab`(1,293줄 → 682줄), `Assets/Art/Reference/quill_pen_low.fbx`·`quill_pen_albedo.png`·`QuillPen.mat`(신규), `Assets/Docs/README.md`, `docs/quill_feather_appearance_plan.md`(폐기 표시)
- 삭제한 것: `Assets/Editor/QuillFeatherAppearanceTests.cs`(테스트 9개), `Assets/Editor/QuillFeatherAssetRefresh.cs`, 구운 깃펜 에셋 8종(메시 3, 텍스처 2, 머티리얼 3) 약 720 KB. `QuillWritingPoseTests`와 `QuillCrispUiMask`는 절차적 기하에 의존하지 않아 그대로 둠
- 실행한 검증: 컴파일 통과, 콘솔 에러 0건. EditMode 241/241 통과. 프리팹은 `PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset`으로 제자리 수정해 `.prefab.meta`가 변경 목록에 없음을 확인(GUID 보존). 닙 정렬은 씬의 실제 Transform 값으로 검산해 닙 끝이 `Quill Pen Root` 원점에서 0.0012 유닛 안에 들어옴을 확인했다. 월드 전장은 9.92로, `QuillHoverAnimator`가 전제하는 9.93과 같다
- 남은 문제/차단 요소: 화면 확인 미실시. 깃펜 자세·픽셀 안정성·족보 가림 범위·깃털 표현 네 가지를 함께 본다
- 다음 작업: 화면 확인 세 건(`M17-T22`·`M17-T18-5`·`M17-T9-1-5`)을 한 세션에서 처리

### 2026-09-09 — Claude (`M17-T9-1` 56 `dice-alchemy` 연기 가림 연출)

- 작업 ID: `M17-T9-1` (시작 상태 `TODO`, 현재 `DOING`)
- 시작 상태: 증강 56 `dice-alchemy`는 연출이 하나도 없어, 굴림 없이 값만 바뀌면 주사위 눈이 예고 없이 툭 바뀌고 점수표 후보 점수가 먼저 결과를 알렸다. 계획은 `docs/agent/m17_vfx_spec.md` §9에 이미 작성돼 있었다
- 완료 내용: 연기 파티클이 킵하지 않은 주사위를 가리는 0.25초 동안 주사위 눈 표시와 점수표 후보 점수 갱신을 보류하고, 가려진 순간 스냅 교체한 뒤 연기가 걷히도록 구현. 로직은 그대로 두고 표시만 미룸. 하위 작업 `M17-T9-1-1`(연기 스프라이트)·`M17-T9-1-2`(표시 갱신 지연 큐 `I5`)·`M17-T9-1-3`(`AugmentVfxPlanner` 확장)·`M17-T9-1-4`(연기 컴포넌트)까지 완료. `M17-T9-1-5`(Play 모드 화면 확인)는 남음
- 확인한 사실: 계획서 §9.5는 `AugmentedYachtController`가 연기 컴포넌트를 만들어 주입한다고 적었으나, 실제로는 `YachtDiceRoundPresenter`가 직접 지연 생성하도록 구현했다. `YachtTurnFlowPresenter.BindProps`가 이미 인자 11개이고 연기는 주사위 비주얼 소관이라 컨트롤러를 경유할 이유가 없었다. `AugmentedYachtController.cs`는 이번 작업으로 변경되지 않았다
- 변경 파일: `Assets/Scripts/Dice/BakedDiceController.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/DiceSmokePuffVfx.cs`(신규), `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtDiceRoundPresenter.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentVfxPlanner.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtTurnFlowPresenter.cs`, `Assets/Editor/DiceSmokeSpriteBaker.cs`(신규), `Assets/Resources/Vfx/DiceSmokePuff.png`(신규 에셋), `Assets/Editor/AugmentVfxPlannerTests.cs`
- 실행한 검증: Unity 컴파일 통과, 신규 경고 없음. `AugmentVfxPlannerTests` 격리 실행 13/13 통과(기존 10 + 신규 3). 전체 EditMode 887개 중 Tessera 실패 3건(`FontFallbackTests` 2건, `YachtGameRulesTests.LuckySevens_MidGameAcquisitionResetsOnlyOwnerAcesAndGrantsExtraTurn` 1건) — 셋 다 이번 변경과 무관한 선행 실패로 손대지 않음. `DiceSmokePuff.png` 픽셀 검사에서 유니크 알파 `{0,64,128,192,255}` 5종만 확인, 안티에일리어싱 중간값 0건, 알파>0 픽셀 RGB 전부 (255,255,255)
- 남은 문제/차단 요소: `M17-T9-1-5` Play 모드 화면 확인 미실시. `M17-T18-5`·`M17-T19-6` 화면 확인도 아직 열려 있어 세 화면 확인이 동시에 대기 중. §2 「한 번에 하나만 `DOING`」 규칙과 달리 `M17-T18`·`M17-T19`·`M17-T9-1` 셋 다 `DOING`으로 남아 있다. 임의로 상태를 바꾸지 않고 그대로 둠
- 다음 작업: `M17-T9-1-5` Play 모드 화면 확인. 이후 `M17-T19-6`·`M17-T18-5`와 합쳐 진행

### 2026-09-09 — Claude (`M17-T19` 깃펜 깃털 흰색 전환과 갈라짐 연출)

- 작업 ID: `M17-T19` (시작 상태 `DOING`)
- 시작 상태: `M17-T18` 호버 필기 연출로 깃펜이 화면 중앙에 자주 올라오면서, 깃털이 웜 브라운 3단 그라데이션이라 흰 깃펜으로 안 읽히고 얕은 실루엣 노치뿐이라 깃가지가 갈라진 느낌이 없다는 두 문제가 드러남
- 사용자 결정: 흰색 바탕은 오프화이트 아이보리 + 웜 그레이 결로, 갈라짐은 알베도 알파 클립 슬릿 + 메시 노치 강화를 병행하는 것으로 확정
- 한 일:
  - 픽셀 예산 재계산: 480×270 필터에서 깃면이 화면 7~15 px뿐임을 확인하고, 이를 근거로 `BarbSlant = 1/6`·`SlitCycles = 12`(각도 33.8도, 최악 구멍 1.5~2.2 px)를 채택
  - 공유 상수 격자(`BarbCount`·`SlitCycles`·`BarbSlant`·`SlitDuty` 등)와 아이보리 4색 팔레트로 교체
  - 알베도 알파 슬릿 함수와 메시 노치(`MaxNotchDepth 0.40`, `NotchSharpness 1.8`) 강화 구현
  - 머티리얼에 알파 클립(`_Cutoff`·`_ALPHATEST_ON`·`_AlphaClip`·큐 2450) 적용
  - 베이커에 밉 커버리지 보존 2줄과 `isNormalMap` 가드 추가, 헬퍼 `internal` 승격, 전용 갱신 도구 `QuillFeatherAssetRefresh` 신설(전체 재베이크 대신 4개 경로만 같은 GUID로 덮어써 프리팹 GUID 재발급을 피함)
  - `QuillCrispUiMask` 깊이 마스크와 알파 슬릿의 불일치를 수용하고 탈출구를 사전 명세
  - 상세 계획서 [`docs/archive/plans/quill_feather_appearance_plan.md`](../archive/plans/quill_feather_appearance_plan.md) 신규 작성
- 실행한 검증: `M17-T19-1`~`M17-T19-5` 범위의 EditMode 테스트 9개(`QuillFeatherAppearanceTests`)와 컴파일 확인. `M17-T19-6` 에셋 갱신·화면 확인은 미실시
- 결정 기록: `D-042`
- 남은 문제/차단 요소: `M17-T19-6`(에셋 4종 갱신과 화면 확인) 미실시. `M17-T18-5`(호버 필기 화면 확인)도 아직 열려 있어 두 화면 확인을 한 세션에 합쳐 진행하기로 함
- 다음 작업: `M17-T19-6`을 `M17-T18-5`와 합쳐 진행. 이후 `M17-T9` 증강 발동 VFX 복귀

### 2026-09-09 — Claude Opus 5 (`SOLID-T01` 증강 획득 초기화 자율화)

- 작업 ID: `SOLID-T01` (`DONE`)
- 변경: `YachtAugmentRuntime.ApplyAugment`의 증강 ID별 if-else 사다리(11개 분기, 63줄) 삭제. `FastStraight`에 `IOnAugmentSelected` 구현 추가(`FastStraightState`와 레거시 `context.Player` 필드를 함께 초기화하는 기존 이중 기록 패턴을 따름). 나머지 10개 분기는 대응 핸들러 `OnSelected`가 이미 같은 초기화를 수행하고 있어 삭제만으로 충분했음. `Modification` 분기·`ResetFilledTarget` 호출·`IOnAugmentSelected` 디스패처·`replacement` 이벤트 발행 블록은 유지
- 착수 중 계획서(`docs/agent/solid_refactoring_work_plan.md`) 기술과 실제 코드가 어긋난 것을 확인해 해당 절을 실제 범위로 교정함(`D-041`)
- 검증: EditMode 전체 884건 중 874 통과, 6 실패(전부 무관, 기존 실패), 4 스킵. 계획서가 지목한 `YachtQuestAugmentTests`, `YachtEnhanceAugmentTests`, `YachtManualActionAugmentTests`는 전부 통과. 신규 컴파일 경고 없음
- 다음 작업: `SOLID-T02` (`Describe` switch 제거)

### 2026-09-09 — Claude (`M17-T18` 깃펜 호버 필기 연출 계획 수립)

- 작업 ID: `M17-T18` (신규, `TODO`)
- 시작 상태: 깃펜 재모델링(`7cebe26`)으로 잉크통·깃펜 소품은 끝났으나 정지 장식이었다. 점수 칸을 고를 때의 피드백은 `Button`의 색 하이라이트뿐이었다
- 사용자 요청: 점수 칸에 호버하면 깃펜이 잉크통에서 나와 그 칸으로 따라붙고, 호버가 풀리면 잉크통으로 돌아가는 필기 연출
- 완료 내용: 계획서 [`docs/agent/m17_quill_hover_animation.md`](m17_quill_hover_animation.md) 작성. 구현은 아직 없다
- 확인한 사실:
  - `Quill Pen Root`의 원점이 곧 닙 끝이라 목표 좌표를 그대로 넣으면 닙이 그 지점에 닿는다. 오프셋 계산이 필요 없다
  - 점수 칸의 기입 가능 조건은 이미 `button.interactable`에 정리돼 있어 깃펜 반응 조건으로 그대로 쓸 수 있다
  - 점수표 오버레이는 `CrispUI` 전용 카메라가 월드 화면 위에 합성하므로 깊이 비교가 없다. 깃펜은 언제나 오버레이 뒤에 그려진다. 칸 인셋 알파는 22 %라 비쳐 보이지만 호버 하이라이트 알파가 70 %라 가리키는 칸에서 닙이 묻힌다
  - 깃털 날이 3.22 단위인데 시트 세로가 8.80 단위라, 자세를 잘못 잡으면 깃털 하나가 여러 행을 덮는다. 깃털 방향(yaw)을 항상 `칸 → 잉크통`으로 두면 시트 바깥으로 빠진다
- 변경 파일: `docs/quill_hover_writing_animation_plan.md`(신규), `docs/augmented_yacht_work_plan.md`
- 실행한 검증: 없음(문서 작업)
- 다음 작업: `M17-T18-1` 점수표 호버 이벤트 노출. 또는 진행 중인 `M17-T9` 복귀

### 2026-09-08 — Claude (점수표 6열 재배치와 턴별 Categories 접힘·펴짐)

- 작업 ID: `M17-T9` (진행 중), 결정 `D-040`

- 사용자 결정: 표를 `[P1 아이콘][P1 Categories][P1 점수][P2 점수][P2 Categories][P2 아이콘]` 여섯 열로 나누고, 현재 턴인 쪽 이름 열만 펴고 반대쪽은 0으로 접는다. 접힌 쪽에는 축소된 천 패치가 아이콘 섹터에 남아 상대의 변형 증강이 보인다. 전환은 0.35초 ease-out cubic (`D-040`)
- 완료 내용:
  - `ParchmentScoreSheet`가 열 컨테이너 6개를 만들고 행 요소를 그 안에 `0..1`로 앵커. 전환은 컨테이너 여섯 개의 가로 앵커만 바꾼다
  - `ResolveColumnBounds(expandT, bounds)` 순수 함수로 경계 7개 계산. 화면 없이 검증 가능한 유일한 지점
  - 이름 열에 `RectMask2D` + `CanvasGroup`. 폭이 줄기 전에 알파로 먼저 사라져 글자가 점수 열로 삐져나오지 않는다
  - 스티커 슬롯·족보 아이콘·족보 이름을 플레이어별 두 벌로 분리. 공개 API 다섯 개에 `playerIndex` 추가
  - 천 패치가 `[아이콘 섹터 + Categories]`를 덮도록 넓히고, 전환 매 프레임 가로 위치·크기를 다시 잡음
  - `YachtTurnFlowPresenter.SyncAugmentStickers`가 두 사람 몫을 각각 동기화. `shownStickers`도 플레이어별
  - `Assets/Editor/ScoreSheetColumnLayoutTests.cs` 신규 (열 경계 14건)
- 확인한 사실:
  - 낙인(`S2`) 연출은 `AugmentVfxRequest.PlayerIndex`를 써야 한다. 이 시점에는 턴이 이미 넘어갔을 수 있어 `CurrentPlayerIndex`로는 엉뚱한 쪽에 찍힌다
  - 부착·낙인 코루틴이 시작 시점의 제자리를 붙잡고 있었다. 열이 접히고 펴지면 그 값이 낡으므로 매 프레임 다시 읽도록 바꿨다
  - 보너스 진행도가 P1 것만 그려지고 있었다(`UpdatePlayerScoreUI(players[1], p2ScoreLabels, null)`). 이름 열이 둘로 갈라지면서 양쪽 각자 값이 나온다
  - 머리글을 열 밖에 두고 그룹 세 열을 덮게 했더니 `P1`·`P2`가 점수 열을 벗어나 이름 열 위로 밀려났다(사용자 지적). 머리글도 다른 행과 같이 자기 열 안에 넣어야 접힘을 그대로 따라온다. `CATEGORIES` 머리글도 각자 이름 열에 되살렸다
  - 스티커 실루엣을 우표 톱니에서 각진 사각형 러너로 바꾸고, 오른쪽·아래 1px 불투명 그림자를 넣었다. 재질이 알파 컷아웃이라 반투명 그림자는 쓸 수 없다
  - 금테가 행마다 다른 자리에 나타난 원인은 굽는 해상도였다. 캔버스 해상도(100/단위)로 구운 텍스처가 픽셀 필터 화면 발자국보다 4배 촘촘해서, 점 샘플링이 행마다 다른 텍셀을 골랐다. `StickerPixelsPerUnit = CanvasUnitsPerWorldUnit / 4`로 굽고 크기를 칸이 아니라 천 전체로 재도록 바꿔 한 텍셀 = 화면 한 픽셀로 맞췄다(펴진 천 59×15 텍셀)
  - 스티커 글자를 증강 이름(한글) 대신 영문 축약 표기로 바꿨다. 원본은 `augmented-dice` 프로젝트 `src/augments.json`의 `mark` 태그이고, 활성 변형 증강 18종 전부에 값이 있었다. `AugmentStickerCatalog.Marks`로 옮기고 `MarkLabel(augmentId, fallback)`으로 읽는다
- 변경 파일: `Assets/Scripts/Games/AugmentedYacht/Presentation/ParchmentScoreSheet.cs`, `.../YachtTurnFlowPresenter.cs`, `.../AugmentStickerCatalog.cs`, `Assets/Editor/ScoreSheetColumnLayoutTests.cs`, `Assets/Editor/AugmentStickerCatalogTests.cs`, `docs/augmented_yacht_m17_vfx_spec.md`, `docs/augmented_yacht_work_plan.md`
- 실행한 검증:
  - C# 컴파일 오류 0
  - EditMode 875건 실행. `ScoreSheetColumnLayoutTests` 14건 전부 통과. 실패 7건은 모두 변경과 무관한 기존 실패(`FontFallbackTests` 2건, `YachtGameRulesTests.LuckySevens_중간획득시...` 1건, UnitySkills 패키지 자체 테스트 4건)
  - `Tessera/Rebuild Score Sheet` 메뉴 실행 후 플레이 모드 진입, 게임 뷰 캡처로 6열 배치·좌우 대칭·접힘 상태 육안 확인 (P1 펴짐 / P2 펴짐 두 경우)
- 남은 문제/차단 요소: 결정 표에 `D-039`가 두 번 있다(드래프트 순서 / 테이블 나뭇결). 이번 작업 이전부터 있던 문제이고 손대지 않았다
- 다음 작업: `M17-T9` 나머지 34개 연출 설명 기입

### 2026-09-08 — Claude (`M17-T17` 드래프트 제시 구성과 선공 순서)

- 작업 ID: `M17-T17` (신규, `DONE`)
- 시작 상태: `M17-T9` 증강 발동 VFX 사양을 기입하던 중 사용자가 드래프트 규칙 두 건을 지시. 제시 구성은 `CreateDraftOptions`가 후보를 섞어 앞에서 3개를 자르기만 했고, 선공은 `FindNextDraftPlayer`가 항상 인덱스 0부터 훑어 `P1`이 언제나 먼저 골랐다
- 사용자 결정: 같은 대상 족보를 교체하는 변형 증강이 한 제시에 겹치지 않게 하고, 1번 증강은 무작위·2·3번 증강은 총점이 낮은 쪽이 먼저 고르게 한다. 이 변경은 신규 태스크로 분리한다 (`D-039`)
- 완료 내용:
  - `YachtDraftState`에 `FirstPlayerIndex` 추가. 드래프트 라운드가 시작될 때(`Draft.IsActive`가 꺼져 있을 때) 한 번만 정하고 라운드 내내 고정
  - `DetermineFirstDraftPlayer` 추가. 라운드 1은 무작위, 이후는 `PlayerScoreData.totalScore`가 낮은 쪽. 동점이면 무작위
  - `FindNextDraftPlayer`가 0부터가 아니라 선공부터 순환하도록 변경
  - `CreateDraftOptions`가 이미 뽑힌 변형 증강의 `Target`과 겹치는 후보를 건너뛰도록 변경
  - `Assets/Editor/YachtDraftOrderTests.cs` 신규. 제시 중복·선공 결정·차례 이동을 EditMode 테스트 5개로 고정
- 확인한 사실:
  - 보유자 기준 중복 차단은 이미 있었다. `CanAcquire`가 보유한 변형 증강의 `Target`과 같은 후보를 거른다. 빠져 있던 것은 **한 번의 제시 안에서의** 중복이었다
  - 변형 증강에는 쓸 만한 트리거 점이 없다. 현재 `AugmentTriggered`를 발행하지 않지만(`ModificationAugment.ModifyScores`에 `Emit`이 없고 `AugmentScoreContext`가 이벤트 수집기를 `null`로 받음) 이는 고치면 되는 문제이고, 진짜 이유는 `ModifyScores`가 굴림 직후와 킵 토글마다 호출되는 `UpdateCandidates`(`LocalGameAuthority.cs:170`·`237`)에서 매번 돌면서 조건 불성립 시에도 `0`을 쓴다는 점이다. 이벤트를 내도 "실행됐다"와 "뭔가 일어났다"가 구분되지 않는다. 퀘스트 계열이 `AfterScoreCommit`에서 한 턴에 한 번 발행하는 것과 대비된다. `M17-T9` 변형 계열을 상태 기반으로 설계한 근거이며 사양 문서 §3.1.1에 기록했다
- 변경 파일: `Assets/Scripts/Games/AugmentedYacht/Logic/YachtAugmentRuntime.cs`, `Assets/Editor/YachtDraftOrderTests.cs`, `docs/augmented_yacht_work_plan.md`, `docs/augmented_yacht_m17_vfx_spec.md`
- 실행한 검증:
  - C# 컴파일 오류 0 (`Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`)
  - 빌드된 어셈블리를 직접 실행해 11개 항목 전부 통과. 제시 200회에서 대상 족보 중복 0회, 변형 증강이 2개 이상 함께 제시된 표본 94회로 규칙이 실제로 작동할 여지 확인
  - 변경 전 어셈블리(`Library/ScriptAssemblies`)로 같은 검사를 돌려 5개 실패 확인. 제시 200회 중 12회 중복(FullHouse 5, LargeStraight 3, SmallStraight 2, Yacht·Aces 각 1)
- 남은 문제/차단 요소: 없음
- 다음 작업: `M17-T9` 변형 계열 구현

### 2026-09-08 — Antigravity (한글 폰트 Mulmaru 적용 및 폴백 구성)

- 시작 상태: 한글 텍스트(증강 카드, HUD 등) 렌더링 시 영문 폰트 Alagard의 한글 글리프 부재로 시스템 고딕 폰트로 자동 폴백되어 픽셀 아트 스타일 불일치 발생
- 완료 내용:
  - 다운로드 폴더의 물마루(Mulmaru) 픽셀 폰트 에셋(`Assets/Fonts/Mulmaru.ttf`) 및 라이선스 고지(`Mulmaru-LICENSE.txt`) 추가
  - 영문 폰트 `alagard.ttf.meta` 및 `m6x11.ttf.meta`의 `fallbackFontReferences`에 Mulmaru 등록
  - 에디터 자동 폴백 보장 도구(`Assets/Editor/FontFallbackSetup.cs`) 및 단위 테스트(`FontFallbackTests.cs`) 작성
  - 증강 카드(`AugmentCardView`) 및 게임 HUD(`YachtHudFactory`)의 폰트 로드 1순위를 `Mulmaru.ttf`로 지정하여 한글 UI의 직접 픽셀 렌더링 보장
  - 카드 텍스트 생성 시 `FilterMode.Point` 설정 추가로 픽셀 글리프 번짐 방지
- 변경 파일: `Assets/Fonts/Mulmaru.ttf`, `Assets/Fonts/Mulmaru-LICENSE.txt`, `Assets/Fonts/alagard.ttf.meta`, `Assets/Fonts/m6x11.ttf.meta`, `Assets/Editor/FontFallbackSetup.cs`, `Assets/Editor/FontFallbackTests.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentCardView.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtHudFactory.cs`, `docs/augmented_yacht_work_plan.md`
- 실행한 검증: C# 컴파일 오류 0 (`Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` 빌드 성공)

### 2026-09-07 — Claude (`M17-T8` 후속: 8면 면 숫자 폰트 전환과 렌더링 결함 세 건 수정)

- 작업 ID: `M17-T8` (후속 수정)
- 시작 상태: 사용자가 실제 화면에서 세 가지 문제를 지적. 주사위 몸체에 픽셀 필터가 걸리지 않음, 면 숫자가 작고 타이머 폰트를 쓰기로 한 결정이 미구현, 몸체 색이 의도한 딥블루보다 밝음
- 완료 내용:
  - 몸체 재질의 자발광 제거. 바탕색과 같은 색을 자발광으로 더해 조명 계조가 덮이고 어두운 팔레트가 화면에서 밝게 뜨던 문제 해소
  - 면 숫자를 직접 만든 7세그먼트 메시에서 `Assets/Fonts/alagard.ttf` 기반 `TextMesh`로 교체. 타이머·점수표와 같은 서체. 글리프 높이 0.22 → 0.43
  - 깊이를 검사하는 글리프 셰이더 `DicePoC/CrispUiText` 추가. 뒷면 글자가 몸체를 뚫고 보이던 문제 해소
  - 깊이 전용 판이 몸체 재질로 덮여 Crisp 카메라가 몸체를 원본 해상도로 다시 그리던 결함 수정
- 수정한 버그:
  - 내장 폰트 재질(`GUI/Text Shader`)은 `ZTest Always`에 `Cull Off`라 깊이 버퍼를 무시한다. `CrispUiDepthMask`가 깊이를 남겨도 글자가 검사를 받지 않아 8면 전부의 숫자가 겹쳐 보였다. 이전 메시 숫자는 Unlit이라 우연히 가려지고 있었다
  - `ApplyDiceMaterialsToFbx`가 자식 렌더러를 전부 훑으면서 `Crisp Depth Mask`까지 몸체 재질로 덮었다. 색을 쓰지 않아야 할 판이 조명 받는 몸체 사본이 되어 `CrispUI` 레이어에서 풀 해상도로 렌더됐고, 그래서 주사위에만 픽셀 필터가 걸리지 않은 것처럼 보였다. 깊이 마스크를 도입한 시점부터 있던 결함이다
  - `TextMesh`는 글자가 바로 읽히는 쪽이 `-Z`다. 면 법선을 `+Z`에 맞췄더니 좌우가 뒤집혀 `LookRotation(-n, localUp)`으로 고쳤다. 같은 이유로 `Cull Back`에서는 글자가 통째로 사라져 컬링 대신 깊이 검사로 뒷면을 가린다
- 변경 파일: `Assets/Editor/DiceShapeBaker.cs`, `Assets/Editor/DiceShapeAssetTests.cs`, `Assets/Rendering/Shaders/CrispUiText.shader`, `Assets/Scripts/Dice/DicePaletteCatalog.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/DiceVisualPool.cs`, `Assets/Prefabs/Dice/Die_Octahedron.prefab`, `docs/augmented_yacht_work_plan.md`
- 실행한 검증: 프로젝트 EditMode 테스트 121/121 통과. Play 모드 실렌더로 확인 — 8면체 실루엣에 배경과 같은 크기의 픽셀 계단이 생기고, 면 숫자는 원본 해상도로 또렷하며, 보이는 면에만 숫자가 하나씩 나온다. 실제 `8면 주사위` 증강으로 굴린 주사위가 값 면을 정면으로 세우고 숫자를 똑바로 표시하는 것도 확인
- 진단 방법 기록: 카메라 컬링 마스크를 하나씩 꺼서 어느 카메라가 몸체를 그리는지 좁혔다. 월드 카메라에서 `Dice` 레이어를 빼도 몸체가 남고 Crisp 카메라까지 꺼야 사라지는 것으로 원인을 특정했다
- 새 결정/가정: 8면체 면 숫자는 UI와 같은 폰트로 찍는다. 동적 폰트 아틀라스는 다시 구워질 수 있으므로 `Font.textureRebuilt`를 받아 재질의 텍스처를 다시 물린다. 같은 폰트로 증강 카드 한글도 찍는 씬이라 실제로 일어난다
- 남은 문제/차단 요소: 없음
- 다음 작업: 주사위 눈 렌더링 방식 논의

### 2026-09-07 — Claude (`M17-T8` 특수 주사위 외형 검수 완료)

- 작업 ID: `M17-T8` (`DONE`)
- 시작 상태: 2026-09-05 중단 기록상 "8면 도중"이었으나, 실제로는 `M12` 진행 중 매핑·프리셋·테스트가 채워져 있어 신규 구현이 아닌 검수 성격이었음
- 완료 내용:
  - Unity Editor Play 모드에서 8종 주사위 외형을 실렌더로 검수. 논리 종류→팔레트 적용을 런타임 머티리얼 `_BaseColor`로 8종 전부 확인
  - 실제 증강(묵직한 주사위) 보유 상태에서 화면 주사위 5개 중 1개만 빨강으로 표시되는 것을 확인
  - 주사위 몸체 누적 버그 수정 (`DiceVisualPool.BuildVisual`)
  - 8면체 정규화 크기를 1.0 → 1.2로 확대 (`DiceVisualPool.NormalizedSizeFor`)
  - 8면체 면 숫자 가독성 실측 결과를 `DiceShapeBaker` 주석에 기록
  - 일반 모드에서 드래프트 오버레이·보유 증강 카드·특수 주사위가 모두 비활성인 것을 확인
- 수정한 버그: 재생 중 `Destroy`가 프레임 끝에 처리되는데 `Find("Visual")`이 파괴 예약된 몸체를 다시 잡아, 같은 프레임에 종류가 두 번 바뀌면 몸체가 겹쳐 쌓였다. 실측에서 주사위당 `Visual` 자식이 5~7개까지 누적됐다. 이름이 `Visual`인 자식을 전부 버리고, `Destroy` 전에 이름 변경과 비활성화를 먼저 해 같은 프레임의 다음 조회가 다시 잡지 못하게 했다
- 변경 파일: `Assets/Scripts/Games/AugmentedYacht/Presentation/DiceVisualPool.cs`, `Assets/Scripts/Dice/DiceFaceOrientation.cs`, `Assets/Editor/DiceShapeBaker.cs`, `docs/augmented_yacht_work_plan.md`
- 실행한 검증: EditMode 전체 807/812 통과(실패 1건은 `UnitySkills` 패키지 자체 문서 용량 테스트). 재베이킹 후 프로젝트 테스트 121/121 통과. 종류 9회 연속 교체 후 주사위당 `Visual` 1개 유지 확인
- 8면체 면 숫자 해결: 픽셀 패스에서는 어떤 크기로도 읽을 수 없음을 실측으로 확인한 뒤(면 내접원 약 7.6px, 획 0.5px는 얼룩·2px는 흰 덩어리·점 표기는 값 6이 5×8px로 초과), 크기를 키우는 대신 숫자만 픽셀 필터 밖으로 뺐다. 런타임이 8면체의 `Pip_*` 를 `CrispUI` 레이어로 올려 점수표·증강 카드 글자와 같은 경로로 원본 해상도에 합성한다. Crisp 카메라는 월드 물체를 찍지 않아 깊이 버퍼가 비어 있으므로 몸체 사본을 깊이 전용으로 같은 레이어에 두어 뒤쪽 면 숫자가 뚫고 나오지 않게 했다
- `GetOctaCameraFacingRotation`이 면 법선만 맞추고 롤을 정하지 않아 면 숫자가 누워 보이던 것도 고쳤다. 6면과 같이 면 법선과 글자 윗방향을 함께 맞춘다
- 새 결정/가정: 8면체 값 표시는 면에 새긴 숫자를 `CrispUI`로 올리는 방식으로 확정한다. 몸체는 픽셀 패스에 남아 질감이 유지된다
- 남은 문제/차단 요소: 일반 모드에서 빈 증강 카드 트레이(석재 프롭)가 그대로 보인다. 카드 인스턴스는 모두 비활성이라 리소스 누수는 아니며, 프롭을 감출지는 미결
- 다음 작업: 사용자 지시 대기

### 2026-09-07 — Claude (M17 착수 준비, `M17-T8` 시작)

- 작업 ID: `M17-T8` (`DOING` 지정)
- 시작 상태: `feature/yacht-dice-migration` → `main` 병합이 이미 완료(`fbe93df`, `origin/main` 동기)됐으나 §2 포인터가 "병합 준비"로 남아 있었음
- 사용자 결정: `M17` 첫 작업은 `M17-T8` 특수 주사위 검수. 새 브랜치 `feature/m17-augmented-hotseat`에서 진행. graphify 라벨 갱신분은 선행 `chore` 커밋으로 정리
- 완료 내용:
  - `main` 기준 새 브랜치 `feature/m17-augmented-hotseat` 생성
  - graphify 커뮤니티 이름 갱신분 커밋(`44fef09`)
  - §2 포인터를 `M17` / `M17-T8` `DOING` 기준으로 갱신, §M17 표의 `M17-T8` 상태 변경
  - `M17` 15개 작업의 코드 기준 현행 격차 조사
- 조사 결과 (문서 기록과 코드의 차이):
  - `M17-T8`은 2026-09-05 중단 기록보다 진척됨. `YachtDieType` 8종 전부 `YachtDieVisuals.Resolve`로 팔레트에 매핑되고 8면 형상·프리셋·회전 테스트가 이미 존재. 남은 범위는 게임 내 최종 검수와 프로모션 레벨 등 잔여 표현
  - `M17-T2`(드래프트 오버레이)·`M17-T4`(수동 발동 버튼)·`M17-T5`(턴 시간 증강 배선)는 상당 부분 구현되어 있음
  - `M17-T1`(교대 가림)·`M17-T9`(발동 VFX)·`M17-T11`·`M17-T12`는 미착수, `M17-T6`은 점수만 표시, `M17-T7` 무작위 완주 테스트는 없음
- 변경 파일: `docs/augmented_yacht_work_plan.md`, `graphify-out/.graphify_labels.json(.sig)`
- 실행한 검증: `git log`로 병합·푸시 상태 확인. 코드·씬 변경 없음
- 새 결정/가정: 없음
- 남은 문제/차단 요소: 없음
- 다음 작업: `M17-T8` 게임 내 특수 주사위 외형 검수

### 2026-09-07 — Claude (M7-T5~T12를 M17로 이동)

- 작업 ID: 없음(문서 관리, `D-038`)
- 시작 상태: `D-036`으로 M7을 차단 최소분/폴리시분 태그로 나눴으나(미커밋), 사용자가 실제 태스크 이동을 요청
- 사용자 결정: `M7-T5`~`M7-T12`를 `M17`로 옮기고 `M7`은 `T1`~`T4`로 완료 처리
- 완료 내용:
  - `D-038` 추가, `D-036` 태그 분리 대체
  - §6: `M7` `DONE`, `M17` 선행 `M11`만, 결과물 갱신
  - §7 M7 절: 태그 분리 문단을 이동 문단으로 교체
  - §M17 절: `M17-T8`~`M17-T15`(구 `M7-T5`~`T12`) 추가, 완료 조건 보강. `M17-T13`~`T15` `DEFERRED`
  - §2 전체 상태 줄, §3.3 연기 테이블, §M16 매듭 절 참조 갱신
  - `m7_graphics_plan.md`: 표 상태·소속 갱신, §5 로그 추가
- 변경 파일: `docs/augmented_yacht_work_plan.md`, `docs/augmented_yacht_m7_graphics_plan.md`
- 실행한 검증: 두 문서 M7/M17 표기 일치, `M17` 선행 정합 확인. 코드·씬 변경 없음
- 새 결정/가정: `D-038`
- 남은 문제/차단 요소: 없음
- 다음 작업: `feature/yacht-dice-migration` → `main` 병합

### 2026-09-07 — Claude (M16 매듭 — 셀 셰이딩 채택 보류)

- 작업 ID: `M16` (매듭)
- 시작 상태: `M16`이 `DOING`으로 채택 판단 대기. `M16-T7`만 `TODO`, 나머지 완료. 코드는 커밋됨(26953db·9de4418·22fd80b·6dd46a0). `main` 병합 전 마일스톤을 확정해야 했음
- 사용자 결정: 채택 보류 + 인프라 유지. `m_RenderingMode`는 Forward 유지 + 문서화
- 완료 내용:
  - `D-037` 추가: 셀 셰이딩 채택 보류, 인프라·기본값 Baseline 유지, `M16-T7` `DEFERRED`, Forward 유지
  - §6 표 `M16` 행 `DOING` → `DEFERRED`, 결과물 갱신
  - §7 M16 절: `M16-T7` `DEFERRED`, "M16 매듭 (2026-09-07)" 기록 추가
  - §2 진행 포인터: 현재 마일스톤 없음(`main` 병합 준비), 전체 상태 줄에 M16 보류 반영
  - §3.3 연기 테이블에 M16 셀 셰이딩 행 추가
  - §15 최우선 작업을 `M16`에서 `main` 병합으로 교체
  - `cel_shading_pixel_plan.md` §12 매듭 절 추가, §6.4 표 주석 갱신
- 변경 파일: `docs/augmented_yacht_work_plan.md`, `docs/cel_shading_pixel_plan.md`
- 실행한 검증: 두 문서의 M16 상태 표기 일치 확인. 코드·씬·에셋 변경 없음(인프라 유지, `m_RenderingMode` 현행 유지)
- 새 결정/가정: `D-037`
- 남은 문제/차단 요소: 없음. Cel 육안 비교는 그래픽 패스로 이월
- 다음 작업: `feature/yacht-dice-migration` → `main` `--no-ff` 병합

### 2026-09-07 — Claude (M7 그래픽 후순위화 문서 정리)

- 작업 ID: 없음(문서 관리, `D-036`)
- 시작 상태: `M16` 매듭 작업 진행 중. 그래픽 폴리시를 `M17` 뒤로 미루고 싶으나 M7이 번호상 `M17` 선행에 물려 있었음. `work_plan` §2는 "M7-T1~T5 완료"였으나 `m7_graphics_plan` 표·로그상 `M7-T5`는 8면 주사위 도중 중단 상태로 두 문서가 어긋나 있었음
- 완료 내용:
  - `D-036` 추가: M7 잔여를 차단 최소분(T5·T7~T9 기능 레벨)과 폴리시분(T6·T11·T12)으로 분리, 폴리시분 `DEFERRED`, `M17` 선행을 차단 최소분으로 한정, 재번호 없음
  - §6 표: M7 행 결과물 갱신, `M17` 선행 `M7`, `M11` → `M7` 차단 최소분(T5·T7·T8·T9), `M11`
  - §2 전체 상태 줄: "M7-T1~T4 완료, M7-T5 중단", "M7 폴리시분 `DEFERRED`" 반영
  - §3.3 연기 테이블에 M7 폴리시분 행 추가
  - §7 M7 절: 차단 최소분/폴리시분 분류 문단 추가, `M7-T5` 정의 변경 이력(아이콘 이식 → 특수 주사위 외형) 명시
  - §15 최우선 작업 줄을 stale한 `M7-T1`에서 현재 `M16` 상태로 교체
  - `m7_graphics_plan.md`: 표 상태 갱신(T6·T11·T12 → `DEFERRED`), 분류 문단·완료 조건·§3 규칙 7 수정, stale한 §2 협업 포인터를 `work_plan` 참조로 축약
- 변경 파일: `docs/augmented_yacht_work_plan.md`, `docs/augmented_yacht_m7_graphics_plan.md`
- 실행한 검증: 두 문서의 M7 상태 표기 일치 확인, `M17` 선행 참조 정합 확인, 코드·씬·에셋 변경 없음
- 새 결정/가정: `D-036`
- 남은 문제/차단 요소: 없음
- 다음 작업: `M16` 채택 판단 재개

### 2026-09-06 — Claude (코스믹 큐브 머티리얼 null 오버라이드 수정)

- 작업 ID: 없음(마일스톤 밖 버그 수정)
- 시작 상태: 씬의 코스믹 큐브 프리팹 인스턴스에 렌더러 8개의 머티리얼과 테서랙트 메시를 null로 덮는 오버라이드 9개가 저장되어 있었음. 프롭이 통째로 기본 머티리얼로 보임. `HEAD`에는 없고 작업 트리에만 있던 변경
- 원인: `RollCosmicCube`가 `[ExecuteAlways]`라 에디트 모드에서도 연출을 만들고, 그때 `RuntimeAssetGuard`가 `HideFlags.DontSave` 사본을 `sharedMaterial`·`sharedMesh`에 끼운다. 사본은 씬에 직렬화되지 않으므로 그 상태로 씬을 저장하면 참조가 null로 기록되고, 프리팹 인스턴스에서는 그 null이 오버라이드로 굳는다
- 완료 내용:
  - 씬에서 null 오버라이드 9개를 제거해 프리팹의 구운 머티리얼·메시 참조를 되살림
  - `RuntimeAssetGuard`가 사본과 원본 에셋의 짝을 기억하도록 하고, 씬 저장 직전에 구운 에셋으로 되돌린 뒤 저장이 끝나면 같은 사본을 다시 끼우는 경로를 추가
  - 그 시점을 잡는 `RuntimeAssetGuardSceneHook`(`EditorSceneManager.sceneSaving`/`sceneSaved`)을 추가. 코스믹 큐브뿐 아니라 이 헬퍼를 쓰는 프롭 전부에 적용됨
- 변경 파일: `Scripts/Core/RuntimeAssetGuard.cs`, `Editor/RuntimeAssetGuardSceneHook.cs`(신규), `Editor/RuntimeAssetGuardTests.cs`(신규), `Scenes/Augmented Dice.unity`
- 실행한 검증: 씬을 다시 열고 저장해도 null 오버라이드가 생기지 않음(파일 해시 불변), 플레이 모드에서 크리스탈 셸·내부 성운·엣지 아웃라인·테서랙트 정상 렌더 확인, EditMode 795개 중 프로젝트 테스트 전부 통과
- 새 결정/가정: 없음
- 남은 문제/차단 요소: 없음
- 다음 작업: `M10.8-T0` 기준선 캡처와 토글 골격

### 2026-09-06 — Claude (M10.9 Unity 물리 기반 프리셋 베이커)

- 작업 ID: `M10.9-T1`~`M10.9-T6`
- 시작 상태: `M10.7` 트레이 확대가 작업 트리에 있었고, 프리셋은 여전히 웹 스튜디오에서 구운 것이라 착지 좌표가 트레이 좌표계와 다른 경로로 스케일되고 있었음
- 완료 내용:
  - `EditorSceneManager.NewPreviewScene()`과 `PhysicsScene.Simulate`로 플레이 모드 없이 물리를 스텝할 수 있음을 확인하고 그 경로를 채택
  - 트레이 내부 림 경계에 박스 물리벽 5장을 세우는 리그와 D6 박스·D8 볼록 메시 바디를 작성
  - 시뮬레이션 좌표계를 주사위 루트 로컬 프레임으로 두고, 저장 시 `BakedDiceController.TransformPresetDie`의 역함수로 소스 단위로 되돌리는 인코더 작성
  - 분포도·인식도·판정 시간 세 기준을 구현하고, 판정 시간 1.0~2.0초를 20구간으로 나눠 구간별 최고 분포 점수를 뽑는 층화 선별기 작성
  - 트레이 STL의 펠트 바닥 면을 실측해 플레이 경계를 바로잡음. 원본 웹 값을 그대로 옮겼더니 +Z 상한이 실제보다 낮아 착지가 화면 아래로 몰렸음
  - 회전량을 원본 웹 베이커와 같은 범위(일반 ±11, 판 뒤집기 ±40 rad/s)로 맞춤. 처음 값은 과해 저해상도에서 눈이 읽히지 않았음
  - 프리셋 20종을 다시 구움. 재생 코드와 JSON 포맷은 건드리지 않음
- 변경 파일: `Editor/DicePresetBakeRig.cs`, `Editor/DicePresetScoring.cs`, `Editor/DicePresetWriter.cs`, `Editor/DicePresetBaker.cs`, `Editor/DicePresetBakeTests.cs`(신규), `StreamingAssets/WebSource/presets/*.json`(21개 재생성)
- 실행한 검증: EditMode 793개 중 프로젝트 테스트 전부 통과, `Verify Pure Yaw Alignment` 600/600(최대 기울기 7.66도, 허용 25도), `Run Physics And Keep Validation` PASSED, 굴림 스크린샷 육안 확인
- 새 결정/가정: 프리셋 생산 경로를 `preset-studio`에서 Unity 에디터로 옮김. `preset-studio`는 트레이 STL 원본 생성 코드를 품고 있어 삭제하지 않고 그대로 둠
- 남은 문제/차단 요소: 판 뒤집기 프리셋 재생 길이가 원본 2.65초에서 2.0초 이하로 짧아짐. 판정 시간 상한을 맞추기 위한 의도된 변화지만 연출 강도는 실제 플레이에서 재확인 필요
- 다음 작업: `M10.8-T0` 기준선 캡처와 토글 골격

### 2026-09-06 — Claude (M10.8 셀 셰이딩 전환 구현)

- 작업 ID: `M10.8-T0`~`M10.8-T6`, `M10.8-T8`~`M10.8-T10`
- 시작 상태: `M10.7`·`M10.9` 커밋 완료. 화면이 여전히 "픽셀 필터를 씌운 3D 렌더"로 읽히고, M10.6의 포스트 색 양자화는 고유 색을 30색까지 줄이고도 이 문제를 풀지 못한 상태
- 완료 내용:
  - `Tessera/CelSurface` 셀 셰이더와 공유 include 신설. 노멀 축 스냅, 밴드 램프, 하드 그림자 계단, 하드 림
  - `PC_Renderer`를 Deferred에서 Forward로 전환. 셀 셰이더가 GBuffer를 채우지 않아 엣지 피처의 노멀 공급원이 `DepthNormals` 패스가 되기 때문
  - `RenderStyle { Baseline, Cel }` 런타임 토글을 `V` 키와 HUD `Style:` 버튼에 연결. 재료·조명·SSAO·렌더 타깃·엣지 임계값이 한 번에 따라감
  - 주사위 재료를 Lit 경로를 남긴 채 Cel 오버로드로 분기. 테이블·소품은 `CelStyleSwitcher`가 이미 만들어진 렌더러를 훑어 교체·복구
  - Cel에서 월드 카메라 렌더 타깃을 내부 해상도로 직접 생성. 가짜 픽셀화(1920 렌더 후 점 샘플링)를 실제 저해상도 렌더로 교체
  - `PixelReadabilityMetrics` 세 지표와 Baseline·Cel 동시 측정 도구 신설. M10.6의 "고유 색 수" 지표를 대체
  - 상세 문서 `docs/reference/art/cel_shading_pixel_plan.md` 작성
- 변경 파일: `Rendering/Shaders/CelSurface.shader`·`CelSurfaceShading.hlsl` 신규, `Scripts/Rendering/RenderStyle.cs`·`CelMaterialFactory.cs`·`CelStyleSwitcher.cs`·`PixelReadabilityMetrics.cs` 신규, `Editor/CelSurfaceTests.cs`·`PixelReadabilityMetricsTests.cs`·`RunPixelReadabilityValidation.cs` 신규, `Scripts/Rendering/TesseraPixelPalette.cs`·`PixelEdgeRendererFeature.cs`, `Scripts/Dice/DicePaletteCatalog.cs`, `Scripts/Games/AugmentedYacht/Presentation/`의 `AugmentedYachtController.cs`·`YachtCameraRig.cs`·`YachtLightingRig.cs`·`YachtInputRouter.cs`·`YachtSceneAssembler.cs`·`DiceVisualPool.cs`, `Settings/PC_Renderer.asset`, `docs/cel_shading_pixel_plan.md` 신규
- 실행한 검증: 정적 검토만. URP 패키지 소스에서 `unity_AmbientSky` 선언과 `DepthNormalsPass`의 비-OCT 출력 규약(`half4(normalWS, 0.0)`)을 확인해 셰이더 계약을 맞춤
- 새 결정/가정: 테이블·소품은 빌더에 분기를 넣지 않고 렌더러 순회 교체로 처리(롤백 지점을 한 곳으로 모으고 Baseline 경로를 무손상으로 유지). 밴드 수는 새 필드 없이 기존 `Metallic`에서 파생
- 남은 문제/차단 요소: **Unity 컴파일·테스트·플레이를 실행하지 않았다.** 셰이더 컴파일, 엣지 임계값 시작값, 화면 결과가 모두 미확인. `M10.8-T7` 특수 연출 셰이더 축소는 착수 전 확인 필요
- 다음 작업: `M10.8-T11` Unity 검증

### 2026-09-06 — Claude (M10.7 트레이 확대·상수 단일화, M10.8 계획 수립)

- 작업 ID: `M10.7-T1`~`M10.7-T4`, `M10.8` 계획
- 시작 상태: 작업 트리에 트레이 배율 확대 변경이 커밋되지 않은 채 쌓여 있었음. `DiceBoardMetrics`의 주석이 존재하지 않는 `Assets/Editor/DicePresetBaker.cs`를 참조했고, 신설한 `PlayBounds*` 상수는 소비처가 없었음
- 완료 내용:
  - 트레이 배율 0.05 → 0.06으로 올리고 프리팹 스케일·씬 인스턴스 Y를 맞춤
  - `AugmentedYachtController`의 중복 상수 `TrayScale`·`RollSurfaceY`·`TrayVisualY`를 제거하고 `DiceBoardMetrics` 참조로 교체
  - `TrayVisualY`를 `RollSurfaceY - PlayFloorSourceY * TrayScale`로 정의해 배율과 플레이 바닥 높이의 관계를 식으로 고정
  - 플레이 영역 경계 상수를 정의하고, 소스 원본과 Z 부호가 반대인 이유를 주석에 기록
  - 존재하지 않는 파일을 가리키던 주석을 사실에 맞게 고치고, 경계 상수에 아직 소비처가 없다는 사실을 명시
- 변경 파일: `Scripts/Core/DiceBoardMetrics.cs`, `Scripts/Games/AugmentedYacht/Presentation/AugmentedYachtController.cs`, `Prefabs/Tabletop/Yacht Tray Visual.prefab`, `Scenes/Augmented Dice.unity`
- 실행한 검증: 파생 상수 값 손계산 검증(`TrayVisualY` 0.81701186이 씬 값 0.81701183과 일치, `TrayRimTopY` ~2.257, `ActiveArrangedY` ~2.81, `TrayOuterWidth` 9.30), 제거한 상수의 잔여 참조 없음 확인
- 새 결정/가정: `D-033`(그래픽 트랙 마일스톤을 소수점으로 연장), `D-034`(포스트 양자화 대신 재료 단계 셀 램프로 전환하고 A/B 토글을 구조에 넣음)
- 남은 문제/차단 요소: 프리셋 베이커 파일 5개(`DicePresetBaker`·`DicePresetBakeRig`·`DicePresetScoring`·`DicePresetWriter`·`DicePresetBakeTests`)가 세션 중 병행 작성되어 아직 미추적 상태. 커밋 범위 확정 전까지 대기
- 다음 작업: `M10.8-T0` 기준선 캡처와 토글 골격

### 2026-09-04 — Claude (M10-T6·M10-T8 컨트롤러 분해)

- 작업 ID: `M10-T6`, `M10-T8`
- 시작 상태: `M10-T1`~`M10-T5`와 `M10-T7`이 끝나 컨트롤러가 1,593줄로 줄어 있었으나, 턴 흐름 플래그 5개와 씬·HUD 생성 코드가 그대로 남아 있었고 `ITurnDelaySource`는 정의만 있고 모래시계 직접 호출이 9곳 남아 있었음
- 완료 내용:
  - `PresentationPhase` 열거형을 추가해 `turnTransitionInProgress`·`hasCompletedRoll`·`isArranging`과 코루틴 핸들 2개의 조합을 여섯 가지 명시적 상태(Idle/TurnTransition/AwaitingRoll/Rolling/Arranging/Settled)로 통합
  - `YachtTurnFlowPresenter`로 세션 생성, 턴 타이머 콜백, 드래프트 선택, 점수 확정, 턴 넘김, 게임 종료, 상태 문구·타이머 문구를 이관
  - `YachtDiceRoundPresenter`로 주사위 화면 사본과 굴림 궤적·킵 정렬 애니메이션을 이관
  - `YachtSceneAssembler`로 씬 레이아웃 해석, 최소 월드·프레젠테이션 구성, 게임 흐름 UI 생성, 디버그 버튼 배선을 이관
  - `YachtRunicPresenter`로 추가 턴 적립·소모, 증강 점수 덮어쓰기, 룬 디버그 버튼을 이관
  - `ITurnDelaySource`에 `Pause`·`Stop`을 추가해 턴 흐름의 모래시계 직접 호출을 제거
  - `AugmentCardValidationRunner`가 리플렉션 대신 `YachtTurnFlowPresenter`를 직접 호출하도록 수정
- 변경 파일: `Presentation/PresentationPhase.cs`·`YachtTurnFlowPresenter.cs`·`YachtDiceRoundPresenter.cs`·`YachtSceneAssembler.cs`·`YachtRunicPresenter.cs` 신규, `Presentation/AugmentedYachtController.cs`, `Presentation/ITurnDelaySource.cs`, `Tabletop/HourglassTimer.cs`, `Editor/AugmentCardValidationRunner.cs`
- 검증 도구: 점수표 클릭을 흉내 낼 수단이 없어 `YachtTurnFlowPresenter.CommitScore`를 공개 진입점으로 두고, `Tessera/Validation/Visual Check/Auto Play Augmented Game` 메뉴로 드래프트·굴림·기입을 끝까지 자동 진행하도록 만듦. `M11-T7` 반복 전체 게임 테스트에서 재사용함
- 실행한 검증: Unity 에셋 재임포트 후 재컴파일, 신규·수정 스크립트별 컴파일 진단 조회, EditMode 전체 테스트, 플레이 모드 진입 후 런타임 오류 관찰, 증강 요트 자동 완주 1회
- 검증 결과: 컴파일 오류 0건, 콘솔 오류 0건. EditMode 755/761 통과(실패 1건은 `unity-skills` 패키지 자체의 `SKILL.md` 바이트 예산 테스트로 이 저장소 코드와 무관, 건너뜀 5건). 자동 완주에서 드래프트·굴림·킵 정렬·점수 기입 24회·턴 넘김·게임 종료 오버레이가 모두 동작(P1 61점, P2 76점). 컨트롤러 1,593줄 → 604줄(M10 시작 시점 대비 3,096줄 → 604줄)
- 새 결정/가정: `D-032`(400줄 기준을 책임 기준으로 대체). 흐름 프레젠터는 갱신 직전 `TrayRebindRequested`로 증강 트레이 참조를 다시 맞춰, 늦게 채워지는 카메라·트레이 참조를 붙잡지 않게 유지함
- 남은 문제/차단 요소: 없음
- 다음 작업: 사용자 결정에 따라 `M11-T1` 또는 `M7` 잔여 그래픽 작업

### 2026-09-03 — Claude (M7.5-R2 변형 18개 이관)

- 작업 ID: `M7.5-R2`
- 시작 상태: 변형 18종의 점수 규칙이 `YachtAugmentScoreEngine.ApplyReplacementScores`의 else-if 체인에, 획득 시 부수 효과는 `YachtAugmentRuntime.ApplyAugment`의 분기에 나뉘어 있었음
- 완료 내용: `Augments/Modification/`에 공통 기반 `ModificationAugment`와 처리기 18개를 추가하고 카탈로그에 등록함. 족보 판정 헬퍼를 `Augments/Core/YachtDiceFacts.cs`로 추출해 기본 점수 계산과 처리기가 공유하게 함. `YachtAugmentScoreEngine`에서 교체 로직과 전용 판정 헬퍼를 제거해 기본 족보 계산만 남김. 점수 미리보기·후보 생성 경로가 `IBeforeScorePreview` 처리기를 호출하도록 바꾸고, 정의 목록·드래프트 후보·랜덤 박스 후보가 카탈로그와 잔여 정적 배열을 합친 `AllDefinitions`를 쓰도록 정리함
- 변경 파일: `Augments/Modification/` 19개 신규, `Augments/Core/YachtDiceFacts.cs` 신규, `Augments/Core/AugmentContexts.cs`, `Augments/Core/YachtAugmentCatalog.cs`, `YachtAugmentRuntime.cs`, `YachtAugmentScoreEngine.cs`, `YachtGameRulesTests.cs`, `YachtModificationAugmentTests.cs`
- 실행한 검증: 생성된 C# 처리기에서 점수 식을 추출해 이관 전 규칙과 전수 대조(증강 18종 × 주사위 5개 조합, 눈 1~6과 1~7 두 범위, 12,852건). Unity 재임포트 후 EditMode 결과를 지우고 전체 재실행
- 검증 결과: 전수 대조 불일치 **0건**, 컴파일 오류 0건, EditMode **83/83 통과**
- 새 결정/가정: `D-026`(분류 공통 규칙은 런타임에, 증강 고유 부수 효과만 처리기에), `D-027`(`YachtDiceFacts` 추출). 정의 노출 순서를 유지하려고 카탈로그 등록 순서를 기존 배열 순서와 일치시킴
- 남은 문제/차단 요소: 없음
- 다음 작업: `M7.5-R3` 강화 중 주사위·상시형 이관

### 2026-09-03 — Claude (M7.5-R1 검증 및 R2 특성화 테스트 선행 작성)

- 작업 ID: `M7.5-R1` 검증, `M7.5-R2` 사전 준비
- 시작 상태: R1 구현은 끝났으나 클라우드 환경에 C# 컴파일러를 설치할 수 없어 컴파일·테스트가 미실행 상태였음
- 완료 내용: 변형 18종의 실패·경계 분기를 덮는 특성화 테스트 23개를 `Assets/Editor/YachtModificationAugmentTests.cs`로 추가함. Unity 에디터를 직접 조작해 재컴파일과 EditMode 전체 실행을 수행하고, Kind 3종 축소로 깨진 기존 단정 1건을 수정함
- 변경 파일: `Assets/Editor/YachtModificationAugmentTests.cs`(신규), `Assets/Editor/AugmentCardViewTests.cs`
- 실행한 검증: Unity 6000.3.21f1에서 `Ctrl+R` 재임포트 후 Console 확인, Test Runner EditMode 전체 실행 2회
- 검증 결과: 컴파일 오류 0건. 1차 실행 82/83 (기존 `CommonCard_이름효과종류대상상태를_같은레이아웃에표시한다`가 종류 라벨 `"족보 교체"`를 기대해 실패). 라벨 기댓값을 `"변형"`으로 갱신한 뒤 2차 실행 **83/83 통과**
- 새 결정/가정: 없음. `D-021`의 Kind 축소가 카드 종류 라벨 표시에 미치는 영향이 테스트로 확인됨
- 남은 문제/차단 요소: 없음. Unity MCP는 데스크톱 앱에 미등록 상태라 이번에는 에디터를 직접 조작해 검증함
- 다음 작업: `M7.5-R2` 변형 18개 이관

### 2026-09-03 — Claude (M7.5-R1 코어 골격)

- 작업 ID: `M7.5-R1`
- 시작 상태: 증강 로직이 `YachtAugmentRuntime.cs` 한 파일의 시점별 메서드에 `if (Owns(...))` 나열로 있었고, 발동 시점을 나타내는 구조가 없었음
- 완료 내용: `Assets/Scripts/Games/Yacht/Augments/Core/`에 시점 인터페이스 9종, 컨텍스트 7종, `IAugmentState`/`AugmentStateStore`, 카탈로그, 디스패처를 추가함. `YachtAugmentPlayerState`에 증강별 상태 저장소 `States`를 추가하고 `Clone()`에 반영함. `YachtAugmentKind`를 3종(`Modification`/`Enhance`/`Quest`)으로 축소하고 `YachtAugmentHook`을 제거함
- 변경 파일: `Augments/Core/` 6개 신규, `YachtAugmentRuntime.cs`, `AugmentCardView.cs`, `AugmentIconGalleryWindow.cs`, `AugmentCardViewTests.cs`
- 실행한 검증: 리포지토리 전체에서 제거된 심볼(`YachtAugmentHook`, 구 `Kind` 값 5종) 잔존 0건 확인, 신규·수정 파일의 괄호 균형 검사, 정의 팩토리 5개의 초기화 구문 육안 확인, CRLF 줄바꿈 보존 확인
- 검증 결과: 정적 검사 통과. 컴파일과 EditMode 테스트는 이 세션에서 실행하지 못해 후속 로그에서 확인함 (결과: 83/83 통과)
- 새 결정/가정: `D-024`(시점 인터페이스를 처리기가 있는 9개로 한정), `D-025`(`YachtAugmentHook` 제거). 코어는 아직 어디에서도 호출되지 않으므로 게임 동작 변화는 `Kind` 축소에 따른 카드 종류 라벨 표시뿐임
- 남은 문제/차단 요소: Unity 컴파일·EditMode 테스트 결과 확인 대기
- 다음 작업: `M7.5-R2` 변형 18개 이관

### 2026-09-03 — Claude (M7.5-R0 작업 계획서 갱신)

- 작업 ID: `M7.5-R0`
- 시작 상태: M7-T4까지 완료 후 사용자 지시 대기 중이었고, M7.5는 "M7 완료 후 예정"으로만 적혀 있었음
- 완료 내용: 사용자 지시로 M7을 중단하고 M7.5를 현재 마일스톤으로 승격함. 진행 포인터, 마일스톤 표, `M7.5` 상세 절(현재 구조의 문제·목표 구조·디렉터리·R0~R6 작업표·완료 조건), 결정 기록 `D-019`~`D-023`을 추가함
- 변경 파일: `docs/augmented_yacht_work_plan.md`
- 실행한 검증: 활성 증강 45개의 3분류 분해(변형 18 + 강화 16 + 퀘스트 11)를 이식 매트릭스 3.1~3.3절과 대조
- 검증 결과: 45개가 정확히 나뉘어 디렉터리 분류와 `D-021`의 근거가 성립함
- 새 결정/가정: `D-019`~`D-023`. 상태 직렬화는 인터페이스만 맞춰두고 실제 구현은 M9로 연기함
- 남은 문제/차단 요소: 없음
- 다음 작업: `M7.5-R1` 코어 골격 (사용자 지시 후 시작)

### 2026-09-03 — Claude (M7-T5 증강 고유 아이콘 이식)

- 작업 ID: `M7-T5`
- 시작 상태: 카드 아이콘이 `YachtAugmentKind` 6종 글리프를 절차적으로 그려 쓰고 있었고, 증강별 구분이 없었음. `AugmentCardView`에는 `Resources.Load<Sprite>("AugmentIcons/{id}")` 조회 경로가 이미 있었으나 해당 폴더가 없었음
- 완료 내용: 이전 프로젝트 `augmented-dice`의 `src/svgIcons.js`에서 활성 증강 45개 아이콘을 추출해 앤틱 잉크(`#3B2A1D`) 64×64 투명 PNG로 렌더링하고 Sprite/Point 임포트 설정 `.meta`와 함께 배치함. 고유 아이콘 사용 시 `icon.color` 틴트를 해제하고 아이콘 받침판을 제거함
- 변경 파일: `Assets/Resources/AugmentIcons/` (PNG 45 + `.meta` 45 + 폴더 `.meta`), `Assets/Scripts/Games/AugmentedYacht/AugmentCardView.cs`
- 실행한 검증: 45개 컨택트 시트 육안 확인, 카드 컨텍스트(받침판 56px·아이콘 46px·Point 필터)에서 잉크/틴트 3안 비교, 알파 경계 상자 검사로 빈 아이콘·과소 렌더 확인
- 검증 결과: 45개 모두 정상 렌더되고 두 톤 구조가 유지됨. 기존 테스트 `CommonCard_FallbackPixelIconUses64pxPointFilter`의 조건(64×64, Point)을 새 아이콘도 만족함
- 새 결정/가정: `D-022`, `D-023`. 원본의 도달 불가능한 중복 분기 2개(`lucky-sevens`, `every-little`)는 실제 게임에 표시되던 앞쪽 분기를 채택함
- 남은 문제/차단 요소: Unity 에디터에서의 임포트·표시 확인은 사용자 확인 대기
- 다음 작업: `M7.5-R0` 작업 계획서 갱신

### 2026-08-27 — Codex (M7-T4 Scene 조정값 기본 프리팹 이관)

- 작업 ID: M7-T4 증강 카드 후속 수정
- 완료 내용: 보조 말림 레이어를 제거하고 2.5바퀴 말림을 Z 210°로 고정함. 밝은 양피지 재질, 슬롯 전체 카드 크기, 원통형 가죽 띠·왁스·인장의 공통 말림 중심을 4종 프리팹과 런타임 폴백에 적용함
- 실행한 검증: 프리팹·프리뷰 재생성, 카드 전용·전체 EditMode, 선택 창 및 트레이 Game View, Console 확인
- 검증 결과: 카드 전용 24/24, 전체 60/60 통과, Play Mode 콘솔 오류 0건
- 남은 문제/차단 요소: 없음
- 다음 작업: 사용자 지시 대기

### 2026-08-27 — Codex (M7-T4 증강 카드 디자인 통합 완료)

- 작업 ID: M7-T4 증강 카드 디자인 수정
- 시작 상태: 선택 창과 트레이의 카드 디자인이 달랐고, 기존 말림 외곽과 철 봉 없는 카드형 스크롤로 재정리가 필요했음
- 완료 내용: 펼쳐진 오른쪽 종이를 직사각형 카드로 재구성하고 왼쪽에 2.5바퀴 말림·가죽 띠·정육면체 인장을 결합함. 철 봉과 리본은 제외하고, 4종 손상 프리셋과 얇은 시안 내부선 및 공통 텍스트 안전 영역을 선택 창·트레이에 함께 적용함
- 변경 파일: 카드/스크롤 런타임·에셋 생성기·EditMode 테스트, `Assets/Resources/AugmentScrolls`, 검증 캡처, M7 관련 문서
- 실행한 검증: 카드 전용 및 전체 EditMode, 양쪽 드래프트 선택, 선택 프리셋의 트레이 배치, Console 확인
- 검증 결과: 카드 전용 24/24, 전체 EditMode 60/60 통과. 선택 창과 트레이에서 같은 카드형 스크롤 외형을 확인했고 콘솔 오류는 0건
- 남은 문제/차단 요소: 없음
- 다음 작업: 사용자 지시 대기

### 2026-08-27 — Codex (M7-T4 3D 스크롤 검증 재실행)

- 작업 ID: M7-T4 후속 카드 시각 리디자인 검증
- 시작 상태: 이전 배치 검증이 Unity 라이선스 재연결 오류로 중단된 상태
- 완료 내용: Unity MCP 연결 후 전체 에셋 재임포트·컴파일, 카드 전용 및 전체 EditMode 회귀, Play Mode 보유 카드 3장 강제 표시, 일반 모드 비생성을 재검증함. 현재 NUnit과 호환되지 않던 `IReadOnlyList Has.Count` 테스트 표현은 직접 Count 비교로 수정함
- 변경 파일: `Assets/Editor/AugmentCardViewTests.cs`, `Assets/Screenshots/m7_t4_3d_scroll_validation.png`, M7 관련 문서
- 실행한 검증: 카드·양피지 EditMode, 전체 EditMode, 프리셋 0·2·4 런타임 강제 표시, 메시·인장·오버레이·레이캐스트 상태 검사, 일반 모드 비활성 검사, Game View 캡처, Console 확인
- 검증 결과: 카드·양피지 23/23, 전체 EditMode 59/59 통과. 보유 카드 3장 각각 정적 메시 5개·중앙 인장·활성 오버레이를 확인했고 일반 모드는 활성 카드와 오버레이가 모두 0개였음
- 시각 검수: 캡처 기준 펼친 본문은 정상 배치되지만 말림이 얇은 직사각 막대처럼 읽혀 레퍼런스의 느슨한 다층 롤 기준은 미통과. 캡처 도구가 Screen Space Overlay를 합성하지 않아 텍스트 선명도는 별도 수치 상태로만 확인함
- 콘솔: 프로젝트 코드 오류는 없으며 Unity Quick Install의 한글 경로 변환 예외 1건과 Unity AI 계정 API 경고 1건이 남음
- 남은 문제/차단 요소: 기술적 차단 없음. 3D 롤과 인장 시각 보정 필요
- 다음 작업: 롤 단면 노출과 본문 연결 곡면, 밀랍 인장 크기·명암을 재조정한 뒤 동일 구도로 재검증

### 2026-08-27 — Codex (M7-T4 3D 스크롤 리디자인 구현)

- 작업 ID: M7-T4 후속 카드 시각 리디자인
- 시작 상태: 레퍼런스의 펼쳐진 종이와 느슨한 말림, 중앙 밀랍 인장을 실제 3D 오브젝트로 구현하라는 사용자 지시 접수
- 완료 내용: 펼친 양면 종이, 2중 나선 롤, 인장 밴드, 불규칙 밀랍 인장, 리본 꼬리, 오버레이 안전 앵커를 갖춘 모델을 구현함. 종이 Base Color·URP Lit 재질과 5종 정적 메시 프리팹을 생성하고 보유 카드가 프리셋 ID에 따라 로드하도록 교체함
- 변경 파일: `Assets/Scripts/Games/AugmentedYacht/AugmentScrollModel.cs`, `Assets/Editor/AugmentScrollAssetGenerator.cs`, `Assets/Resources/AugmentScrolls`, `Assets/Scripts/Games/AugmentedYacht/AugmentTrayCardView.cs`, `Assets/Editor/AugmentCardViewTests.cs`, M7 관련 문서
- 실행한 검증: 격리된 Unity 프로젝트 컴파일 및 프리팹 생성, 생성 로그와 프리팹·메시·재질 파일 확인, 카드 전용 테스트 추가, 변경 파일 공백 오류 검사
- 검증 결과: Unity 컴파일 오류 0개, 프리팹 5종과 메시 25개 생성 성공. 배치 테스트 실행은 Unity 라이선스 서비스가 반복적으로 끊겨 결과 파일 생성 전에 중단됨
- 남은 문제/차단 요소: 메인 카메라 Play Mode 시각 검수와 EditMode 재실행 필요
- 다음 작업: Unity 라이선스가 정상 연결된 편집기에서 카드 전용·전체 EditMode와 5종 강제 표시 캡처 수행

### 2026-08-27 — Codex (M7-T4 3D 스크롤 리디자인 계획 전환)

- 작업 ID: M7-T4 후속 카드 시각 리디자인
- 시작 상태: 2D 스크롤 시안 계획을 3D 오브젝트 제작 방식으로 전환한다는 사용자 결정 접수
- 완료 내용: 상단 왼쪽의 다층 롤과 펼쳐진 본체, 중앙 밀랍 인장, 카메라용 형태 과장, 3단계 종이 재질, 5종 정적 메시 프리팹, 고해상도 오버레이 앵커를 포함한 3D 제작 계획으로 재작성함
- 변경 파일: `docs/augmented_yacht_m7_scroll_redesign_plan.md`, `docs/augmented_yacht_m7_graphics_plan.md`, `docs/augmented_yacht_work_plan.md`
- 검증 결과: 기존 슬롯 비율, High-Res Overlay, 프리셋 상태 상속, 월드 콜라이더 입력과 런타임 생성 구조를 유지하도록 계획 범위를 대조함. 중단된 2D 시안은 프로젝트에서 제거함
- 남은 문제/차단 요소: 구현 시작 전 사용자 승인 필요
- 다음 작업: 프리셋 0 무재질 블록아웃 제작 및 메인 카메라 가독성 검증

### 2026-08-27 — Codex (M7-T4 양피지 말림 형태 보정)

- 작업 ID: M7-T4 후속 카드 시각 고도화
- 시작 상태: 보유 카드의 왼쪽 말림이 원통을 배치한 것처럼 보이고, 고해상도 사각 오버레이가 3D 곡률을 평면처럼 보이게 하는 피드백 접수
- 완료 내용: 왼쪽을 본체와 연속된 1.28~1.42회 나선형 종이 단면으로 재구축하고, 왼쪽 아래가 강하고 오른쪽 아래가 약한 비대칭 하단 말림을 추가함. 메시 분할 확대, 카메라용 높이 과장, 앞·뒷면 재질 분리, 중앙 콘텐츠 전용 오버레이 투영을 함께 적용함
- 변경 파일: `Assets/Scripts/Games/AugmentedYacht/AugmentParchmentVisuals.cs`, `Assets/Scripts/Games/AugmentedYacht/AugmentCardView.cs`, `Assets/Scripts/Games/AugmentedYacht/AugmentTrayCardView.cs`, `Assets/Editor/AugmentCardViewTests.cs`, M7 그래픽 계획 및 현재 진행 포인터
- 실행한 검증: Unity 컴파일, 카드·상태 전용 EditMode 테스트, 전체 EditMode 회귀 테스트, Play Mode 드래프트·보유 카드 캡처, Console 확인
- 검증 결과: 컴파일 오류 0개, 전용 18/18 및 전체 54/54 테스트 통과. 보유 카드에서 나선 외곽·하단 뒷면·중앙 고해상도 콘텐츠 분리를 확인함
- 남은 문제/차단 요소: 없음
- 다음 작업: 사용자 시각 검수 후 미세 조정 또는 다음 M7 작업 진행

### 2026-08-26 — Antigravity (M7 카드 비주얼 개선 요구사항 반영 및 백업)

- 작업 ID: M7-T4 후속 시각 고도화 사항 정리 및 백업
- 시작 상태: M7-T4(보유 카드 3D 트레이 슬롯 배치 및 포인터 상호작용) 완료 후 개선 피드백 접수
- 완료 내용: 증강 카드 비주얼 3가지 핵심 개선 요구사항을 마일스톤 및 그래픽 계획 문서에 반영하고 변경사항 백업 커밋/푸시 준비
  1. **텍스트 픽셀 필터 우회**: 3D 픽셀화 렌더 타겟으로 인한 텍스트 깨짐 방지 위해, 점수표(`ParchmentScoreSheet`) 시스템과 동일하게 3D 트레이 슬롯 좌표를 스크린 오버레이(`Pixel Presentation` Canvas)로 투영(`WorldToScreenPoint`)하는 High-Res Overlay 파이프라인 확정
  2. **호버 애니메이션 개선**: 픽셀 격자 스냅 튐 없이 부드러운 보간(`Lerp`/`SmoothDamp`)을 통해 카드 고도와 스케일(1.0 → 1.06)이 자연스럽게 늘어나는 애니메이션 확정
  3. **스크롤(두루마리) 디자인 및 3~5종 테마**: 단정한 네모 카드 형태 대신 롤 바(Rolled Spindles)와 앤틱 놋워크 보더, 왁스 인장이 적용된 **펼쳐진 양피지 스크롤 외형 세트 3~5종** 제작 및 랜덤 출력 사양 확정
- 변경 파일: `docs/augmented_yacht_m7_graphics_plan.md`, `docs/augmented_yacht_work_plan.md`
- 실행한 검증: 문서 동기화, Unity EditMode 전체 테스트 통과 상태 보존
- 검증 결과: 마일스톤 문서 갱신 완료
- 남은 문제/차단 요소: 없음
- 다음 작업: 사용자 지시 대기

### 2026-08-26 — Codex (M7 협업 문서 분리)

- 작업 ID: M7 문서 구조 정리
- 시작 상태: M7 상세 표가 전체 계획서에 포함되어 있었고, 완료 즉시 다음 작업을 자동으로 `DOING` 처리하는 규칙이 사용자 승인 방식과 충돌했음
- 완료 내용: M7 전용 그래픽 계획 문서를 만들고 기존 M7 내용, 확정 시각 사양, 단계별 사용자 협업 규칙을 이전함. 전체 계획서는 M7 요약과 전용 문서 참조만 유지하도록 정리함
- 변경 파일: `docs/augmented_yacht_m7_graphics_plan.md`, `docs/augmented_yacht_work_plan.md`
- 실행한 검증: Markdown 링크, M7-T1~M7-T12 상태, 현재 포인터, 사용자 승인 규칙, Git 변경 범위 확인
- 검증 결과: `M7-T1`은 `TODO`·사용자 지시 대기이며 `M7-T10`은 `DEFERRED`; Unity 코드·씬·에셋 변경 없음
- 새 결정/가정: M7 상세 상태는 전용 문서만 기준으로 관리하고 다음 단계는 사용자가 작업 ID를 언급한 경우에만 시작함
- 남은 문제/차단 요소: 없음
- 다음 작업: 사용자 지시 대기

### 2026-08-25 — Codex (M6 전체 이식)

- 작업 ID: `M6-T1`~`M6-T8`
- 시작 상태: M5 대표 6종만 권위 런타임에 연결되어 있었고, 나머지 활성 증강의 공통 점수 계층·개인 진행 상태·수동 행동 경로가 없었음
- 완료 내용: 활성 증강 45개 정의와 설명, 점수 교체 18종, 기본/강화/주사위 보너스 분리, 음수 점수 기입 플래그, 퀘스트·라운드 비교·저축 상태, 특수 주사위 6종, 요트 뱅크, 갬빗 4→6개, 더블 다운, 등가교환, 연금술, 랜덤 박스와 개인 충돌 필터를 구현함
- UI 연결: M7 최종 그래픽 전의 기능형 수동 행동 버튼 5종과 노즈도르무 15초 모래시계 타이머를 연결함
- 제외 처리: 3~8번 6종은 삭제, `four-by-four`·`two-households`·`strange-die`·`coin-toss` 4종은 사용자 결정에 따른 미구현 상태로 드래프트와 랜덤 박스에서 제외함
- 실행한 검증: Unity 스크립트 재컴파일, 활성 정의/점수 교체/점수 계층/음수/요트 뱅크/프로모션/수동 행동/퀘스트 조합 EditMode 테스트, 일반 요트 전체 회귀 테스트, 메인 씬 Play Mode 진입 점검
- 검증 결과: Unity 컴파일 오류 0건, 전체 EditMode 36/36 통과, Play Mode 진입 후 Console 오류 0건
- 새 결정/가정: 원본 JSON 55개는 이력·표시 근거로 유지하되 실행 정의는 구현 승인된 45개만 사용한다. M6 완료 조건의 “미구현 없음”은 이후 사용자 결정과 충돌하므로 모든 항목의 구현·삭제·의도적 미구현 상태 확정으로 변경함
- 남은 문제/차단 요소: 규칙 구현 차단 없음. 카드·스티커·특수 주사위 외형·강화 점수 글로우·요트 뱅크 킵 존 금색 표시는 M7 범위
- 다음 작업: `M7-T1` 증강별 그래픽 에셋 및 상태 표현 목록 작성

### 2026-08-25 — Codex (M5 최신 규칙 재실행)

- 작업 ID: `M5-R1`
- 시작 상태: 기존 M5 대표 수직 구현은 완료되어 있었으나, 이후 확정된 개인 변형 정책과 달리 `lucky-sevens`가 전역 보유되고 중간 획득 시 양쪽 Aces를 초기화하며 양쪽에 추가 턴을 지급했음
- 완료 내용: `lucky-sevens`를 보유자 전용 정의로 전환하고 보유자의 Aces·총점·추가 턴만 원자적으로 변경하도록 수정. 상대 플레이어가 같은 변형을 별도로 획득할 수 있고 상대 점수 미리보기에는 효과가 적용되지 않도록 회귀 테스트 보강. `GlobalAugmentIds`는 명시적 전역 카드용 예약 상태로 유지
- 변경 파일: `Assets/Scripts/Games/Yacht/YachtAugmentRuntime.cs`, `Assets/Editor/YachtGameRulesTests.cs`, `docs/augment_migration_matrix.md`, `docs/yacht_state_ownership.md`, 이 문서
- 실행한 검증: Unity Console 컴파일 오류 확인, `Tessera.Editor.Tests.YachtGameRulesTests` 실행, 전체 EditMode 테스트 실행
- 검증 결과: Unity 컴파일 오류 0건, M5 규칙 테스트 22/22 통과, 전체 EditMode 28/28 통과
- 새 결정/가정: 변형 증강은 플레이어별 소유·중복·충돌·점수 계산을 사용하고, 복수 `random-box`는 P1→P2 순서만 고정하되 상대 결과를 후보 충돌로 보지 않음(`D-016`, `D-017`)
- 남은 문제/차단 요소: 없음. 전체 변형 증강과 플레이어별 족보 스티커·스냅샷 확장은 M6 범위
- 다음 작업: `M6-T1` 점수·족보 변경 증강 공통 실행 프레임 확장

### 2026-08-24 — Codex (`step-by-step` B안 적용 확정)

- 작업 ID: `M6-T2` 보상 조건 확정
- 시작 상태: `step-by-step`의 +35→+55 강화는 유지하되 상단 합계 63점 조건이 어렵다는 플레이 검토가 있었고, 55 또는 58 완화안을 비교함
- 완료 내용: B안으로 상단 보너스 기준을 63→58로 낮추고 +55 보상을 유지하도록 정적 설명, 마이그레이션 판정, 증강 런타임 판정을 확정
- 변경 파일: `Assets/StreamingAssets/WebSource/data/augments.json`, `Assets/Scripts/Games/Yacht/YachtAugmentRuntime.cs`, `Assets/Scripts/Games/Yacht/YachtGameCore.cs`, `Assets/Scripts/Games/Yacht/LocalGameAuthority.cs`, `Assets/Editor/YachtGameRulesTests.cs`, `docs/augment_migration_matrix.md`, 이 문서
- 실행한 검증: `step-by-step` JSON 항목과 런타임 `stepRewarded`·상단 보너스 처리 대조, 58점 경계·순서 실패·Phase 1 후보 필터 테스트, Unity EditMode 전체 테스트 실행
- 검증 결과: JSON 증강 55개 유지, `step-by-step` 보상 문구가 기준 58·보너스 55를 명시함. Unity EditMode 테스트 27개 모두 통과
- 새 결정/가정: 기본 +35는 유지하고, 순서 완료 후 보유자 전용 threshold 58 이상이면 +55를 확정한다(`D-015`). 별도 +20은 추가하지 않는다.
- 남은 문제/차단 요소: 저장·업적 UI가 새 `stepRewarded`/`stepBonusGranted` 상태를 표시·직렬화하는지는 M6 저장 계층 점검 필요
- 다음 작업: M6 저장·UI 계층에서 `stepRewarded`, 개인 threshold 58, +55 확정 상태를 스냅샷과 표시 문구에 연결

### 2026-08-24 — Codex (`step-by-step` 보상 조건 완화안)

- 작업 ID: `M6-T2` 사전 재검토
- 시작 상태: +35→+55 강화 보상 자체는 수용 가능하나, 상단 합계 63점 조건 달성이 어렵다는 플레이 피드백이 제기됨
- 완료 내용: 보상량을 유지하고 `step-by-step` 전용 threshold를 63→55 또는 58로 낮추는 안을 비교. 즉시 +55 보장안의 밸런스 위험과 중간 점수 경쟁 영향을 기록
- 변경 파일: `docs/augment_migration_matrix.md`, 이 문서
- 실행한 검증: 원본 `augments.json`·`authoritativeGame.js`의 `stepRewarded` 및 상단 보너스 조건 확인, 단순 목표 눈 재굴림 모델의 기준별 확률 추정
- 검증 결과: 63 기준은 단순 모델에서 약 4%, 58 기준 약 11%, 55 기준 약 17%로 추정됨. 실제 게임 확률은 전략·조합에 따라 달라짐
- 새 결정/가정: 우선 63→55 완화, +55 보상 유지. +20 즉시 지급안은 이번 조정의 1순위에서 제외
- 남은 문제/차단 요소: 55와 58 중 최종 threshold, 보너스 확정 후 상단 칸 초기화 시 보너스 유지 정책
- 다음 작업: 사용자 결정 후 `M6-T2`의 정적 설명·threshold 적용·경계 테스트를 갱신

### 2026-08-24 — Codex (`step-by-step` 보상 변경안 검토)

- 작업 ID: `M6-T2` 사전 검토
- 시작 상태: 원본 `step-by-step`은 Aces→Sixes 완료 후 상단 보너스를 +35에서 +55로 강화하며, 실제 보너스는 상단 합계 기준 달성 시 반영됨
- 완료 내용: 퀘스트 완료 즉시 +20점을 지급하는 보상안을 검토하고 긍정·부정 관점, 기본 +35 중첩 여부, UI·상태·업적·테스트 영향을 매트릭스에 기록
- 변경 파일: `docs/augment_migration_matrix.md`, 이 문서
- 실행한 검증: 원본 `augments.json`, `authoritativeGame.js`, `augmentRules.js`의 `stepRewarded`·상단 보너스 처리 대조
- 검증 결과: 현재 구현은 `stepRewarded`가 상단 보너스를 55로 바꾸고 퀘스트 보상 자체는 0점으로 기록하는 구조임을 확인
- 새 결정/가정: 기본 +35를 유지하고 별도 `questBonus`로 +20을 즉시 지급하는 안을 우선 검토한다. +20은 상단 합계 판정에 포함하지 않는다.
- 남은 문제/차단 요소: +20과 +35의 중첩 여부, 상단 보너스 선지급 예외, 실패 후 재도전 정책
- 다음 작업: 사용자 결정 후 `M6-T2`의 정적 설명·런타임 보상·저장 상태·회귀 테스트를 함께 갱신

### 2026-08-24 — Codex (M5 대표 증강 수직 구현)

- 작업 ID: `M5-T1`~`M5-T9`
- 시작 상태: 증강 모드는 기본 요트 규칙만 실행했고 드래프트·증강 런타임·발동 표현이 없었음
- 완료 내용: 정적 정의/런타임 상태 분리, 라운드 1·6·9의 양 플레이어 드래프트, 공통 처리 지점, `lucky-sevens`·`8-sided`·`no-time-to-waste`·`table-flip`·`random-box`, 전역 점수 초기화/추가 턴, 임시 선택·보유·발동 UI를 연결
- 변경 파일: `Assets/Scripts/Games/Yacht/YachtGameCore.cs`, `Assets/Scripts/Games/Yacht/LocalGameAuthority.cs`, `Assets/Scripts/Games/Yacht/YachtAugmentRuntime.cs`, `Assets/Scripts/Games/AugmentedYacht/AugmentedYachtController.cs`, `Assets/Editor/YachtGameRulesTests.cs`, `docs/augment_migration_matrix.md`, `docs/yacht_state_ownership.md`, 이 문서
- 실행한 검증: Unity 스크립트 재컴파일, M5 규칙 EditMode 테스트, 전체 EditMode 회귀 테스트, 공유 씬 Play Mode 드래프트/선택/혼합 주사위 굴림, Unity Console 확인
- 검증 결과: M5 규칙 18/18, 전체 EditMode 24/24 통과. Play Mode에서 드래프트 UI 중복 0건, 버튼 선택 후 혼합 프리셋·주사위 타입 일치, Unity Console 오류 0건
- 새 결정/가정: `table-flip`은 굴림으로 세지 않아 `no-time-to-waste` 성공을 유지함(`D-013`); 복수 `random-box`는 P1→P2로 순차 해결함(`D-014`)
- 남은 문제/차단 요소: 없음. 최종 카드·특수 주사위 외형·VFX·사운드는 계획대로 M7 범위
- 다음 작업: `M6-T1` 증강 공통 실행 프레임 확장

### 로그 템플릿

```markdown
### YYYY-MM-DD HH:mm — 작업자/에이전트

- 작업 ID:
- 시작 상태:
- 완료 내용:
- 변경 파일:
- 실행한 검증:
- 검증 결과:
- 새 결정/가정:
- 남은 문제/차단 요소:
- 다음 작업:
```

### 2026-08-24 — Codex (PC 2인 공유 변형 증강 정책)

- 작업 ID: `M5` 사전 설계 검토
- 시작 상태: 기존 구현은 증강 상태와 점수 변형을 플레이어별로 보관했으나, PC 버전에서는 변형 증강이 두 플레이어에게 함께 영향을 주도록 기획 변경
- 완료 내용: 기존 `modificationAugmentIds` 1~26번을 전역 변형 후보로 분류하고, Yacht 칸 변형 22~26번만 Phase 1(`currentRound = 1`)에 제한하도록 정책을 수정. 1~21번의 중간 획득 시 대상 점수 초기화·영향받은 플레이어 추가 턴, 전역 상태 분리, 드래프트·점수·UI·네트워크·random-box 연쇄 영향을 매트릭스에 기록
- 변경 파일: `docs/augment_migration_matrix.md`, 이 문서
- 실행한 검증: 기존 증강 분류와 Phase 1 전용 필터를 이전 권위 코드 기준으로 대조
- 검증 결과: 1~26번 점수/족보 변형과 기존 Phase 1 퀘스트(`fast-straight`, `step-by-step`)를 구분했으며, 3~8번은 폐기 검토 상태를 유지
- 새 결정/가정: 1~26번 변형은 전역 효과로 두고 Yacht 칸 변형 22~26번만 Phase 1에 제한한다(`D-012`). 1~21번은 중간 획득 시 대상 칸을 초기화하고 해당 플레이어에게 추가 턴을 준다. `yacht-bank`의 은행 진행, 주사위 구성 변경의 전역화, 전역 변형 중복, random-box 복수 발동·기준 58의 범위는 별도 결정
- 남은 문제/차단 요소: 중간 변형으로 두 플레이어가 동시에 초기화될 때 추가 턴 예약 순서, `yacht-bank` 공유 범위, 전역 변형 충돌·중첩 정책
- 다음 작업: `M5-T1`에서 전역/보유자 전용 상태 타입과 후보 필터 계약을 확정한 뒤 대표 증강을 검토

### 2026-08-24 — Codex (3~8번 anti 시리즈 폐기 검토)

- 작업 ID: `M1-T6` 사전 검토
- 시작 상태: 상단 고득점용 anti 시리즈 3~8번의 유지 여부가 미정
- 완료 내용: 3~8번을 `폐기 검토`로 표시하고, 삭제 또는 대체 논의 기준을 매트릭스에 기록
- 변경 파일: `docs/augment_migration_matrix.md`
- 실행한 검증: 3.4 결정 필드에서 3~8번 상태와 ID를 확인하고 기존 데이터·규칙 보존 여부 확인
- 검증 결과: 6개 항목 모두 `논의 필요`; 원본 JSON과 이전 구현은 삭제하지 않음
- 새 결정/가정: 최종 삭제 또는 대체 전까지 M5/M6 이식 대상으로 확정하지 않음
- 남은 문제/차단 요소: 대체 콘셉트의 수와 방향 미정
- 다음 작업: 삭제안과 대체안의 재미·이름·상태 복잡도·테스트 가능성 비교

### 2026-08-24 — Codex (M3 타이머 정책 확정)

- 작업 ID: `M3-T6`
- 시작 상태: 일반 모드 턴 제한이 현재 Unity 60초와 이전 웹 45초로 달라 사용자 결정 대기
- 완료 내용: 일반 모드 턴 제한을 60초로 확정하고 공용 옵션 상수, 컨트롤러 참조, 회귀 테스트와 문서를 동기화
- 변경 파일: `Assets/Scripts/Games/Yacht/YachtGameCore.cs`, `Assets/Scripts/Games/AugmentedYacht/AugmentedYachtController.cs`, `Assets/Editor/YachtGameRulesTests.cs`, `docs/augment_migration_matrix.md`, 이 문서
- 실행한 검증: Unity 스크립트 재컴파일, EditMode 전체 테스트, Unity Console 확인
- 검증 결과: 스크립트 오류 0건, Unity Console 오류 0건, EditMode 전체 15/15 통과
- 새 결정/가정: 일반 모드의 턴 제한은 60초 (`D-010`)
- 남은 문제/차단 요소: 없음
- 다음 작업: 후속 요청 시 `M5-T1` 대표 증강 5종 확정

### 2026-08-24 — Codex (M1~M4 구현)

- 작업 ID: `M1-T1`~`M4-T6` (`M3-T6` 제외)
- 시작 상태: 기존 증강 테스트 기준선 미확인, 주사위 값·킵·프리셋 난수가 Unity 컨트롤러에 결합, 모드 분리 없음
- 완료 내용: 이전 프로젝트 조사와 55개 이식표, 순수 C# 상태·명령·결과·난수·로컬 권위, 일반 요트 24턴 흐름, 일반/증강 실행 옵션과 기본 규칙 합성 구조 구현
- 변경 파일: `Assets/Scripts/Games/Yacht/YachtGameCore.cs`, `Assets/Scripts/Games/Yacht/LocalGameAuthority.cs`, `Assets/Scripts/Games/AugmentedYacht/AugmentedYachtController.cs`, `Assets/Scripts/Games/AugmentedYacht/ParchmentScoreSheet.cs`, `Assets/Editor/YachtGameRulesTests.cs`, `docs/augment_migration_matrix.md`, `docs/yacht_state_ownership.md`, 이 문서
- 실행한 검증: 이전 프로젝트 `npm test`, `npm run validate:augments`; Unity 표준 스크립트 검증; EditMode `YachtGameRulesTests`; Play Mode 일반/증강 시작 및 일반 굴림; Unity Console 확인
- 검증 결과: 이전 테스트 36개 파일 통과, 증강 55개 유효, Unity 전체 EditMode 14/14 통과, Play Mode에서 일반 굴림 결과가 권위 상태와 함께 확정되고 증강 모드가 기본 규칙으로 시작함, 새 컴파일 오류·런타임 예외 없음
- 새 결정/가정: 공유 씬 유지; 사용자의 요청에 따라 일반 모드에서도 왼쪽 증강 트레이 그래픽은 임시 유지하고 증강 논리만 비활성화
- 남은 문제/차단 요소: 후속 `M3-T6`에서 60초로 확정하여 해결됨
- 다음 작업: 후속 `M3-T6` 완료

### 2026-08-24 — Codex (증강 이식 매트릭스 초안)

- 작업 ID: `M1-T3`
- 시작 상태: 55개 증강의 표시 데이터는 존재했지만 대화형 검토를 위한 공통 필드와 행이 없었음
- 완료 내용: 55개 증강을 ID, 판정, 설명, 유형·대상·발동, 정적 파라미터, 런타임 상태, 충돌, 근거, 테스트, 상태 필드로 초안 작성
- 변경 파일: `docs/augment_migration_matrix.md`, `docs/augmented_yacht_work_plan.md`
- 실행한 검증: JSON 항목 수와 매트릭스 행 수·augmentId 집합·열 수를 비교
- 검증 결과: JSON 55개와 매트릭스 55개가 일치하며 숫자 ID 40 공백은 별도 기록함
- 새 결정/가정: augmentId를 주 식별자로 사용하고 정적 데이터·실행 규칙·런타임 상태를 분리함
- 남은 문제/차단 요소: 모든 행은 아직 검토 전이며 일부 이전 구현과 설명의 차이를 사용자 결정으로 확정해야 함
- 다음 작업: `lucky-sevens`부터 대화형 검토를 진행하고 확정 내용을 매트릭스에 반영

### 2026-08-24 — Codex (증강 그래픽 마일스톤 추가)

- 작업 ID: 문서 계획 보완
- 시작 상태: 증강 규칙 구현과 로컬 핫시트 사이에 최종 그래픽 제작 단계가 명시되지 않았음
- 완료 내용: `M7 증강 시스템 그래픽 요소 추가`를 신설하고 이후 마일스톤을 M8~M11로 재번호화
- 변경 파일: `docs/augmented_yacht_work_plan.md`
- 실행한 검증: 마일스톤 요약, 상세 작업 ID, 결정 기록, 미결정 사항의 번호 참조 확인
- 검증 결과: 카드, 특수 주사위, 발동 VFX, 점수표/진행 상태, 수동 행동 피드백, 사운드, 폴백, 성능 검증 작업이 계획에 포함됨
- 새 결정/가정: M5에서는 검증용 임시 표현만 사용하고 M7에서 최종 게임 품질의 그래픽을 완성
- 남은 문제/차단 요소: 그래픽 에셋별 제작/재사용 여부는 `M7-T1`에서 확정
- 다음 작업: 기존 진행 포인터대로 `M1-T1` 이전 프로젝트 테스트 기준선 확인

### 2026-08-24 — Codex

- 작업 ID: `M0-T1`
- 시작 상태: 일반/증강 요트 구현 순서가 대화로만 정리돼 있었음
- 완료 내용: 현재 코드 기준선, 목표 구조, 마일스톤, 상세 작업, 완료 조건, 위험, 결정, 재개 절차를 포함한 작업 계획서 작성
- 변경 파일: `docs/augmented_yacht_work_plan.md`
- 실행한 검증: 현재 Tessera 및 이전 `augmented-dice`의 관련 파일 목록과 핵심 구조 확인
- 검증 결과: 기본 요트 규칙·프리셋·증강 데이터는 존재하며 실제 증강 런타임과 네트워크 모듈은 아직 미구현임을 확인
- 새 결정/가정: 일반 요트를 기본 규칙으로 먼저 완성하고 공유 씬에서 증강 모드를 합성
- 남은 문제/차단 요소: 이전 프로젝트 전체 테스트 기준선은 아직 실행하지 않음
- 다음 작업: `M1-T1` 이전 프로젝트에서 `npm test` 실행

---
