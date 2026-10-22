# 오케스트레이션 근거

> **문서 종류**: 작업 규칙 (AI 대상)
> **코드 대조 기준**: 2026-10-22 · 커밋 `d4996d4`
> 이 문서는 위 시점의 코드에서 확인된 것만 기술합니다. 계획 항목은 상태 표기로 구분합니다.

`CLAUDE.md` 「오케스트레이션」과 「Unity Editor 자동화」 규칙의 근거와 사례를 모은 문서입니다. 규칙 자체는 `CLAUDE.md` 가 원본이고, 여기에는 왜 그렇게 정했는지만 둡니다.

## 역할별 재사용 방침의 근거

`CLAUDE.md` 「역할별 재사용 방침」 표의 방침마다 근거는 다음과 같습니다.

| 에이전트 | 근거 |
|---|---|
| `tessera-unity-operator` | REST 포트·엔드포인트 함정·씬 계층 경로가 누적됨. Unity 단일 인스턴스라 어차피 직렬이어서 잃을 병렬성이 없음 |
| `tessera-verifier` | 포트 탐색과 "어떤 실패가 패키지 소속 잡음인지"의 판정 기준이 누적됨 |
| `tessera-implementer` | 같은 파일군을 계속 다루는 동안은 맥락이 값이 되고, 범위가 바뀌면 부담이 됨 |
| `tessera-scout` · `tessera-reader` · `tessera-reviewer` | 한 번 결론 낸 감사자는 그 결론을 재주장하는 쪽으로 기울어 독립 판정이라는 존재 이유가 깎임. 쌓이는 컨텍스트가 읽은 파일 본문이라 다음 작업에 재사용 가치가 없음 |
| `tessera-scribe` · `tessera-committer` | 상태가 없음 |

살아 있는 에이전트를 먼저 이어 쓰는 규칙의 사례: 실제로 한 세션에서 `tessera-unity-operator` 를 네 번 따로 띄워 약 255k 토큰을 썼고, 네 번 모두 REST 포트 탐색·모듈 문서 재독·`prefab_set_property` 우회로를 되풀이했습니다.

## 토큰 예산의 근거

주 세션은 매 턴 컨텍스트 전체를 다시 읽습니다. 사용량은 **턴 수 × 컨텍스트 크기**입니다. 2026-09 초 로그에서 주 세션이 전체의 83%를 썼습니다. 세션당 약 400턴, 평균 컨텍스트 250k였고 압축은 한 번도 없었습니다.

- `CLAUDE_CODE_AUTO_COMPACT_WINDOW=200000` 이 필요한 이유: 1M 창에서는 자동 압축이 사실상 일어나지 않습니다.
- 내장 `Explore`·`Plan` 대신 `tessera-scout` 를 쓰는 이유: 여러 Explore가 같은 파일을 각자 통째로 읽은 사례가 있습니다.

### 주 세션 가드 훅의 동작 범위

`.claude/hooks/main-session-guard.py` 가 주 세션의 Unity `curl`·셸 탐색 호출에 `CLAUDE.md` 「토큰 예산」 규칙을 상기시킵니다. 차단하지는 않으며, 서브에이전트(`agent_id` 가 있는 호출)는 건너뜁니다. 소스 트리 탐색만 대상이라 다른 명령의 출력을 `head`/`tail` 로 자르는 경우와 `docs/`·`.claude/`·`*.md` 를 읽는 경우는 조용히 지나갑니다.

## 한국어 출력 고정의 근거

`CLAUDE.md` §0 의 출력 언어 규칙은 전역 `unity-skills` 스킬 문서 다수가 간체 중국어라는 데서 나왔습니다. 모듈 문서를 읽으면 중국어 지시문이 그대로 컨텍스트에 들어오므로, 필요한 모듈 하나만 읽고 알려진 REST 엔드포인트를 직접 호출해 중국어 유입량과 토큰을 함께 줄입니다.

## graphify 운영 근거

`CLAUDE.md` 「파일 탐색 시 토큰 절약」 절 규칙의 근거입니다.

- **라벨 파일을 추적하는 이유**: `graphify-out/.graphify_labels.json` 과 같은 이름의 `.sig` 는 git 이 추적합니다. 노드·엣지·커뮤니티 분할은 AST에서 결정돼 같은 커밋이면 어디서든 똑같이 재생성되지만, 커뮤니티 이름은 LLM으로만 만들 수 있어 재생성이 불가능합니다. 이 두 파일 덕분에 새 환경에서 `graphify update .` 만 돌려도 같은 이름이 붙습니다.
- **`PYTHONHASHSEED=0` 이 필요한 이유**: 커뮤니티 검출이 문자열 키 집합의 순회 순서에 의존하는데 그 순서가 프로세스마다 무작위라, 시드를 고정하지 않으면 같은 코드에서도 커뮤니티 경계가 흔들립니다. 그러면 저장된 이름이 대량으로 무효화돼 라벨 파일이 매번 수백 줄씩 바뀌고 기기 간 충돌이 납니다. `post-commit` 훅과 `.claude/settings.json` 이 이 값을 고정하므로 이 저장소에서 작업할 때는 신경 쓸 필요가 없지만, 다른 터미널에서 직접 실행할 때는 `PYTHONHASHSEED=0 graphify update .` 로 돌립니다.

## `unity-agent-plugin` 구성

Unity Technologies 공식 플러그인 `unity@unity-agent-plugin` 이 설치돼 있습니다(사용자 범위, 스킬 31개, `unity:` 접두사). 명령어·에이전트·훅·MCP 서버는 없고 스킬만 제공합니다.

Editor 통로를 작업 종류로 나눈 것은 2026-10-22 실측 비교로 정했습니다. 비교 결과와 `script eval` 이 dirty 표시·Undo를 남기지 않는 사유는 `docs/reference/unity-cli.md` 에 있습니다.

### 이 프로젝트에서 값이 큰 `unity:` 스킬

- `unity:optimize-text-mesh-pro` (TMP 동적 아틀라스·한글 폴백)
- `unity:urp-postprocessing` 과 `unity:validate-urp-render-graph-renderer-feature` (픽셀 엣지 Renderer Feature)
- `unity:optimize-audio` (`Assets/05.Audio` 임포트 설정)
- `unity:physics-3d-collision` (주사위 프리셋 베이킹)
