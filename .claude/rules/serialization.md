---
paths:
  - "Assets/Scripts/**/*.cs"
  - "Assets/**/*.{unity,prefab}"
---

# 직렬화 안전성

이 프로젝트에서 가장 비싼 실수는 씬·프리팹 데이터 유실입니다.

- `[SerializeField]` 필드의 이름과 타입을 바꾸지 않습니다. 직렬화 필드는 그대로 두고 내부 로직만 헬퍼로 위임합니다. 원본은 `docs/plan/tracks/solid/README.md` §1.1 3항입니다.
- 직렬화 필드를 프로퍼티로 바꾸지 않고, 직렬화 대상 클래스에서 `[Serializable]` 을 빼지 않습니다.
- 지시가 위 변경을 요구하면 실행하지 않고 보고합니다.
- 씬을 저장하기 전에 null 머티리얼 오버라이드가 새지 않는지 확인합니다(커밋 `8083b1b` 에서 실제로 발생).
- `MonoBehaviour`/`ScriptableObject` 를 상속하지 않는 순수 로직 클래스는 이 규칙의 대상이 아닙니다.
