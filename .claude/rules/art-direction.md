---
paths:
  - "Assets/**/*.{unity,prefab,mat,shader,shadergraph,hlsl}"
  - "Assets/02.Scripts/Rendering/**"
  - "Assets/06.Rendering/**"
  - "Assets/02.Scripts/Games/Yacht/Presentation/**/{YachtLightingRig,YachtCameraRig,TabletopSurfaceBuilder}.cs"
---

# 아트 방향

- 색·조명·재질 값의 원본은 `docs/reference/art_style_guide.md` 입니다. 조명은 §1, 테이블·러너는 §2·§3, 팔레트 토큰은 §4, 작업 지침은 §5를 봅니다. 값을 다른 문서나 에이전트 정의에 다시 적지 않습니다.
- 조명 색·강도는 `YachtLightingRig` 프리셋이 런타임에 덮어씁니다. 씬에 직렬화된 라이트 값을 고쳐도 반영되지 않습니다.
- §4 팔레트 토큰 중 조명·카메라 토큰은 실제 라이트·카메라 값과 다릅니다. 라이트나 카메라를 토큰 값에 맞추라는 근거로 쓰지 않습니다.
- 실사 텍스처 노이즈, 주름, 노멀 왜곡을 지향하는 셰이더·머티리얼 설정을 넣지 않습니다. 스타일라이즈드 핸드페인티드 방향을 따릅니다.
- 원본과 다른 색·조명·재질 값을 지시받으면 실행하되 그 사실을 보고에 남깁니다.
