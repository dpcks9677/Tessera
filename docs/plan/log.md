# 공통 세션 로그

> **문서 종류**: 시점 기록
> 최신 항목이 위에 옵니다. 각 항목은 **작성 당시의 사실**이며 이후 코드 변경을 반영하지 않습니다.
> 2026-10-22 이전 항목은 옛 `docs/agent/session_log.md`에서 옮겼습니다. 양식은 [`process.md`](process.md) §5.3을 따릅니다.

<!-- 양식
### YYYY-MM-DD — <작업자> (<작업 ID> <요약>)

- 작업 ID:
- 변경:
- 검증:
- 다음 작업:
-->

---

### 2026-10-22 — Claude (하네스 정비 1·2단계, 태스크 ID 없음)

- 작업 ID: 없음. 하네스 구조 평가(저장소 밖 `C:\Users\dpcks\Downloads\tessera-harness-review.html`)의 H1~H3·M1~M4·L1~L3 중 1·2단계. 사용자 결정으로 태스크를 만들지 않고 세션 로그로만 기록. 3단계는 `ORCH-T6`
- 변경:
  - 1단계 (적용 완료)
    - H2 커밋 허가 강제: 프로젝트 `.claude/settings.json` 에 `permissions.ask` 추가. Bash·PowerShell 각각 `git commit *`, `git push *`. 전역 allow 는 다른 프로젝트용으로 유지. ask 가 allow 보다 우선
    - H3: `tessera-scout`·`tessera-reviewer` frontmatter 의 `skills: graphify` 제거. 전역 graphify SKILL.md(43.7KB)를 에이전트 생성 때마다 프리로드하던 비용 제거. 본문에 graphify 명령·실행 경로가 이미 있음
    - M1: `settings.local.json` 의 graphify hook-guard(Bash|Grep, Read|Glob) 제거. 서브에이전트·docs 면제가 없어 `main-session-guard` 와 이중 발화했고, `tessera-reader` 에도 graphify 강제 문구가 들어가 있던 문제. 훅은 세션 시작 때 읽히므로 다음 세션부터 적용
    - M4: 허용 목록 정리. `settings.local.json` allow 34개 → 3개(`dotnet build`, `graphify update`, `git add`). `__TRACKED_VAR__` curl 12개, 일회성 PowerShell 12개(원격 설치 스크립트 `irm … | iex` 포함), `python -c` 3개 삭제. 전역 `~/.claude/settings.json` 의 포트 8090 하드코딩 curl 7개 삭제
    - 전역·로컬 설정은 git 미추적이라 이 로그가 유일한 변경 기록
  - 2단계 (적용 완료, 저장소 밖 전역 파일)
    - H1: `~/.claude/CLAUDE.md` 「하위 에이전트 위임 규칙」 절에 우선순위 문장 추가. 프로젝트 CLAUDE.md 에 오케스트레이션 규칙이 있거나 프로젝트 `.claude/agents` 에 같은 역할 에이전트가 있으면 이 절 대신 프로젝트 쪽을 따름. 전역 「Unity 작업은 메인이 직접」 규칙과 프로젝트 「unity-operator 위임」 규칙의 충돌 해소
    - 전역 에이전트 scout·implementer·reviewer 설명 끝에 「프로젝트 전용 에이전트가 있으면 그쪽을 쓰고 이 에이전트는 쓰지 않음」 추가
  - 사용자 결정: Tessera 는 이 PC 에서만 작업하므로 Karpathy 지침은 전역 CLAUDE.md 에만 두고, 프로젝트 CLAUDE.md §1~4 는 `ORCH-T6` 에서 삭제. L3(에이전트 정의의 기기 고정 경로)는 조치 불필요
- 검증: 미실행. 1단계 ask 규칙의 확인창 실제 발화 여부는 미확인
- 미결: L2 전역 `unity-mcp-skill` 삭제 여부. Unity MCP 서버가 전역·프로젝트 어디에도 없어 이 프로젝트에서 동작 불가. 사용자 답 대기
- 다음 작업: 다음 세션에서 `ask` 확인창 재시험(auto 모드에서 건너뛰면 PreToolUse 훅 방식으로 전환). `ORCH-T6` 착수

---

### 2026-09-20 — Claude (훅 Python 이식 잔여 정리)

