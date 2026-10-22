# AGENTS.md

이 문서는 본 리포지토리에서 작업하는 모든 AI 에이전트 및 모델들을 위한 공통 프로젝트 규칙 및 가이드라인입니다.

---

## 1. 기본 작업 원칙
- **언어**: 모든 답변, 주석, 문서는 한국어로 작성합니다.
- **식별자는 영문**: 클래스·메서드·필드·지역 변수는 물론 **테스트 메서드명까지 영문으로만 짓습니다.** 한글 식별자는 쓰지 않습니다. 주변 파일이 한글 이름을 쓰더라도 따라가지 않습니다. 한국어 맥락은 주석과 문서에 남깁니다. 근거는 [`docs/reference/coding_conventions.md`](docs/reference/coding_conventions.md) §3을 보십시오.
- **간결성 (Simplicity First)**: 불필요한 과도한 추상화나 불필요한 PBR 연산 대신, 명확하고 간결한 코드를 작성합니다.
- **정밀 수정 (Surgical Changes)**: 요청받은 영역만 정확히 수정하며, 연관 없는 코드를 임의로 리팩터링하지 않습니다.
- **모듈 아키텍처 준수**:
  - 프로젝트명: `Tessera` / 루트 네임스페이스: `Tessera`
  - 메인 씬: `Assets/Scenes/Augmented Dice.unity`
  - 공통 시스템: `Tessera.Core`, `Tessera.Dice`, `Tessera.Tabletop`, `Tessera.Rendering`
  - 게임별 독립 모듈: `Tessera.Games.Yacht` (요트 규칙과 증강 로직), `Tessera.Games.AugmentedYacht` (증강 요트 프레젠테이션)
  - 네트워크/멀티플레이어: `Assets/Scripts/Network/`는 아직 빈 폴더입니다
  - 위 이름들은 어셈블리가 아니라 **C# 네임스페이스**입니다. 프로젝트에 `.asmdef`가 없어 모든 스크립트가 `Assembly-CSharp` 하나로 컴파일되므로, 모듈 경계는 컴파일러가 아니라 규약으로 지킵니다
- **코딩 규약**: C# 코드는 [`docs/reference/coding_conventions.md`](docs/reference/coding_conventions.md)와 리포지토리 루트의 `.editorconfig`를 따릅니다. Microsoft C#/.NET 규약 기준이며 Unity 직렬화 관련 예외가 명시돼 있습니다. 규약은 신규 파일과 실제 수정하는 파일에만 적용하고, 기존 파일을 일괄 재포맷하지 않습니다.
- **구조적 결정**: 상태 소유권·계층 경계 등 구조 판단은 [`docs/reference/architecture_decisions.md`](docs/reference/architecture_decisions.md)의 ADR을 먼저 확인합니다. 짧은 결정은 [`docs/decisions.md`](docs/decisions.md)에 있습니다.

---

## 2. 문서 작성

문서를 쓰거나 고칠 때는 **실제로 존재하는 것만 기술합니다.** 확인하지 못한 것은 `미확인`으로 남깁니다.

이 원칙을 포함한 문서 작성 규칙, 헤더 양식, 코드 출처 표기의 원본은 [`docs/README.md`](docs/README.md)입니다.
계획 문서의 상태 어휘·ID·양식은 [`docs/plan/process.md`](docs/plan/process.md)에 있습니다. 문서를 고치기 전에 둘 중 해당하는 쪽을 읽습니다.

---

## 3. 아트 & 디자인 스타일 가이드라인 (Art Direction)

본 프로젝트는 **스타일라이즈드 중세 판타지 서재/여관(Cozy Fantasy Hearth & Tabletop)** 룩앤필을 지향합니다.
상세한 아트 스펙은 [`docs/reference/art_style_guide.md`](docs/reference/art_style_guide.md)를 참조하십시오.

### 핵심 비주얼 규칙 요약:
1. **분위기 & 라이팅 (Atmosphere & Lighting)**:
   - **Key Light**: 벽난로/촛불의 따뜻한 골든 앰버 (`#ff9e3b`, 2800K~3000K, 강도 1.4~1.6)
   - **Rim / Fill Light**: 창가 달빛의 차가운 쿨 인디고 (`#364b6e`, 강도 0.35~0.5) 보색 대비
   - **Background**: 차분한 딥 챠콜 (`#0f0c10`)

2. **테이블 (Tabletop Wood)**:
   - 따뜻한 웜 허니 브라운 / 토피 월넛 톤 (`#6e432a` ~ `#825033`)
   - 둥글고 두툼한 챔퍼(Chunky Beveled Edges)의 원목 판자
   - 자잘한 실사 노이즈 없는 부드러운 핸드페인티드 스타일라이즈드 나뭇결

3. **러너 (Runner & Trims)**:
   - 묵직하고 따뜻한 딥 크림슨 / 테라코타 버건디 패브릭 (`#882d22`)
   - 앤틱 골든 옐로우 (`#e5a93c`) 톤의 기하학적 켈틱/노르딕 놋워크 패턴 트림
   - 실사 주름 및 노멀 왜곡 배제, 단정하고 깔끔한 머티리얼 구성
