# 증강 획득 단계 분류

> **문서 종류**: 설계 결정 상세 (AI 대상)
> **코드 대조 기준**: 2026-10-25 · 커밋 `a1cb2a6`
> **결정**: `D-057`(단계 도입), `D-058`(이 분류)

15턴 규칙(`D-057`)에서 증강마다 등장하는 드래프트 단계를 정한 표입니다. 게임 구현의 원본이며, BAL 도구는 이 단계의 남은 턴으로 증강 가치를 잽니다.

## 단계

| 단계 | 드래프트 라운드 | 남은 턴 | 기준 |
|---|---|---|---|
| Opening | 4 | 12 | 주사위를 바꾸지 않는 지속형, 여러 턴이 필요한 퀘스트 |
| Middlegame | 9 | 7 | 주사위를 바꾸는 강화, 중간 길이 퀘스트 |
| Endgame | 12 | 4 | 한 번에 끝나는 즉시형(변형, 일회성 수동 증강) |
| Timeless | 4·9·12 모두 | 12·7·4 | 획득 시점을 플레이어 판단에 맡기는 증강 |

- 증강마다 단계는 하나입니다.
- 드래프트 후보 풀은 그 단계의 증강과 Timeless 증강을 합친 것입니다.
- 퀘스트는 그 단계의 남은 턴 안에 달성할 수 있어야 합니다.
- 단계마다 증강을 한 장씩 얻으므로, 주사위를 바꾸는 증강은 `random-box`를 거치지 않는 한 플레이어당 최대 하나입니다.

## 분류

DONE 46종을 나눕니다. HOLD 3종(`four-by-four`, `two-households`, `strange-die`)은 다시 살릴 때 정합니다.

### Opening (7종)

| 증강 | 종류 | 근거 |
|---|---|---|
| `fast-straight` | 퀘스트 | 스몰·라지 스트레이트 두 칸을 Middlegame 전(4~8번째 턴, 5턴) 안에 기입해야 합니다 |
| `every-little` | 퀘스트 | 득점한 1을 7개 모으려면 여러 칸이 필요합니다 |
| `copycat` | 퀘스트 | 상대 족보를 세 번 따라 해야 합니다 |
| `bounty-hunter` | 퀘스트 | 무작위 대상 칸 세 개를 기다려야 합니다 |
| `piggy-bank` | 강화 | 12원마다 반복 지급해(`PiggyBank.cs:40`) 남은 턴이 많을수록 가치가 크고, 주사위를 바꾸지 않습니다 |
| `momentum` | 강화 | 주사위를 바꾸지 않는 1회 발동형입니다. Endgame에 두면 같은 단계의 `double-down`과 함께 가질 수 없어 2배 겹침이 사라집니다 |
| `step-by-step` | 퀘스트 | 리워크 예정이라 분류를 보류하고 임시로 둡니다. 4라운드 획득 시 Numbers 칸이 이미 찼을 수 있는 문제도 리워크 때 다룹니다 |

### Middlegame (13종)

| 증강 | 종류 | 근거 |
|---|---|---|
| `weighted-dice`, `golden-die`, `8-sided`, `promotion-die`, `couple-dice`, `sevens-dice` | 강화 | 주사위를 바꿉니다. 일찍 얻을수록 효과가 몰리므로 남은 7턴 단계에만 둡니다(`D-057`). `promotion-die`는 7턴 동안 6에 한 번 도달합니다 |
| `yacht-bank` | 강화 | 저축 3턴과 수령 1턴으로 길이가 고정돼 게임 길이와 무관합니다. 후보가 적은 단계에 둡니다 |
| `no-time-to-waste` | 퀘스트 | 연속 3턴 무리롤 |
| `holdout` | 퀘스트 | 풀하우스를 Endgame(12번째 턴 이후)에 기입해야 합니다 |
| `cautious-straight` | 퀘스트 | 스몰 뒤 라지, 최소 2턴 |
| `doubling` | 퀘스트 | 같은 점수 두 번, 최소 2턴 |
| `nozdormu` | 퀘스트 | 획득 턴부터 4턴 |
| `prophet` | 퀘스트 | 3턴 창 |

### Endgame (21종)

| 증강 | 종류 | 근거 |
|---|---|---|
| `lucky-sevens`, `perfect-squares`, `gambler`, `three-of-a-kind`, `tiny-house`, `two-pair`, `head-and-tail`, `evens`, `odds`, `double-large-straight`, `prime-collection`, `duplex-house`, `mountain`, `high-dice` | 변형 | 즉시형입니다. 점수가 낮은 칸을 살리거나 이미 찬 칸을 덮어써 추가 턴을 받는 쓰임새가 마지막 단계와 맞습니다 |
| `table-flip`, `equivalent-exchange`, `coin-toss`, `gambit`, `double-down`, `dice-alchemy` | 수동 | 일회성입니다 |
| `duel` | 강화 | 획득 즉시 판정합니다 |

