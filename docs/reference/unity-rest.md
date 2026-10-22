# Unity REST 공통 요령

> **문서 종류**: 작업 규칙 (AI 대상)
> **코드 대조 기준**: 2026-10-22 · 커밋 `d4996d4`
> 이 문서는 위 시점의 코드에서 확인된 것만 기술합니다. 계획 항목은 상태 표기로 구분합니다.

unity-skills REST 서버를 부를 때 공통으로 쓰는 요령과 함정의 원본입니다. `tessera-verifier`, `tessera-unity-operator`, `/tessera-unity` 가 이 문서를 참조합니다. CLI 쪽 요령은 `docs/reference/unity-cli.md` 에 있고, 스크린샷을 언제 찍는가의 정책은 `CLAUDE.md` §0.1 「검증 스크린샷」에 있습니다.

## 서버 탐색

포트를 하드코딩하지 마십시오. 이전 Unity 인스턴스의 죽은 리스너 때문에 포트가 8090에서 8091, 8092로 옮겨 다닌 전례가 있습니다. 8090~8100을 훑어 `projectName`이 `Tessera`인 응답을 찾습니다. 세션의 첫 호출이 서버 탐색입니다.

```bash
for p in $(seq 8090 8100); do
  r=$(curl -s -m 2 "http://127.0.0.1:$p/health")
  case "$r" in *'"projectName":"Tessera"'*) echo "$p"; break;; esac
done
```

`/health` 응답에서 확인할 것:
- `projectName` 이 `Tessera` 이고 `instanceId` 가 다른 Unity 프로젝트의 것이 아닌지. 이웃 포트를 다른 Unity 프로젝트가 잡고 있을 수 있습니다.
- `currentMode`. `bypass` 가 아니면 `approvalBehavior: forbid` 인 스킬(`editor_play_capture`, `test_run` 등)이 `MODE_FORBIDDEN` 으로 막힙니다. 이런 스킬은 승인 요청 흐름도 적용되지 않습니다.
- `surfaceProfile` 이 `full` 인지.

서버가 응답하지 않으면 Unity Editor가 닫혀 있는 것입니다. 다만 서버가 사라졌다고 크래시로 단정하지 마십시오. unity-skills REST 서버는 Unity 재시작 시 꺼지고, 사용자가 Unity Skills 창에서 수동으로 켜야 다시 뜹니다. 재시작하면 포트도 바뀝니다(2026-09-20 에 8091 → 8090). 포트를 다시 훑고, 그래도 없으면 사용자에게 서버 재기동을 요청하십시오.

## 호출 형태

```bash
POST http://127.0.0.1:<port>/skill/<skill_name>              # 실행
POST http://127.0.0.1:<port>/skill/<skill_name>?mode=dryRun  # 파라미터만 검증
POST http://127.0.0.1:<port>/skills/batch                    # 최대 50단계
GET  http://127.0.0.1:<port>/skills/recommend?intent=...&topN=N&includeSchema=true&wire=v2
GET  http://127.0.0.1:<port>/jobs/{id}                       # 비동기 잡 폴링
```

스키마 조회 엔드포인트에는 `curl --compressed` 를 쓰십시오. 여러 단계를 연달아 할 때는 개별 호출을 반복하지 말고 `batch` 를 쓰십시오.

GameObject 생성·삭제, 컴포넌트 제거, 머티리얼 교체, 트랜스폼 일괄 변경처럼 되돌리기 어려운 조작은 `?mode=dryRun` 으로 파라미터를 먼저 검증한 뒤 실행하십시오.

## `job_wait` 금지

**`job_wait` 을 쓰지 마십시오.** compile/package/test/playmode/play_capture/build_player 잡은 메인 스레드에서 진행되는데 `job_wait` 이 그 스레드를 막아, `waitNotSupported: true` 를 돌려주며 타임아웃만 소모합니다. `GET /jobs/{id}` 또는 `job_status` 로 폴링하십시오. 테스트 잡은 `test_get_result` 로도 폴링합니다.

## 스크린샷

언제 찍는가는 `CLAUDE.md` §0.1 「검증 스크린샷」을 따릅니다. 아래는 찍기로 정했을 때의 기술 규칙입니다.

- `scene_screenshot` 은 Game View 최종 합성을 찍습니다.
- 해상도는 `960x540` 을 기본으로 씁니다. 이미지 토큰은 넓이에 비례하며 `1920x1080` 은 약 2.7배 비쌉니다. 픽셀 판독성이나 폰트 글리프처럼 해상도 자체가 판정 대상일 때만 올립니다.
- `returnImage` 는 쓰지 않고 저장된 PNG 경로를 읽습니다. 로컬 Unity 환경에서는 그쪽이 쌉니다.
- `filename` 은 경로 구분자 없는 순수 파일명이어야 하며 `Assets/Screenshots/` 에 저장됩니다.
- 비동기라 약 1프레임 뒤에 파일이 생깁니다. 읽기에 실패하면 200ms 후 재시도합니다.

```bash
curl -s -m 120 -X POST http://127.0.0.1:<port>/skill/scene_screenshot \
  -H 'Content-Type: application/json' \
  -d '{"filename":"verify.png","width":960,"height":540}'
```

**함정: Game View 창이 렌더링되지 않은 상태면 `scene_screenshot` 이 `success: true` 를 돌려주면서 파일을 쓰지 않습니다.** 메뉴로 Game View를 강제로 열면 파일은 생기지만 변경 전의 오래된 캐시 프레임이 찍힐 수 있습니다. 이때는 `camera_screenshot` 으로 `Full Field World Camera` 를 직접 렌더해 대체하십시오. `PixelEdgeCamera` 가 붙은 월드 카메라라 픽셀 필터는 반영되지만 `Crisp UI Camera` 오버레이(HUD)는 빠집니다. 대체했으면 보고에 그 사실을 적으십시오.

## Play Mode 진입

- `editor_play_capture` 와 `editor_play` 는 Play Mode에 진입·이탈하면서 **저장 안 된 씬 변경을 폐기합니다.** 씬을 편집한 직후, 저장 전에는 호출하지 않습니다.
- 둘 다 `approvalBehavior: forbid` 라 bypass 모드에서만 동작합니다.
- `editor_play` 는 도메인 리로드를 일으켜 서버가 15~20초 응답하지 않습니다. 그동안 걸린 요청은 타임아웃되므로 포트를 다시 탐색하며 재시도하십시오.