- 작업 ID: 없음. 커밋 `4aee9db`의 Node → Python 훅 이식에서 검증되지 않은 채 남아 있던 3개 지점을 정리하며, 마일스톤·태스크로 기록하지 않는다
- 시작 상태: 머지 `b068bef` 이후 훅 2개와 `settings.json`은 Python 경로로 일관됐고 `korean-guard.js` 잔여 참조도 0건. 저장소 상태 자체는 이미 정상이었고, 남은 것은 이식 당시 확인하지 못한 동작 3건
- ① 서브에이전트 면제 검증: `main-session-guard.py`에 임시 프로브를 넣어 훅 입력 원본을 덤프하고, 주 세션 Bash 호출과 `tessera-scout` Bash 호출의 키를 비교했다. 서브에이전트 입력에만 `agent_id`와 `agent_type`이 실리는 것을 확인해 기존 `agent_id` 면제가 유효함을 확정. 코드 변경 없이 주석에 확인 근거를 남기고 프로브는 제거
- ② `korean-guard.py`가 stdin을 읽지 않던 비대칭 해소: JS 판을 그대로 옮기며 빠진 부분. 하네스가 프롬프트 페이로드를 stdin으로 넘기므로 파이프를 비우지 않으면 페이로드가 파이프 버퍼를 넘길 때 Windows에서 기록 측이 막힐 수 있다. `sys.stdin.buffer.read()` 한 줄 추가
- ③ 셸 탐색 경고의 오탐 축소: 기존 정규식은 `git`·`graphify` 로 시작하는 명령만 면제해, 다른 명령의 출력을 `head`로 자르거나 `docs/`·`.claude/` 문서를 읽을 때도 매번 경고가 붙었다. 명령을 `|`·`;`·`&&` 단위로 나눠 세그먼트별 선두 명령과 경로 인자를 보도록 교체. 경로 인자가 없으면 표준입력 필터로 보고 지나가되 `rg`·`find`는 인자 없이 작업 디렉터리를 훑으므로 경고를 유지하고, 경로 인자가 전부 `docs/`·`.claude/`·`*.md` 면 면제한다
- 검증: 케이스 12개 표로 판정해 불일치 0건. 면제 6건(`ls docs/ | head`, `git ls-files | head`, `cat .claude/settings.json`, `docs/` 대상 `grep` 2건, `/health` curl), 경고 6건(`Assets/` 대상 `grep`, 인자 없는 `rg`, `find .`, `Assets/` 경로 `cat`, `/skill/` REST curl). 서브에이전트 입력은 무출력·exit 0, `korean-guard.py`는 100KB 페이로드에서 exit 0
- 시각 확인 생략, 사유: 훅 스크립트 변경이라 화면 결과물 없음
- 변경 파일: `.claude/hooks/main-session-guard.py`, `.claude/hooks/korean-guard.py`, `CLAUDE.md`, 이 문서
- 다음 작업: `M17-T23-7` 동전 시각 재점검

### 2026-09-20 — Claude (에디터 스크립트 기능별 폴더 분리, 디렉터리 정리 2단계)

