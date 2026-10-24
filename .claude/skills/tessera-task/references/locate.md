# 0단계 — 태스크 판독

ID 접두사로 계획서를 고릅니다.

- `Mnn-Tnn` → `docs/plan/mNN/README.md` 태스크 표 (완료 마일스톤은 `docs/archive/milestones/`)
- `SOLID-Tnn` → `docs/plan/tracks/solid/README.md`
- `ORCH-Tnn` → `docs/plan/tracks/orch/README.md`
- `LAYOUT-Tnn` → `docs/plan/tracks/layout/README.md`
- `KLSS-Tnn` → `docs/plan/tracks/klss/README.md`
- `BAL-Tnn` → `docs/plan/tracks/bal/README.md`
- `BUILD`·`TEST`·`AUG`·`PERF`·`ARCH`·`DOC`·`LOAD` → `docs/plan/tracks/improvement/README.md`

`python .claude/scripts/tessera_query.py task <ID>` 로 행(ID·상태·작업·`파일:줄`·문서 칸)을 얻고, 상세 섹션은 `sections <파일>` 목차에서 줄 번호를 골라 그 구간만 읽습니다. 계획서 전체를 읽지 마십시오. 태스크 표의 「문서」 칸에 태스크 문서(예: `docs/plan/m17/T25-bank-keep-zone.md`)가 걸려 있으면 그것도 확인합니다. 같은 README의 완료 게이트도 읽어 둡니다. 5단계에서 씁니다.

상태가 이미 `DONE` 이면 중단하고 사용자에게 알립니다(`--check` 는 예외). `DEFERRED` 면 왜 미뤄졌는지 확인하고 진행 여부를 묻습니다.

인자가 비어 있으면 `docs/STATUS.md`의 "진행 중 태스크"를 대상으로 삼고, 그 사실을 사용자에게 알린 뒤 진행합니다. 진행 중 태스크가 여럿이면 고르지 말고 어느 것인지 사용자에게 묻습니다.
