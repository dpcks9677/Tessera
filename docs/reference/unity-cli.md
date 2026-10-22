# Unity CLI 사용 요령

Unity 공식 CLI(`unity`)와 Unity Pipeline 패키지(`com.unity.pipeline`)로 Editor를 다루는 방법과 함정을 모은 원본입니다. 어느 작업을 CLI로 하고 어느 작업을 unity-skills REST로 하는지는 `CLAUDE.md` 「Unity Editor 자동화」의 역할 분담을 따릅니다.

## 도입 배경

2026-10-22 에 같은 과제 3개(EditMode 테스트 실행, 씬 조회, 프리팹 생성·수정·정리)를 두 경로로 비교했습니다.

| 경로 | 토큰 | 도구 호출 | Editor 호출 | 소요 |
|---|---|---|---|---|
| unity-skills REST (`tessera-unity-operator`) | 56,769 | 14 | HTTP 약 21 | 60초 |
| CLI, 요령 없이 | 69,402 | 14 | 11 (탐색 7) | 101초 |
| CLI, 이 문서의 요령 제공 | 59,338 | 4 | 3 | 36초 |

CLI 쪽은 범용 에이전트로 돌려 기본 오버헤드가 더 컸습니다. 작업분만 보면 CLI가 호출 수와 시간 모두 적습니다. 다만 `script eval` 로 바꾼 씬은 dirty 표시가 남지 않아 저장 누락 위험이 있어, 씬·프리팹 수정은 REST에 남겼습니다.

## 버전

- CLI `1.0.0-beta.8`, 패키지 `com.unity.pipeline` `0.9.0-exp.1`. 둘 다 실험 단계라 명령 형태가 바뀔 수 있습니다. 아래 명령이 실패하면 버전부터 확인합니다.
- Editor 6.0 LTS 이상이 필요합니다. 이 프로젝트는 충족합니다.

## 기본 사용

- **항상 프로젝트 폴더에서 실행합니다.** 다른 폴더에서 부르면 `No Unity Editor instances found` 로 실패합니다. `cd "C:/Users/dpcks/문서/Tessera" && unity ...` 형태로 씁니다.
- 연결 확인은 `unity status --format tsv --no-banner` 입니다. Tessera 행의 상태가 `ready` 면 됩니다. 포트는 직접 지정하지 않습니다.
- 모든 호출에 `--format json --no-banner` 를 붙입니다. 응답은 `{success, data, errors, warnings}` 봉투이고, 실패해도 stdout 에 완전한 결과가 나오므로 `errors[0].code` 로 분기합니다.
- 도구 목록은 `unity list --format tsv --no-banner` 입니다(이름·그룹). 호출은 `unity command <그룹> <이름> --<인자> <값>` 입니다.

## 함정

**도구별 `--help` 가 동작하지 않습니다.** `unity command <그룹> <이름> --help` 는 `unity command` 의 일반 도움말만 출력합니다. 매개변수는 `unity command --query "<설명 문구>" --detail full --format json` 으로 봅니다. 도구 이름으로 검색하면 비어 나올 때가 있어 설명 문구로 찾습니다. `--detail compact` 는 매개변수를 빼고 보여 줍니다.

**`script eval` 의 코드는 메서드 본문입니다.** `using` 지시문을 쓰면 `CS1001` 로 실패하므로 `UnityEngine.GameObject`, `UnityEditor.PrefabUtility` 처럼 전체 이름을 씁니다. 결과는 `return <문자열>;` 로 돌려줍니다. 여러 단계를 한 번에 묶을 수 있습니다.

```bash
unity command script eval --code 'var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene(); return s.name + " " + s.rootCount;' --format json --no-banner
```

**`eval` 응답은 보낸 코드를 다시 싣습니다.** `parameters` 와 진단 메시지에 소스 전문이 반복되므로 `| grep -v '"source"'` 등으로 줄여서 읽습니다.

**`eval` 로 바꾼 씬은 dirty 로 표시되지 않습니다.** Undo 기록도 남지 않습니다. 이대로 씬을 바꾸면 Editor가 저장 여부를 묻지 않아 변경이 사라질 수 있습니다. 그래서 씬·프리팹·에셋 수정에는 `eval` 을 쓰지 않습니다.

**Editor가 백그라운드에 있으면 매니페스트 변경을 바로 반영하지 않습니다.** 패키지를 추가한 뒤 `unity status` 에 프로젝트가 안 나오면 REST `package_refresh` 를 한 번 부르거나 Editor 창에 포커스를 줍니다.

## 테스트 실행

```bash
unity command test run --mode editor --filter Tessera.Editor.Tests --format json --no-banner
```

동기 실행이라 결과 요약과 테스트별 결과가 한 번에 돌아옵니다. 잡 폴링이 필요 없습니다. 2026-10-22 네임스페이스 필터 전체 실행은 370건, 벽시계 8초였습니다(REST 경로 약 170초).

**출력이 큽니다.** 370건 기준 약 97KB 입니다. 표준 출력으로 받지 말고 임시 파일로 리다이렉트한 뒤 요약만 읽습니다. 구조는 `data.result.{Summary{Total,Passed,Failed,Skipped,Inconclusive}, Results[{FullName,Status,Duration,Message,StackTrace}], Duration, Mode, FilterApplied}` 이고, 실패 건은 `Results` 에서 `Status` 가 `Passed` 가 아닌 것을 거릅니다. 필터 규칙과 판정 기준은 `tessera-verifier` 정의를 따릅니다. 그 규칙은 REST 경로에서 확인한 것이라, CLI에서 개수가 기대와 다르면 REST 경로로 한 번 대조합니다.