- 작업 ID: 없음. 어제 아트·오디오 재편(커밋 `a2acacb`)에 이어지는 작업이며 마일스톤·태스크로 기록하지 않고 세션 로그로만 남긴다
- 동기: 외부 구조 평가에서 "코드 구조는 목적 중심인데 에셋·에디터 구조는 타입 중심"이라는 지적을 받음. `Assets/Editor` 루트에 테스트·베이커·생성기·검증 도구 55개가 평면으로 섞여 있었음
- 결과 구조: `Tests/{Yacht 13, Dice 8, Tabletop 4, Rendering 3}` + `Tests/` 루트 2개(`FontFallbackTests`, `RuntimeAssetGuardTests`), `Yacht/` 3개, `Dice/` 11개, `Tabletop/` 4개, `Rendering/` 4개, `Tools/` 2개. 총 54개 이동
- 분류 기준: 테스트는 검증 대상 도메인을 따라 프로덕션 폴더와 대칭을 이룬다. 도메인 폴더는 그 도메인의 에셋을 굽거나 씬을 조작하거나 검증하는 코드가 들어간다. `Tools/`는 도메인에 묶이지 않는 전역 도구. 동전은 굴려서 면이 결정되고 메시 베이크 → 면 텍스처 → 프리팹 파이프라인이 주사위와 같아 `Dice/`에 둠
- 삭제 1건: `ProcessRunnerTexture.cs`. 입출력 png를 어제 삭제했고 원본 jpg가 저장소 밖 개인 경로라 다른 환경에서 실행 불가
- 이동이 안전했던 근거: 프로젝트에 asmdef가 없어 `Assets/Editor` 하위 어느 깊이든 `Assembly-CSharp-Editor`로 컴파일된다. `[MenuItem]` 메뉴 경로는 파일 위치와 무관하다. 에디터 스크립트를 경로 문자열로 여는 코드는 없다
- 검증: GUID 54개 전부 보존, 컴파일 에러 0. 디스커버리 캐시 갱신 후 Tessera 테스트 328개(29개 클래스)로 이동 전과 클래스별 개수까지 일치. EditMode 전체에서 Tessera 328/328 통과, 실패·스킵 0. 패키지 `UnitySkills.Tests.*` 860개 중 실패 6·스킵 6은 패키지 소속(csproj 공유 위반·타임아웃, 실행마다 건수 변동). 메뉴는 조회 엔드포인트가 없어 `[MenuItem]` 문자열과 컴파일 포함으로 갈음. 시각 확인 생략, 사유: 에디터 스크립트 이동이라 화면 결과물 없음
- 문서: `docs/reference`·`docs/agent` 활성 문서 12개의 `Assets/Editor` 경로를 갱신하고 `architecture_overview.md` §3.2를 신설. `m17_vfx_spec.md`의 `DiceSmokeSpriteBaker.cs`가 커밋 `2497bfc`에서 이미 삭제된 파일을 현재형으로 가리키던 서술을 교정
- 후속 등재: `improvement_tasks_spec.md`에 `ARCH-02`(기능 경계 에셋 재편)·`ARCH-03`(`Resources` 축소·Addressables 검토)·`ARCH-04`(`DicePresetCatalog` StreamingAssets → `UnityWebRequest` 대응)·`ARCH-05`(asmdef 전체 설계)·`LOAD-01`(런타임 6개 스크립트 에디터 전용 로딩 결함)·`LOAD-02`(죽은 `Resources.Load` 폴백) 추가. `ARCH-01`은 asmdef 부분 도입을 전제한 항목이라 `ARCH-05`에 흡수하고 `VOID` 표기
- 알게 된 것: `Packages/manifest.json`의 `testables`는 지워도 Unity가 패키지 재해석 때 스스로 복원한다. 어제 제거해 커밋했으나 즉시 되돌아온 것을 확인했다. 제거 대신 `UnitySkills.Tests.*`를 판정에서 걸러내는 방침으로 전환하고 `.claude/agents/tessera-verifier.md`에 기록
- unity-skills REST 서버는 Unity 재시작 시 꺼지고 사용자가 창에서 수동으로 켜야 한다. 이번 세션에서 실제로 중단됐고 포트도 8091에서 8090으로 바뀌었다
- 변경 파일: 이동된 에디터 스크립트 54개(및 `.meta`), 삭제된 `ProcessRunnerTexture.cs`, `docs/reference/architecture_overview.md`, `docs/reference/augments_specification_and_status.md`, `docs/reference/art/cel_shading_pixel_plan.md`, `docs/reference/art/pixel_edge_filter_plan.md`, `docs/agent/work_plan.md`, `docs/agent/improvement_tasks_spec.md`, `docs/agent/m17_coin_mesh_plan.md`, `docs/agent/m17_vfx_spec.md`, `docs/agent/m7_graphics_spec.md`, `docs/agent/m17_quill_hover_animation.md`, `docs/agent/solid_refactoring_work_plan.md`, `docs/agent/session_log.md`
- 다음 작업: 미지정

### 2026-09-19 — Claude (아트·오디오 디렉터리 재편 및 미사용 에셋 정리)

