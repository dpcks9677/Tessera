---
name: tessera-balance-check
description: Tessera 증강의 밸런스를 augment_balance_analysis.md 의 GEV 기준으로 점검하고, 기록된 수치가 현재 코드와 맞는지 확인해 docs/qa/balance/ 에 판정을 남긴다. 수치나 코드를 고치지 않는다.
argument-hint: <증강 ID[,ID...]> | <분류: 변형|주사위|퀘스트|수동|배율> | all
model: opus
effort: high
---

# 증강 밸런스 점검

인자: `$ARGUMENTS`

이 스킬은 **기존 기준으로 재고 판정합니다.** 기준 문서와 코드는 고치지 않습니다. 새 수치 계산이 필요하면 그 사실을 보고하고 멈춥니다.

## 원본

- 평가 기준·등급 구간·모델 한계: `docs/reference/augment_balance_analysis.md` §2.4, §3.2. 목록·진단 표는 §5(변형, 계산값), §6(그 밖, 추정값), 재도전 옵션은 §8.
- 증강 효과 설명: `docs/reference/augments_specification_and_status.md`
- 판정 어휘와 출력 형식: `.claude/gates/README.md`

두 문서 모두 크고, 명세 문서는 40KB를 넘어 통째로 읽으면 읽기 가드에 막힙니다. `python .claude/scripts/tessera_query.py sections <경로>` 로 목차를 보고 필요한 절만 `Read` offset/limit으로 읽습니다. 증강 ID 행은 `Grep` 으로 찾습니다.

## 1. 대상 확정

- 증강 ID: 그대로 씁니다. 분석 문서에 없는 ID면 그 ID는 `NOT ASSESSED — 기준 문서에 없음` 입니다.
- 분류: 분석 문서 §2.2 표의 분류에 맞춰 §5 또는 §6의 해당 표 전체가 대상입니다.
- `all`: §5·§6 전체입니다. 대상이 많으면 진단 표 대조를 `tessera-reader` 에 맡기고 행 요약만 받습니다.

## 2. 코드 대조

분석 문서 머리의 「코드 대조 기준」 커밋 이후 증강 코드가 바뀌었는지 봅니다.

```
git log --oneline <기준 커밋>..HEAD -- Assets/Scripts/Games/AugmentedYacht/Logic
git status --porcelain -- Assets/Scripts/Games/AugmentedYacht/Logic
```

- 변경이 없으면 기록된 수치를 그대로 씁니다.
- 변경이 있으면 `tessera-scout` 에 대상 증강 ID 목록과 분석 문서에 적힌 효과(점수, 조건, 사용 한도, 배율)를 넘겨, 현재 코드 값이 같은지 증강별로 확인받습니다. 값이 달라진 증강은 기록된 GEV가 무효이므로 `NOT ASSESSED — 코드 값 변경, 재계산 필요` 입니다.

## 3. 판정

증강마다 기록된 GEV의 등급(§2.4)으로 판정합니다.

| 등급 | 판정 |
|---|---|
| `B` | `PASS` |
| `C`, `A` | `CONCERNS` |
| `D`, `F`, `S` | `FAIL` |

- §6의 추정값으로 판정했으면 근거 칸에 「추정」을 붙입니다.
- §3.2 한계 5(`duel`, `copycat` 같은 2인 상호작용)에 해당하면 `NOT ASSESSED — 모델 밖` 입니다.
- 변형 증강은 §8의 재도전 옵션 때문에 획득 시점에 따라 등급이 뒤집힐 수 있습니다. §8에 그 증강의 값이 있으면 근거 칸에 함께 적습니다.
- 한계 2·3(기회비용, 회피 선택지)이 그 증강에 걸리면 근거 칸에 어느 쪽으로 치우친 값인지 적습니다.
- 전체 판정은 가장 엄한 판정입니다(`.claude/gates/README.md` 병합 규칙).

## 4. 기록

`docs/qa/balance/YYYY-MM-DD-<대상>.md` 를 만듭니다. 대상 이름은 증강 ID 하나면 그 ID, 여럿이면 분류 이름이나 `all`.

```markdown
# 밸런스 점검 YYYY-MM-DD — <대상>

| 항목 | 값 |
|---|---|
| 기준 문서 | `docs/reference/augment_balance_analysis.md` (코드 대조 기준 `<커밋>`) |
| 점검 시점 | `<HEAD 짧은 해시>` (미커밋 증강 코드 변경 있음/없음) |
| 전체 판정 | `PASS` / `CONCERNS` / `NOT ASSESSED` / `FAIL` |

## 코드 대조
<변경 없음, 또는 증강별 문서 값과 현재 값>
## 진단
| 증강 ID | 분류 | GEV | 등급 | 판정 | 근거 |
|---|---|---|---|---|---|
## 조정 후보
<FAIL·CONCERNS 증강마다 분석 문서 §7의 방향을 인용. 새 수치는 제안하지 않고, 재계산이 필요하면 그렇게 적음>
## 평가하지 못한 항목
```

## 5. 보고

`.claude/gates/README.md` 출력 형식(게이트 이름은 `BALANCE`)으로 판정을 보이고 파일 경로를 알립니다. 코드 값이 바뀐 증강이 있으면 분석 문서 갱신(재계산 포함)이 별도 작업으로 필요하다고 제안합니다. 직접 고치지 않습니다.

**커밋하지 않습니다.**
