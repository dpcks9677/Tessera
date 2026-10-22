---
name: tessera-gate
description: 마일스톤이나 트랙이 끝났을 때 MILESTONE 게이트로 종료 판정을 내리고, PASS이면 커밋 허가 요청으로 이어간다. 커밋은 사용자 허가 뒤 tessera-committer 가 실행한다.
argument-hint: <Mnn | 트랙명> [--review solo|lean|full] [--check]
model: opus
effort: high
---

# 마일스톤 게이트

인자: `$ARGUMENTS`

당신은 오케스트레이터입니다. **판정은 주 세션이 직접 내립니다.** 하위 게이트 리뷰는 `tessera-reviewer`, 문서 갱신은 `tessera-scribe`, 커밋은 `tessera-committer` 에 맡깁니다.

## 원본

- 점검 항목과 판정 기준: `.claude/gates/MILESTONE.md`. 판정 어휘·출력 형식·기록 형식은 `.claude/gates/README.md`
- 마일스톤 종료·보관 순서: `docs/plan/process.md` §8
- 커밋 허가 요청 형식과 메시지 규칙: `CLAUDE.md` 「커밋 & 푸시」

## 인자

- 대상: `Mnn` 또는 트랙명(`orch`, `solid`, `improvement`). 비어 있으면 `docs/STATUS.md` 의 현재 마일스톤을 대상으로 삼고 그 사실을 알립니다.
- `--review solo|lean|full`: 이번 호출의 `review_mode`. 없으면 `.claude/gates/README.md` 의 기본값.
- `--check`: 판정만 읽기 전용으로 냅니다. 파일을 바꾸지 않고, PASS여도 커밋 허가 요청 초안을 보인 뒤 멈춥니다.

## 1. 대상 판독

| 대상 | 태스크 표·완료 게이트 | 세션 로그 |
|---|---|---|
| 진행 중 마일스톤 | `docs/plan/mNN/README.md` | `docs/plan/mNN/log.md` |
| 트랙 | `docs/plan/tracks/<트랙>/README.md` | `docs/plan/tracks/<트랙>/log.md` |
| 보관된 마일스톤 | `docs/archive/milestones/completed.md` 의 `## Mnn.` 절 | `docs/archive/milestones/log.md` 의 해당 항목 |

보관된 마일스톤은 이미 커밋돼 있으므로 `--check` 로만 다룹니다. `--check` 없이 지목되면 그 사실을 알리고 `--check` 로 진행합니다.

`python .claude/scripts/tessera_query.py sections <파일>` 로 절의 시작 줄을 찾고 `Read` offset/limit 로 그 절만 읽습니다. 태스크 상태 집계는 `tasks --plan <폴더>` 로 합니다. 문서 전체를 읽지 않습니다. 대상이 길거나 로그 대조가 많으면 `tessera-reader` 에 태스크 상태·세션 로그 항목·`NOT ASSESSED` 기록 대조를 맡기고 요약만 받습니다.

## 2. 범위 확정

- 진행 중 마일스톤·트랙: `git status --short` 의 미커밋 변경. 대상과 무관해 보이는 파일은 따로 표시합니다(게이트 6항).
- 보관된 마일스톤: `git log --oneline --grep "<Mnn>-T"` 로 찾은 커밋 범위. 게이트 6항은 `해당 없음 — 이미 커밋됨` 입니다.
  - 커밋 `5375a2c`(2026-09-06) 이전 마일스톤은 소수점 번호(`M10.9` 등)로 커밋·기록돼 있습니다. 검색이 비면 보관 로그에서 같은 작업의 옛 ID를 찾아 다시 검색합니다.

## 3. 판정

`MILESTONE.md` 의 점검 항목을 하나씩 대조합니다.

- 4항(계획과 코드의 일치)에서 어긋남이 의심되면 판정을 멈추고 `/tessera-plan-sync <대상>` 을 권합니다. 직접 상태를 고치지 않습니다.
- 5항 하위 게이트는 `review_mode` 를 따릅니다. `lean`·`full` 이면 `tessera-reviewer` 두 개를 **병렬로** 띄워 `.claude/gates/CODE-REVIEW.md`, `.claude/gates/TEST-COVERAGE.md` 를 각각 넘깁니다. 프롬프트 형식은 `.claude/gates/README.md` 「실행 방법」입니다. 대상은 2단계에서 확정한 범위입니다. `solo` 면 생략하고 그 사실을 근거에 적습니다.
- 하위 게이트 판정은 `.claude/gates/README.md` 「판정 어휘」 절의 병합 규칙으로 합칩니다.

판정을 `.claude/gates/README.md` 「출력 형식」으로 사용자에게 보입니다.

## 4. 판정 뒤

- `FAIL`: 멈추고 고칠 항목을 보고합니다.
- `CONCERNS`: 항목을 보이고 수락 여부를 묻습니다. 수락하면 PASS와 같이 진행하고, 수락 사실은 기록에 남깁니다.
- `NOT ASSESSED`: 빠진 입력을 묻습니다. PASS로 취급하지 않습니다.
- `PASS`: 커밋 허가 요청으로 넘어갑니다.

### 커밋 허가 요청

`CLAUDE.md` 「커밋 & 푸시」가 요구하는 세 가지를 함께 보입니다.

1. 마일스톤·트랙과 범위
2. 변경 파일 목록(`git status --short`). 무관한 파일은 커밋 대상에서 빼고, 허가 뒤 1단계에서 고칠 `roadmap.md`·`log.md` 를 더한 목록으로
3. 커밋 메시지 초안. `git log --oneline -10` 의 패턴과 메시지 형식 규칙을 따르고, 본문에 태스크 ID를 남깁니다

현재 브랜치가 `main` 이면 브랜치를 먼저 만든다는 사실도 요청에 넣습니다.

`--check` 는 여기서 멈춥니다. 초안 앞에 「시험 — 실제 요청 아님」을 붙입니다.

### 허가 뒤

`process.md` §8 순서를 따릅니다.

1. 마일스톤이면 `tessera-scribe` 로 `roadmap.md` 상태를 `DONE` 으로 바꾸고, 판정과 수락한 `CONCERNS` 를 세션 로그에 남깁니다. 트랙이면 로그만 남깁니다.
2. `tessera-committer` 에 허가받은 파일 목록과 메시지를 넘깁니다. 푸시는 허가 범위에 푸시가 들어 있을 때만 맡깁니다.
3. 마일스톤 폴더 보관(§8 4항)은 커밋 뒤 사용자에게 따로 묻습니다. 커밋 허가에 보관이 포함됐다고 보지 않습니다.

허가 없이는 1~3을 하나도 하지 않습니다. 이전 허가를 이번 커밋에 넘겨 쓰지 않습니다.