- 작업 ID: 없음. 마일스톤·태스크로 기록하지 않고 세션 로그로만 남긴다(사용자 결정)
- 동기: 아트 리소스가 `Art/`, `Textures/`, `Fonts/`, `Resources/`, 루트 msVFX 팩, `StreamingAssets/WebSource/` 여섯 곳에 흩어져 있었음
- 새 규칙: `Assets/Art/{Source,ThirdParty,Generated}/<도메인>/`. 출처(누가 이 파일을 쓰는가) 기준으로 최상위를 나눈다. `Resources.Load` 대상 생성물은 `Generated/<도메인>/` 안에 중첩 `Resources/` 폴더를 둔다. 오디오는 `Assets/Audio/Sfx/<도메인>/<종류>/`
- 선행 커밋: `eb7a85e`로 기존 미커밋 에디터 재직렬화 변경분을 먼저 분리
- 이동: Unity `asset_move`로 60건 이동, GUID 보존 확인. 폴더 20개 신설
- 삭제 41건. 판정 규칙은 GUID·C# 문자열 경로·`Resources.Load` 이름·`StreamingAssets` 파일 IO 네 경로 모두에 참조가 없는 것. 예외로 `Art/Source` 원본과 msVFX 팩은 보존
  - 효과음 `roll_45.mp3`·`scoreboard.mp3`·`turn_change.mp3` (코드 참조 없음)
  - `WebSource/ui/`, `textures/`(14개), `models/yacht-tray.stl`, `data/augments.json` (읽는 코드 없음. 웹 프로젝트 잔재)
  - `runner_emerald_ribbon.png`, `runner_emerald_wide.png`, `score_sheet_grid.png`, `wood_grain_knots.png` (참조 없음)
  - `Dice_Digit_1~6.mesh` (현재 `DiceShapeBaker`가 생성하지 않는 옛 산출물)
  - AugmentScrolls 메시 16개 (`_PaperFrontAndUnderside`·`_RolledInnerLayers`·`_SealBand`·`_WaxSeal`, 프리셋 프리팹이 참조하지 않는 옛 파트)
  - `Readme.asset`, `normal_dice.blend.bak`, `normal_dice.blend1`, 빈 `Screenshots/`
  - 보존: `SampleSceneProfile.asset`(RP 에셋이 참조), `InputSystem_Actions`(`EditorBuildSettings`가 참조), Tabletop 머티리얼·메시·텍스처 149개(전부 사용 중)
- 코드: C# 경로 문자열 약 20개 파일 교체. `AugmentScrollAssetGenerator`는 다단계 폴더 생성으로 변경. `YachtAudioService`를 `StreamingAssets` + `UnityWebRequest` 로딩에서 직렬화 필드 `rollClips`/`impactClips`로 전환하고 `PublishClips()`를 추가해 컨트롤러 `Start`에서 호출. 씬 `Dice Graphics PoC`에 `YachtAudioService` 컴포넌트를 붙이고 클립 8개 할당. 오디오 임포트는 DecompressOnLoad·Vorbis·Force To Mono
- `.gitignore`에 `*.blend1`, `*.blend.bak`(및 `.meta`) 추가
- 문서: `docs/reference`·`docs/agent` 문서의 경로를 갱신하고 `architecture_overview.md` §3.1을 신설. 시점 기록 문서(session_log 과거 항목, decisions, completed, archive)는 미수정
- 검증: 컴파일 에러 0. 누락 참조 0(씬·프리팹·머티리얼 117개 파일, 참조 GUID 264개 대조). EditMode Tessera 328/328 통과(29개 클래스). 패키지 `UnitySkills.Tests.*` 860개 중 3 실패·6 스킵은 패키지 소속(타임아웃·csproj 공유 위반). Play 모드 Console 에러·경고 0, `YachtAudioService`·`BakedDiceController` 클립 4/4·4/4, 판자·양피지 텍스처, HUD 폰트 Mulmaru, `Quill_Model` 로드 확인. 시각 확인 생략(전 항목 조회로 확인)
- 알려진 문제·후속:
  - 런타임 스크립트 6개가 `#if UNITY_EDITOR` `AssetDatabase`로만 텍스처·폰트·모델을 로드해 플레이어 빌드에서 null (별건)
  - `AugmentScrollModel.cs:438` `Resources.Load("Parchment/parchment_base")` 폴백은 원래부터 죽은 코드
  - `ProcessRunnerTexture`는 저장소 밖 개인 경로를 읽어 다른 환경에서 실행 불가
  - `Packages/manifest.json`의 `testables`에 `com.besty.unity-skills`가 되살아나 있어 EditMode 전체 실행에 패키지 테스트 860개가 섞임(원래 의도적으로 제거했던 항목. 커밋 `eb7a85e`에 포함됨)
  - `AugmentCardViewTests`는 단독 실행 시 `QuestCard_*` 5개가 TMP 아틀라스 파괴로 실패, 전체 스위트에서는 통과
- 변경 파일: 이동/삭제된 에셋 60+41건, C# 경로 문자열 약 20개 파일, 씬 `Dice Graphics PoC`, `.gitignore`, `docs/reference/architecture_overview.md`, `docs/reference/art_style_guide.md`, `docs/reference/augments_specification_and_status.md`, `docs/agent/work_plan.md`, `docs/agent/m17_coin_mesh_plan.md`, `docs/agent/m7_graphics_spec.md`, `docs/agent/session_log.md`
- 다음 작업: 미지정

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