### Timeless (5종)

| 증강 | 종류 | 근거 |
|---|---|---|
| `2nd-choice`, `fibonacci`, `reverse-choice`, `blackjack-21` | 변형(Yacht 칸) | 일찍 집을 수도 있지만 전략적으로 불리한 수입니다. 획득 시점을 플레이어 판단에 맡깁니다. 현행 1라운드 전용(`PhaseOneOnly`)을 대체합니다 |
| `random-box` | 강화 | 단계 규격을 깨는 대신 무작위성을 위험으로 집니다. 교체 증강은 단계와 무관하게 뽑습니다(아래) |

## 증강 규칙 변경

| 증강 | 현행 | 변경 |
|---|---|---|
| `fast-straight` | 8번째 턴까지 두 칸 기입(`DeadlineTurn = 8`, `FastStraight.cs:37`) | Middlegame 드래프트(9라운드) 전, 즉 8번째 턴까지. 값은 현행과 같고 의미가 「Middlegame 전」으로 바뀝니다. 드래프트 라운드가 바뀌어도 어긋나지 않도록 마감을 Middlegame 드래프트 라운드에서 끌어내는 편이 낫습니다 |
| `holdout` | 9번째 턴 이후 풀하우스(`MinTurn = 9`, `Holdout.cs:29`) | 12번째 턴 이후(Endgame) 풀하우스 |
| `nozdormu` | 목표 턴을 획득 라운드로 정함. 6 미만이면 5, 9 미만이면 8, 나머지 12(`Nozdormu.cs:55`) | 획득 턴부터 4턴. 9라운드 획득이면 12번째 턴까지 |
| `double-down` | 9번째 턴부터 사용(`MinTurnsTaken = 8`, `DoubleDown.cs:32`) | 턴 제한 삭제. Endgame에서 얻으므로 조건이 항상 참입니다 |
| `random-box` | 교체 풀은 지금 얻을 수 있는 퀘스트가 아닌 증강(`ResolveRandomBoxes`, `YachtAugmentRuntime.cs:1050`) | 단계 조건만 빼고 퀘스트가 아닌 모든 증강. 보유·충돌·같은 대상 칸·주사위 슬롯 조건은 유지합니다 |
| `step-by-step` | 1라운드 전용 | 리워크 때 정합니다 |

`nozdormu`는 목표 턴까지 버티기만 하면 보상을 받습니다. 실패 판정 코드가 없고 설명문(`Nozdormu.cs:39`)과 어긋납니다. 이번 변경에서 다루지 않고 기록만 합니다.

## Hills와 증강

- 배수 증강(`momentum`, `double-down`)은 Hills 기입 값에 적용합니다(`D-057`).
- `golden-die`의 +2와 `couple-dice`의 +3은 Hills 기입 값에 더합니다.
- `yacht-bank`가 점수에서 빼는 주사위는 Hills 기입 값에서도 뺍니다.
- 주사위 눈을 바꾸는 증강은 바뀐 눈 그대로, 주사위 개수를 바꾸는 증강(`gambit`)은 그 수만큼 모두 더합니다(`D-057`).
- 변형 증강은 Hills를 대상으로 삼지 않습니다(`D-057`).

## 드래프트 규칙

- 후보 풀: 그 단계의 증강과 Timeless 증강. 단계는 `PhaseOneOnly`를 대체합니다.
- 대상 칸이 비었는지로 변형 후보를 거르지 않습니다. 변형은 이미 찬 칸을 덮어써 점수를 지우고 추가 턴을 받는 용도로도 쓰입니다.
- 그 밖의 `CanAcquire` 규칙(보유, 같은 대상 칸 변형 중복, `8-sided`·`table-flip` 상호 배타, 주사위 슬롯 합)은 유지합니다.

## 단계별 수

| 단계 | 수 | 후보 풀(단계 + Timeless) | 메모 |
|---|---|---|---|
| Opening | 7 | 12 | 퀘스트가 아닌 지속형이 `piggy-bank`, `momentum` 둘뿐입니다. 새 증강 기획에서 먼저 채웁니다 |
| Middlegame | 13 | 18 | |
| Endgame | 21 | 26 | 변형이 14종입니다 |
| Timeless | 5 | — | 모든 드래프트 후보 풀에 들어갑니다 |

미확정: Opening 후보 풀 12종 가운데 Timeless가 5종(약 42%)이라, 4라운드 후보 3장에 Yacht 변형이 자주 섞입니다. 비중 조정이 필요한지는 BAL 측정과 새 증강 기획 뒤에 봅니다.
