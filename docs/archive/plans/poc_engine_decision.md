# Engine Decision

> **문서 종류**: 시점 기록 (보관)
> **작성 시점**: 2026-08-17 — 본문은 이 시점의 코드 상태를 기술하며, 이후 변경을 반영하지 않습니다.
> **보관 사유**: 엔진 채택 판단서입니다. 조건부 판단이 이미 해소되어 보관합니다. 원래 `Assets/Docs/Decision.md`였습니다.
>
> 여기 적힌 파일 경로와 클래스명 일부는 현재 존재하지 않을 수 있습니다. 이 문서를 실행 지침으로 쓰지 마십시오. 보관 색인은 [`archive/README.md`](../README.md)입니다.

Status: Conditional Go

URP supports required low-resolution 3D render, Point upscale, hard-edged shadows, palette reduction, and real-time Rigidbody dice. Final decision still needs standalone 60-second profiling and side-by-side capture review on target hardware.

Known risk: physics-driven rotation can cause pixel crawl at 320x180. Compare 426x240 and, if needed, a stepped visual rotation experiment before production integration.
