# 요트 게임 상태 소유권

> **문서 종류**: 시점 기록 (보관)
> **작성 시점**: 2026-08-25 — 본문은 이 시점의 코드 상태를 기술하며, 이후 변경을 반영하지 않습니다.
> **보관 사유**: `M2` 리팩토링 전후의 상태 소유권 대조표입니다. 같은 내용을 `docs/reference/architecture_decisions.md` ADR-001이 더 자세히 다루므로 보관합니다.
>
> 여기 적힌 파일 경로와 클래스명 일부는 현재 존재하지 않을 수 있습니다. 이 문서를 실행 지침으로 쓰지 마십시오. 보관 색인은 [`archive/README.md`](../README.md)입니다.

## 이전 상태

| 상태 | 이전 소유자 | 문제 |
|---|---|---|
| 턴/라운드/남은 굴림 | `YachtGameSession` | 주사위 상태와 분리됨 |
| 주사위 값/킵 | `AugmentedYachtController` | 프레젠테이션이 논리 상태를 직접 소유함 |
| 난수/프리셋/미러 | `AugmentedYachtController` | 결과와 연출 선택이 별도 난수에 의존함 |
| 점수 | `ParchmentScoreSheet.PlayerScoreData` | UI 오브젝트가 권위 데이터 저장소 역할을 겸함 |
| 증강 트레이 | `AugmentedYachtController` | 모드와 무관하게 그래픽을 생성함 |

## M2 이후 상태

| 상태 | 권위 소유자 | 프레젠테이션 역할 |
|---|---|---|
| 모드/단계/턴/라운드/revision | `LocalGameAuthority`의 `YachtGameState` | 읽어서 UI에 표시 |
| 주사위 ID/종류/값/킵/승급 | `YachtGameState.Dice` | Transform과 머티리얼에 반영 |
| 점수/후보 | `YachtGameState.Players`, `Candidates` | 점수표에 표시하고 명령만 전송 |
| 주사위 값/프리셋/미러 | `RollPresentation` 한 결과 | 결과를 변경하지 않고 재생 |
| 난수 | 주입된 `IRandomSource` | 사용하지 않음 |
| 모드별 규칙 | `IYachtRuleSet` | 생성되는 이벤트와 상태만 소비 |
| 드래프트 진행/선택 횟수 | `YachtGameState.Draft` | 제시 옵션과 현재 선택자를 표시하고 선택 명령만 전송 |
| 플레이어별 변형 증강/강화/퀘스트/사용 횟수/추가 턴 | `YachtGameState.AugmentPlayers` | 현재 열람 대상 플레이어의 보유 목록·족보 규칙·진행·액션 가능 여부를 표시 |
| 명시적 전역 증강 예약 상태 | `YachtGameState.GlobalAugmentIds` | 카드 설명이 양쪽 적용을 요구하는 향후 증강에만 사용하며 현재 변형 증강에는 사용하지 않음 |
| 증강 정의/훅/충돌/후보 필터 | `YachtAugmentRuntime` | 이벤트 메시지와 권위가 확정한 프리셋만 소비 |

`ParchmentScoreSheet`의 `PlayerScoreData` 인스턴스는 현재 씬 호환을 위해 권위 상태에 주입하지만, 수정은 권위 명령 처리기가 수행한다. 이후 상태 직렬화/재동기화 시에는 `YachtGameState` 스냅샷을 기준으로 화면을 다시 그린다.
