# PIXEL 트랙 세션 로그

> **문서 종류**: 시점 기록
> 최신 항목이 위에 옵니다. 각 항목은 **작성 당시의 사실**이며 이후 코드 변경을 반영하지 않습니다.
> 양식은 [`process.md`](../../process.md) §5.3을 따릅니다.

<!-- 양식
### YYYY-MM-DD — <작업자> (<작업 ID> <요약>)

- 작업 ID:
- 변경:
- 검증:
- 다음 작업:
-->

---

### 2026-10-23 — Claude (PIXEL-T9 동전 22² 손그림 적용, 64² 예외 철회, D-053 개정)

- 작업 ID: `PIXEL-T9` (`DOING` 유지)
- 변경:
  - 사용자 결정으로 같은 날 「64² 복원(D-053)」을 철회하고 `D-054`(동전 면 22² 손그림, `D-050` 준수)를 추가함
  - 동전 면은 자동 축소 대신 손그림 도트를 씀. 원본 `Assets/04.Art/Source/Coin/coin_head_pixel.png`, `coin_tail_pixel.png`. 앞면 아폴론 흉상(월계관), 뒷면 월계수 화환과 리라. 음각 3색(#E5A93C, #956E27, #503B15)만 씀
  - `CoinMeshBaker`는 변환 없이 `Generated/Coin/Textures/coin_*_albedo.png`로 복사하며 `ValidateFaceSize`(`FaceTextureSize = 22`)로 크기를 검증함
  - `PropTexelTests`에 동전 테스트 5개 추가(해상도 = 바닥 텍셀 수, 텍셀이 필터 칸 이상이고 한 칸 늘리면 칸보다 작음, PNG 4장 22², 색 3색뿐, 크기 다르면 거부)
  - `CoinFaceTextureConverter.cs`는 남겨 두고 베이커가 호출하지 않음. 3색 상수만 테스트가 참조함. 삭제 여부는 사용자에게 제안 예정
- 검증: EditMode 451/451 통과(배치 모드, 컴파일 오류 0, 스킵 0). 새 원본 PNG meta를 Point·밉맵 없음·NPOT None으로 맞춘 뒤 재실행. 다음 단계는 사용자 플레이 확인
- 다음 작업: 사용자 플레이 확인 후 `PIXEL-T9` DONE 게이트. `CoinFaceTextureConverter.cs` 삭제 여부 확인

### 2026-10-23 — Claude (PIXEL-T9 동전 64² 복원, D-053. 앞선 T9 항목 정정)

- 작업 ID: `PIXEL-T9` (`DOING` 유지)
- 변경:
  - 정정 대상: 앞선 2026-10-23 「PIXEL-T9 소품·VFX 진행」 항목의 「코인 면: 22×22」, 「`CoinMeshBaker.CapRadius` 0.8959 → 0.8971」, 「`Tools/Tessera/Generate Coin Face Textures`」 서술. 이 항목은 작성 당시 사실로 두고 고치지 않습니다.
  - 사유: 22×22에서 앞면 얼굴이 덩어리로 뭉개지고 뒷면 화환이 원판처럼 보인다는 것을 사용자가 플레이 화면에서 보고했습니다. 사용자 결정 「64² 복원, D-053 예외」에 따랐습니다(`D-053`).
  - 되돌린 것
    - 코인 PNG 2장과 meta, `CoinFaceTextureConverter.cs`, `CoinMeshBaker.cs`를 HEAD(64²)로 복원. `CapRadius` 상수와 `ModelScale` public 전환도 HEAD 상태로 되돌림
    - `Tools/Tessera/Generate Coin Face Textures` 메뉴 제거
    - `PixelPathSize = 64` 위에 D-050 예외·D-053 주석 추가
    - `PropTexelTests`: 코인 1:1 테스트 3개 삭제, `CoinFaceKeepsDesignResolutionAsTexelRuleException`(64 고정) 추가
  - 유지한 것: 스크롤 PNG 76×64와 머티리얼 타일링, 깃펜·연기·별자리 임포터 설정, `PixelTextureImportRuleTests`, `TabletopPrefabBaker`
- 검증: 코인 복원 뒤 EditMode 재검증은 미실시. Editor가 Play Mode에 있었습니다. 사용자 확인 후 다시 돌립니다. 판정 NOT ASSESSED
- 다음 작업: Editor를 Play Mode에서 끈 뒤 EditMode 재검증. 이어서 플레이 화면에서 코인 면 확인 후 `PIXEL-T9` DONE 게이트

---

### 2026-10-23 — Claude (PIXEL-T3 정렬 주사위 17칸 DONE, 플레이 모드 사용자 확인)

- 작업 ID: `PIXEL-T3` (`DOING` → `DONE`)
- 변경:
  - 정렬 주사위 17칸 확정(`D-052`). 상세는 아래 DOING 항목 참조
  - 사용자 플레이 화면 확인: 「주사위는 잘 나옴」
  - `README.md` 태스크 표 `PIXEL-T3` `DONE`, 「완료 기록」 절 추가
- 검증: EditMode 450/450 통과(코인 복원 전 기준). 플레이 모드는 사용자 확인
- 다음 작업: `PIXEL-T9` 재검증(코인 복원 후 EditMode 재실행)

---

### 2026-10-23 — Claude (PIXEL-T3 정렬 주사위 17칸 확정, DOING)

- 작업 ID: `PIXEL-T3` (`TODO` → `DOING`)
- 변경:
  - 사용자 보고: 정렬된 주사위의 눈 모양이 여전히 맞지 않음. 이전 실험 코드는 18칸이었습니다.
  - 원인 분석: 위 README 확정안의 18칸 실패 이유와 같습니다.
  - 사용자 결정: 세 안 중 「17칸, 현재 메시」를 골랐습니다. 다른 두 안은 「17칸 + 눈 지름 축소」와 「안 A 15칸 + 눈 메시 보정」이었습니다.
  - 코드
    - `Assets/02.Scripts/Core/DiceBoardMetrics.cs`: `ArrangedTopCells` 18 → 17, 주석 수정.
    - `DiceVisualPool.cs`의 정렬 목표 위치 스냅과 `ArrangedDieSizeTests.cs`는 이전 실험 코드입니다. 이번에 정식 범위로 포함합니다.
    - 이번 줄로 이전 기록 「PIXEL-T3 실험 코드는 커밋하지 않음」을 대체합니다.
  - 테스트(`Assets/Editor/Tests/Rendering/ArrangedDieSizeTests.cs`)에 4개를 추가했습니다. 모두 FBX 실제 좌표로 칸 중심 샘플링을 흉내 냅니다.
    - `ArrangedCellsSampleEveryPipAsSameThreeByThree`
    - `ArrangedCellsKeepSameMaskUnderTenthCellJitter`
    - `EighteenCellsGiveDifferentMasks`: 거부 사례
    - `FbxPipSpacingAndDiameterRatiosMatchDocumentedValues`
- 검증
  - EditMode 450/450 통과, 실패·스킵 0. 콘솔 컴파일 오류 0. 씬 isDirty False.
  - 플레이 모드 확인은 아직입니다. 정렬된 주사위(활성·킵)의 눈 모양을 사용자가 확인할 차례입니다.
- 다음 작업: 플레이 모드 1920×1080에서 정렬 주사위 눈 모양을 확인한 뒤 종료 게이트로 갑니다.

---

### 2026-10-23 — Claude (PIXEL-T9 소품·VFX 진행, 종료 게이트 NOT ASSESSED)

- 작업 ID: `PIXEL-T9` (`DOING` 유지)
- 변경:
  - 판정 사유: 코인 플레이 모드 확인 미완. 기본 시작 화면과 증강 선택 화면에 코인이 나오지 않습니다. 필요한 검증은 증강 게임 진행 중 코인이 착지한 정지 자세에서 면 텍스처를 육안으로 확인하는 것이고, 사용자 확인 대기입니다.
  - 코인 면: 22×22, Point, mip 끔. `PropTexelTests` 통과, Unity 실측 22×22, GUID 불변
  - 스크롤 정지 자세 1:1: 텍스처 76×64, 머티리얼 타일링 (1.107582, 0.926584), GUID 불변. 플레이 모드 증강 선택 화면 슬롯 3개에서 칸 한 개 이동당 텍셀 증가량 평균이 U 1.002, V 1.004~1.008(최대 1.02는 가장자리 말림 곡면). 수정 전에는 약 0.76
  - 깃펜·연기·별자리 Point·mip 끔: `PixelTextureImportRuleTests` 통과(T9 대기 예외 제거). Unity 임포터 실측 일치
  - 설계와의 차이(모두 수락): 스크롤 크기 가정 오류 수정(헬퍼 입력을 슬롯 크기에서 본문 월드 크기로 변경, README 설계 절 참조). `CoinMeshBaker.CapRadius` 0.8959 → 0.8971(GLB 실측, 테스트가 잡음). `CoinFaceTextureConverterTests`는 알고리즘 검증용 크기 64 고정, 실제 해상도는 `PropTexelTests`가 검증. 런타임 폴백 경로와 `Generate Augment Scroll Assets` 메뉴가 생성된 본문의 `bounds × lossyScale`로 타일링을 계산하도록 맞춤(메뉴는 실행하지 않음). 거부 사례 테스트 `ScrollBodyWorldSizeRejectsMissingPreset` 추가
  - 한계: 스크롤 위상은 보정하지 않음(T7과 같은 "플레이 확인" 방침). 슬롯당 최대 한 줄 정도 중복될 수 있으나 종이가 거의 단색이라 육안으로 보이지 않음. 슬롯마다 위상이 다름(U 평균 0.47·0.79·0.78). 코인은 22텍셀이 약 22.97칸을 덮어 한 열 정도 중복이 원래 있음
  - 변경 파일
    - 코드: `Assets/02.Scripts/Games/Yacht/Presentation/AugmentViews/AugmentScrollTexelMapping.cs`(신규), `Assets/02.Scripts/Games/Yacht/Presentation/AugmentViews/AugmentScrollModel.cs`
    - 에디터: `Assets/Editor/Dice/CoinFaceTextureConverter.cs`, `Assets/Editor/Dice/CoinMeshBaker.cs`, `Assets/Editor/Games/Yacht/AugmentScrollAssetGenerator.cs`, `Assets/Editor/Tabletop/TabletopPrefabBaker.cs`
    - 테스트: `Assets/Editor/Tests/Rendering/PropTexelTests.cs`(신규, 12개), `Assets/Editor/Tests/Rendering/PixelTextureImportRuleTests.cs`, `Assets/Editor/Tests/Dice/CoinFaceTextureConverterTests.cs`
    - 에셋: 코인 PNG 2장(22×22), 스크롤 PNG(76×64), `AugmentScrollPaperFront.mat`·`AugmentScrollPaperUnderside.mat`, 임포터 meta 3개(별자리 Aries, `DiceSmokeBurstTexture`, 깃펜). 원본 보관 `Assets/04.Art/Source/AugmentScrolls~/parchment_scroll_albedo_source.png`(Unity 임포트 제외 폴더)
  - 기타: `.claude/agents/tessera-unity-operator.md`에 도구 함정 3건 기록
- 검증: EditMode 446/446 통과, 스킵 0. 콘솔 컴파일 오류 0
- 다음 작업: 코인 플레이 모드 확인(증강 게임 진행 중 착지 자세, 1920×1080) 후 `DONE` 게이트

### 2026-10-23 — Claude (PIXEL-T7 탁자·트레이 표면 완료)

- 작업 ID: `PIXEL-T7` (`DOING` → `DONE`, 종료 게이트 `PASS`)
- 변경:
  - 판자 608×75 ×4, 펠트 128×96, 양피지 5장(142×228, 137×220, 132×214, 128×208, 124×204) 생성. 임포트 Point·mip 끔
  - 펠트 런타임 위상 보정 `TabletopSurfaceBuilder.ApplyFeltPhase` 추가. `AugmentedYachtController` 초기화 시 MaterialPropertyBlock `_BaseMap_ST` 적용
  - 설계와 차이: `BuildTrayVisual` 타일링도 상수화(수락), 펠트 런타임 위상 보정 추가(수락 기준의 조건부 항목 발동), `DiceFaceOrientation.DefaultCameraPitch`를 `PixelFilterSettings.WorldCameraPitch`로 통일
  - 한계: F1/F2 내부 해상도 전환 시 위상 재계산 없음 → `PIXEL-T2`
  - 변경 파일
    - 코드: `Assets/02.Scripts/Games/Yacht/Presentation/Table/TabletopSurfaceBuilder.cs`, `Assets/02.Scripts/Games/Yacht/Presentation/Flow/AugmentedYachtController.cs`, `Assets/02.Scripts/Games/Yacht/Presentation/Table/ParchmentScoreSheet.cs`, `Assets/02.Scripts/Core/RuntimeAssetLibrary.cs`, `Assets/02.Scripts/Dice/DiceFaceOrientation.cs`
    - 에디터: `Assets/Editor/Dice/BuildDiceGraphicsPoC.cs`, `Assets/Editor/Tabletop/FeltTextureGenerator.cs`(신규), `Assets/Editor/Tabletop/WoodPlankTextureGenerator.cs`, `Assets/Editor/Tabletop/ParchmentTextureGenerator.cs`
    - 테스트: `Assets/Editor/Tests/Rendering/TabletopTexelTests.cs`(신규, 14개), `Assets/Editor/Tests/Rendering/PixelTextureImportRuleTests.cs`
    - 에셋: 판자 PNG 4장, 펠트 PNG·머티리얼, 양피지 PNG 5장(layer3·layer4 신규)과 L3·L4 머티리얼, `Assets/Resources/RuntimeAssetLibrary.asset`
- 검증:
  - 텍셀 1:1·Point·mip 끔: `TabletopTexelTests` 14개 통과. `PixelTextureImportRuleTests` 통과(T7 대기 예외 제거)
  - 펠트 골 3칸: 플레이 모드 1920×1080에서 런타임 위상 보정 적용 후 두 열에서 골 간격 28/28이 3칸. MPB `_BaseMap_ST` = (0.375, 0.4829629, 0.5, 0.5049894), .mat은 tiling (0.375, 0.4829629)·offset (0.5, 0.5) 그대로
  - 판자·양피지: 1080 스크린샷 점검에서 이상은 펠트 골뿐이었음
  - EditMode 446/446 통과, 스킵 0(T7 이전 기준선 420). 콘솔 컴파일 오류 0
- 다음 작업: `PIXEL-T9` 코인 플레이 모드 확인. `PIXEL-T3` 등 나머지는 트랙 README 순서를 따름

### 2026-10-23 — Claude (PIXEL-T9 소품·VFX 착수)

- 작업 ID: `PIXEL-T9` (`TODO` → `DOING`)
- 변경:
  - 조사 결과 계획과 어긋난 점: (1) 별자리 텍스처는 게임에서 쓰지 않음(`EnabledInGame=false`) (2) 실제 큐브 셰이더는 `DiceCosmicVolume`(계획의 `DiceCosmicCube`는 폴백), 시간 흐름이라 정지 자세 없음 (3) 코인 토스 1024×256 시퀀스 없음. 코인 토스는 코인 메시 사용 (4) 코인 면은 이미 64², Point, mip 끔으로 규칙 준수. 목표 `TexelCountFor(Floor)` 약 22²로 축소 (5) 깃펜은 비스듬한 자세라 면 방향이 하나가 아님
  - 사용자 결정: 정지 평면만 1:1(코인 면, 증강 스크롤 양피지). 깃펜·연기·별자리 텍스처는 Point·mip 끔 임포트 규칙만 적용. 셰이더 무늬 정수 칸 항목과 킵 존은 범위에서 뺌. 근거는 앞선 결정 `D-050`(엄격 1:1, 정지 자세만)
  - 생성은 텍스처 전용 메뉴 신설(`Tools/Tessera/Generate Coin Face Textures`, `Tools/Tessera/Generate Augment Scroll Texture`). 기존 `Tessera/Bake/Coin Model`은 머티리얼 GUID 변경과 프리팹 재생성, `Tessera/Graphics/Generate Augment Scroll Assets`는 프리팹·머티리얼 삭제 후 재생성이라 쓰지 않음
  - 주석 오차 발견(미수정): `CoinFaceTextureConverter`의 "약 16칸"은 실제 약 22칸, `CoinTossVfx`의 "동전 반경 0.8"은 실제 0.717
- 검증: 문서 갱신만 수행. 구현 전 착수 기록이므로 EditMode·플레이 모드 확인 없음
- 다음 작업: `PIXEL-T9` 구현(코인 면·스크롤 생성 메뉴, 임포트 규칙 정리, 스크롤 타일링 헬퍼) → Unity 재생성 → 검증(EditMode, 플레이 모드 1920×1080)

### 2026-10-23 — Claude (PIXEL-T7 탁자·트레이 표면 착수)

- 작업 ID: `PIXEL-T7` (`TODO` → `DOING`)
- 변경:
  - 조사 결과 계획과 어긋난 점 3개: (1) 펠트는 런타임 생성이 아니라 베이크 PNG(`Yacht_Tray_Visual_Runtime_Burgundy_Corduroy_Texture.png`) (2) 양피지 base 텍스처 한 장을 L3·L4·L5가 공유 (3) 펠트 PNG만 다시 쓰는 경로가 없음. 기존 베이크 흐름은 프리팹 15개를 다시 만듦
  - 사용자 결정 3개: Q7 펠트 골 주기 정수 3칸(굵음), 양피지 레이어별 텍스처 5장, 정지 위치 위상 스냅 생략(플레이 모드 확인 후 글리치가 보이면 UV 오프셋으로 보완)
  - 조사 정정: 판자 텍셀 수 78은 sin75 보정 누락. 올바른 값은 75(38×4.9 기준 608×75)
  - `DiceFaceOrientation.DefaultCameraPitch`를 `PixelFilterSettings.WorldCameraPitch`로 단일화(피치 상수 정리). 검증은 T7 검증에 묶음
  - PIXEL-T3 실험 코드(정렬 주사위 18칸, `ArrangedDieSizeTests.cs` 등)는 사용자 판단 「안 A가 맞음, 코드는 실험」에 따라 작업 트리에만 두고 커밋하지 않음
- 검증: 문서 갱신만 수행. 구현 전 착수 기록이므로 EditMode·플레이 모드 확인 없음
- 다음 작업: `PIXEL-T7` 구현 → Unity 재생성 → 검증(EditMode, 플레이 모드 1920×1080)

### 2026-10-23 — Claude (PIXEL-T6 공용 텍셀 밀도 규칙)

- 작업 ID: `PIXEL-T6` (`TODO` → `DONE`)
- 변경:
  - `Rendering/PixelTexelDensity.cs` 신규: `PixelSurfaceFacing`, `PixelTexelDensity`(`TexelWorldSize`, `TexelCountFor` 내림·최소 1, `CanvasUnitsPerWorldUnit = 100`). Games 참조 없음
  - 카메라 피치 단일화: `PixelFilterSettings.WorldCameraPitch = 75` 추가, `YachtSceneAssembler.CameraPitchAngle`은 이 값을 가리킴. `Dice/DiceFaceOrientation.cs:40` `DefaultCameraPitch`는 범위 밖으로 남김
  - `AugmentStickerTexture.TexelCountFor`는 새 API를 호출하고 최소 8텍셀 클램프만 유지. `ParchmentScoreSheet`·`AugmentTrayCardView`의 중복 상수 삭제
  - 임포트 규칙 검사를 전체 스캔 + 대기 예외 목록 축소 방식으로 구현(설계는 검사 목록 증가 방식). 검사 루트 `Assets/04.Art/Generated`, `Assets/04.Art/Source`, `Assets/Resources`. 영구 제외 3개, 대기 예외 20개(T7 4, T8 12, T9 4)
  - 설계 절 갱신, T7·T8·T9 설계 절에 대기 예외 제거 항목 추가
- 검증: 에셋 리프레시 후 컴파일 오류 0. EditMode 420/420, 실패·스킵 0. `PixelTexelDensityTests` 전부, `PixelTextureImportRuleTests` 3개, `AugmentStickerTextureTests` 11개 통과. 플레이 모드 확인 해당 없음(값·계산식 동일)
- 다음 작업: `PIXEL-T7` 탁자·트레이 표면(착수 전 미결 질문 7 펠트 골 주기 결정 필요). `PIXEL-T9`는 병렬 가능

### 2026-10-23 — Claude (PIXEL 텍셀 밀도 정합 설계, PIXEL-T6~T10 등록)

- 작업 ID: `PIXEL-T6`~`PIXEL-T10` 신규 등록(`TODO`). 결정 `D-050`, `D-051`
- 변경:
  - 사용자 요청: 트레이 픽셀 기준으로 폰트를 제외한 모든 요소의 PPU를 일치시키는 설계
  - 코드·에셋 조사: 텍셀 밀도가 칸당 약 0.42(판자)~14(별자리 큐브)로 흩어져 있음. Crisp UI 경로의 비폰트 그래픽은 화면 1px 단위
  - 사용자 결정 4건: Crisp UI 비폰트 그래픽 필터 이관, 엄격 1:1, 정지 자세만 보장, PIXEL 트랙 확장
  - `README.md`: 목표·범위·확정 결정 갱신, 「텍셀 밀도 정합 공통」과 `PIXEL-T6`~`PIXEL-T10` 설계 절 추가, 태스크 표·순서·완료 게이트·미결 질문 7·8·관련 결정 갱신
  - `decisions.md`: `D-050`, `D-051` 추가
  - `docs/STATUS.md`: PIXEL 남은 8개(전체 10)
- 검증: 문서 작업이므로 코드·테스트 검증 없음. 칸당 텍셀 수치는 메시를 단위 크기로 가정한 추정이며 화면에서는 미확인
- 다음 작업: `PIXEL-T6` 공용 텍셀 밀도 규칙. `PIXEL-T3`는 독립 진행 가능. 미결 질문 7은 `PIXEL-T7` 전, 8은 `PIXEL-T8` 전에 정함

---

### 2026-10-23 — Claude (PIXEL 결정 D-048·D-049 기록, PIXEL-T5 등록)

- 작업 ID: `PIXEL-T5` 신규 등록(`TODO`). `PIXEL-T2` 설계 갱신(상태 `DEFERRED` 유지)
- 변경:
  - `decisions.md`: `D-048` 배율 반올림 `floor`, `D-049` 시야 상한 없음·테이블 바깥 배경 채움 추가
  - `README.md`: 「미결 질문」 3·4번 해소 표기. 「PIXEL-T2」 설계에 두 결정 반영. 「완료 게이트」의 배율 테스트와 2560×1440 플레이 확인을 `PIXEL-T2` 재개 시 적용으로 표시. 태스크 표에 `PIXEL-T5` 추가와 순서 문장 보강. 「완료 기록」 T4 후속 문장을 `PIXEL-T5` 등록으로 바꿈
  - `docs/STATUS.md`: PIXEL 남은 3개(전체 5)
- 검증: 문서 작업이므로 코드·테스트 검증 없음. `PIXEL-T5`의 화면 가시성은 미확인
- 다음 작업: `PIXEL-T3` 정지 주사위 눈 선명도(설계안 A). `PIXEL-T5`는 `PIXEL-T4` 뒤. `PIXEL-T2` 재개 시 게이트 두 항목 적용

---

### 2026-10-23 — Claude (PIXEL-T1·PIXEL-T4 DONE 전환, 플레이 모드 확인)

- 작업 ID: `PIXEL-T1`, `PIXEL-T4` (DOING → DONE. 5단계 종료 게이트 PASS. 플레이 모드 사용자 확인 2026-10-23)
- 변경:
  - `README.md` 태스크 표: `PIXEL-T1`, `PIXEL-T4` 상태 `DONE`. 「완료 기록」 절에 두 항목 추가
  - `docs/STATUS.md`: PIXEL 남은 개수, 마지막 완료 갱신
- 검증:
  - 플레이 모드 확인(사용자, 2026-10-23). T1: 가장자리 여백과 UI·연출 위치 이상 없음. T4: 점수표 모든 행의 스티커 금테·그림자 두께가 같음
  - 5단계 판정: T1 PASS, T4 PASS
  - 0단계 READY 판정은 FAIL이었습니다(선행 `PIXEL-T1`이 DOING). 사용자 승인으로 진행했고, T1과 함께 플레이 모드를 확인해 해소했습니다
- 다음 작업: `PIXEL-T3` 정지 주사위 눈 선명도(설계안 A) 착수. `PIXEL-T2`는 DEFERRED(1920×1080 고정 작업)

---

### 2026-10-23 — Claude (PIXEL-T4 스티커 텍셀 밀도 정합 구현, DOING)

- 작업 ID: `PIXEL-T4` (DOING. 구현·EditMode 검증 완료, 플레이 모드 확인 대기. `PIXEL-T1`과 함께 받음. 판정: NOT ASSESSED(플레이 모드 미확인))
- 변경:
  - `AugmentStickerTexture.TexelCountFor(Vector2 worldSize)` 추가. 가로 = `floor(폭 / CellWorldSize)`, 세로 = `floor(깊이 × sin(CameraPitchAngle) / CellWorldSize)`, 하한 8
  - `ParchmentScoreSheet`: `StickerPixelsPerUnit`(25) 제거. `topLayerObject.lossyScale`을 곱한 월드 크기로 계산
  - `YachtSceneAssembler.CameraPitchAngle`: private → public const
  - 테스트 3개 추가: `TexelIsAtLeastOneFilterCellAlongX`, `TexelIsAtLeastOneFilterCellAlongDepth`, `TinyWorldSizeClampsToMinimumTexels`
  - `README.md` T4 설계 절 수치 정정(1.5 스케일 반영): 칸 대비 텍셀 가로 96%·세로 93%, 스티커 텍스처 59×15 → 약 56×13
- 검증:
  - 컴파일 오류 0
  - EditMode 387/387 통과, 스킵 0
  - 실수 1: 설계에서 `CameraPitchAngle`을 internal로 정했으나 asmdef가 없는 구조라 `Assembly-CSharp-Editor` 테스트에서 보이지 않았습니다. public으로 정정했습니다.
  - 실수 2: 테스트 입력 0.55가 하한 8에 걸려 실패했습니다. 0.6으로 교체했습니다.
  - 위험: 점수표 루트 스케일을 바꾸는 애니메이션은 코드에서 찾지 못했습니다. 프리팹·Animator 쪽은 미확인입니다.
- 다음 작업:
  - 플레이 모드 확인(`PIXEL-T1`과 함께). 점수표 모든 행의 금테·그림자 두께 확인
  - 범위 밖 발견(후속 후보, 사용자 결정 대기): 이름 열이 접히면 천 조각 가로 스케일만 줄고 텍스처는 펴진 크기로 고정됩니다. 그래서 세로 금테가 빠질 수 있습니다

---

### 2026-10-23 — Claude (PIXEL-T4 등록, 미결 질문 6 종료)

- 작업 ID: `PIXEL-T4` 등록(TODO, 선행 `PIXEL-T1`). 미결 질문 6 종료, 미결 질문 5는 `PIXEL-T4`로 이관
- 변경:
  - 사용자 결정(2026-10-23): 미결 질문 5는 새 태스크 `PIXEL-T4`로 분리하고 추천안으로 진행. 미결 질문 6은 해당 없음으로 닫음
  - `README.md`: 태스크 표에 `PIXEL-T4` 행 추가, 「PIXEL-T4 스티커 텍셀 밀도 정합」 설계 절 추가, 미결 질문 5·6 해소 표기
- 검증: 코드 변경 없음. 문서만 갱신
- 다음 작업: `PIXEL-T1` 플레이 모드 확인 후 `PIXEL-T4` 구현 전 점수표 루트·천 조각 스케일과 기울기 확인

---

### 2026-10-23 — Claude (PIXEL-T1 픽셀 격자 기준 단위 구현, DOING)

- 작업 ID: `PIXEL-T1` (DOING. 5단계 종료 게이트 중 플레이 모드 확인 미실시로 DONE 보류)
- 변경:
  - `PixelFilterSettings.cs`: `CellWorldSize = 1f/16f` 상수, `WorldOrthographicSize => ResolutionB.y * CellWorldSize * 0.5f`(1920×1080에서 8.4375) 추가
  - `YachtSceneAssembler.cs` `ApplyWorldCameraFraming`: 하드코딩 `8.2f`를 `PixelFilterSettings.WorldOrthographicSize`로 교체, `using Tessera.Rendering` 추가
  - `Assets/Editor/Tests/Rendering/PixelGridUnitTests.cs` 신규. 테스트 4개(`CellWorldSize` 정확값, `WorldOrthographicSize` 정확값, 세로 1칸 = `CellWorldSize`, `ApplyWorldCameraFraming` 적용)
  - `Assets/01.Scenes/Augmented Dice.unity`: Full Field World Camera(:4066)와 Crisp UI Camera(:8668)의 orthographic size 8.2 → 8.4375. 텍스트 치환 2줄로 처리
  - 함정: Unity `scene_save`로 씬을 저장하면 재직렬화 churn이 생겼습니다(내장 Mesh·Material 문서 4개 소실, 978줄 diff). 되돌린 뒤 텍스트 치환으로 처리했습니다. 씬의 카메라 값 변경은 `scene_save`를 쓰지 않습니다.
- 검증:
  - 컴파일 오류 0
  - EditMode `Tessera.Editor.Tests` 384/384 통과(`PixelGridUnitTests` 4, `PixelEdgeFilterTests` 7 포함)
  - 에디터 리로드 후 두 카메라 8.4375, isDirty false
  - 종료 게이트 DONE-CRITERIA: NOT ASSESSED 2026-10-23. 플레이 모드에서 화면 좌표 의존 UI·연출(입력 레이캐스트, 타이머, 깃펜 스냅, 증강 카드·점수판, 화면 가장자리 여백) 확인 미실시. 코드 조사상 화면 좌표 경로는 모두 카메라를 매번 읽어 따라가며, 시야 상수 가정 코드는 없음
  - 640×360 디버그 프리셋에서 1칸은 3/64 월드(의도된 동작, 기록만)
- 다음 작업: 플레이 모드 확인 후 DONE 게이트 판정. 후속 항목은 `README.md` 미결 질문 5·6 참조

---

### 2026-10-23 — Claude (PIXEL 트랙 신설)

- 작업 ID: 트랙 등록(`PIXEL-T1`~`PIXEL-T3` 신설)
- 변경:
  - 트랙 문서 `plan/tracks/pixel/README.md` 작성
  - `BuildDiceGraphicsPoC.cs:152` 펠트 판정 수정(`Mathf.Abs(normal.y)` → `normal.y`)
  - `yacht-tray.asset` 재생성(GUID 유지, 펠트 x 범위 ±51.62로 대칭화)
- 검증:
  - 컴파일 오류 0
  - 메시 재생성 수치 확인(펠트 x 범위 ±51.62)
- 다음 작업: `PIXEL-T1` 착수
