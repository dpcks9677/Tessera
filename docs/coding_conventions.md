# Tessera 코딩 규약

이 문서는 Tessera의 C# 코딩 규약을 정의한다. 기준은 Microsoft의 [C# 코딩 규칙](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)과 [.NET 이름 지정 지침](https://learn.microsoft.com/dotnet/standard/design-guidelines/naming-guidelines)이며, Unity 직렬화와 충돌하는 항목만 예외로 둔다.

기계가 강제할 수 있는 항목은 리포지토리 루트의 [`.editorconfig`](../.editorconfig)에 있다. 이 문서는 그 근거와 예외 사유를 설명한다.

관련 결정 기록: `D-031` (규약 채택 및 Unity 예외), `D-029` (마일스톤 재번호), `D-030` (상태 소유권)

---

## 1. 적용 방침

**일괄 재포맷하지 않는다.** `.editorconfig`는 도입하되, 규약은 다음에만 적용한다.

- 새로 만드는 파일
- 리팩토링으로 실제 수정하는 파일

전체 포맷 diff는 리뷰를 불가능하게 만들고, 씬·프리팹 직렬화와 무관한 위험만 늘린다. 기존 파일을 규약에 맞추려고 별도 커밋을 만들지 않는다.

**규약이 기존 코드와 충돌하면 기존 코드를 따른다.** `AGENTS.md`의 "정밀 수정" 원칙이 우선한다. 주변 코드 스타일을 임의로 바꾸지 않는다.

---

## 2. 서식

| 항목 | 규칙 |
|---|---|
| 들여쓰기 | 스페이스 4칸 (탭 금지) |
| 인코딩 | UTF-8 |
| 줄 끝 | LF |
| 파일 끝 | 개행 1개 |
| 후행 공백 | 제거 (Markdown 제외) |
| 중괄호 | Allman. 여는 중괄호는 항상 새 줄 |

`.unity`, `.prefab`, `.asset`, `.meta` 등 Unity가 생성·관리하는 파일은 규약 적용 대상이 아니다. `.editorconfig`에서 명시적으로 제외했다.

---

## 3. 이름 규칙

| 대상 | 규칙 | 예시 |
|---|---|---|
| 네임스페이스, 클래스, 구조체, 열거형, 델리게이트 | `PascalCase` | `YachtAugmentRuntime` |
| 메서드, 프로퍼티, 이벤트 | `PascalCase` | `CalculateScores`, `CurrentState` |
| 인터페이스 | `I` + `PascalCase` | `IAugmentHandler` |
| 제네릭 타입 매개변수 | `T` + `PascalCase` | `TState` |
| 상수 (`const`) | `PascalCase` | `DeadlineTurn` |
| public / protected 필드 | `PascalCase` | `Revision` |
| **private / internal 필드** | **`camelCase` (접두사 없음)** | `hourglassTimer` |
| 지역 변수, 매개변수 | `camelCase` | `playerIndex` |

### private 필드에 `_` 접두사를 쓰지 않는 이유

Microsoft 규약은 private 인스턴스 필드에 `_camelCase`, private static 필드에 `s_camelCase`를 권장한다. Tessera는 이를 채택하지 **않는다**.

1. **Inspector 표시명**: Unity는 `[SerializeField]` 필드명을 Inspector에 그대로 표시한다. `_hourglassTimer`는 "Hourglass Timer"가 아니라 어색한 형태로 렌더링된다.
2. **직렬화 참조 파손**: 필드명은 씬·프리팹 YAML의 직렬화 키다. 이름을 바꾸면 씬에 저장된 참조가 조용히 끊긴다. `[FormerlySerializedAs]`로 막을 수 있지만 파일 500개 이상에 붙일 이유가 없다.
3. **기존 코드 전체가 이미 이 규칙이다.**

`this.` 한정자도 쓰지 않는다. 필드와 매개변수 이름이 겹치면 매개변수 이름을 바꾼다.

---

## 4. 언어 사용

- `using`은 네임스페이스 블록 **밖**에 두고, `System.*`을 최우선으로 정렬한다.
- 네임스페이스는 **블록 형식**을 유지한다. file-scoped 네임스페이스로 바꾸지 않는다 (기존 전 파일이 블록 형식이며, 변환은 순수 노이즈다).
- `var`는 우변에서 타입이 명백할 때만 쓴다. `new()` 대상 타입 추론은 좌변에 타입이 있으므로 허용한다.
- 접근 한정자를 항상 명시한다.
- 상속하지 않을 클래스는 `sealed`로 선언한다.
- 단일 문 블록에도 중괄호를 쓴다. **예외**: 한 줄 조기 반환 (`if (state == null) return;`). 기존 코드가 광범위하게 쓰고 있어 유지한다.
- `int`, `string` 같은 언어 키워드를 `Int32`, `String` 대신 쓴다.

---

## 5. 파일 구성

- 한 파일에 public 타입 하나가 원칙이다.
- **예외**: 증강 핸들러 파일. `XxxHandler`와 그 전용 `XxxState`를 같은 파일에 둔다. 한 증강의 정의·상태·로직이 한 파일에 모이는 것이 `M8`의 목표였고, 상태 클래스를 분리하면 그 목표가 무너진다.

---

## 6. 비동기

**코루틴을 유지한다.** `async`/`await`로 바꾸지 않는다.

Unity의 프레임 동기 연출(주사위 굴림, 카드 배치, 턴 전환)은 코루틴이 적합하다. `IGameAuthority.ExecuteAsync`가 `Task<>`를 반환하는 것은 원격 권위 이관 대비 시그니처이며, `LocalGameAuthority`는 동기 구현으로 `Task.FromResult`를 반환한다. 이 비대칭은 의도된 것이다.

---

## 7. 주석과 문서

- 모든 주석·문서는 한국어로 쓴다 (`AGENTS.md` §1).
- public 타입과 비자명한 public 멤버에 `///` XML 주석을 단다.
- "무엇을 하는지"가 아니라 **"왜 그렇게 했는지"**를 쓴다. 코드가 이미 말하는 것을 반복하지 않는다.

---

## 8. 검사 방법

```bash
dotnet format Tessera.sln --verify-no-changes --include <수정한 파일 경로>
```

Rider와 Visual Studio는 `.editorconfig`를 자동으로 읽는다. Unity가 `.csproj`를 재생성해도 리포지토리 루트의 `.editorconfig`는 영향받지 않는다.
