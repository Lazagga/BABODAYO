# 전투 프로토타입 기반 구조 — 1단계

> 이 문서는 1단계 당시 기록입니다. 현재 구현·프레임 순서·검증 결과는 [Architecture.md](Architecture.md)와 [ImplementationStages.md](ImplementationStages.md)를 보세요.

Unity **6000.3.11f1**, Input System **1.19.0**, 전투 기준 **60F/s**.
이번 구현 범위는 Clock, 입력 기록/해석/버퍼, PlayerController 연결 골격이다.
이동, State 클래스, 공격, 적, 카메라, 전투 테스트 씬은 아직 구현하지 않았다.

## 파일과 책임

| 파일 (`Assets/Scripts/` 기준) | 책임 |
| --- | --- |
| `Core/CombatClock.cs` | FixedUpdate당 공통 프레임 증가, 입력 준비 → 시뮬레이션 → 논리 틱 완료 순서 보장 |
| `Input/PlayerInputReader.cs` | Unity Input System 접근을 전담. 버튼 에지 보존, 방향 양자화, 프레임 입력 생성 |
| `Input/InputHistory.cs` | `InputFrame`, `InputButtons`, 최근 입력 Circular Buffer. 최신부터 조회 |
| `Input/CombatCommand.cs` | 실제 조작법을 나타내는 enum. 기술명/지상·공중 의미를 포함하지 않음 |
| `Input/CommandParser.cs` | 커맨드 우선순위, History 기반 더블탭 판정, 동일 프레임 중복 해석 방지 |
| `Input/CommandBuffer.cs` | `BufferedCommand`, 수명 설정, Push/Peek/TryConsume/Expire, 용량 제한 |
| `Player/PlayerController.cs` | 명시적 참조와 파이프라인 수명 관리. 향후 StateMachine을 연결할 `StateTick` 제공 |
| `Babodayo.Runtime.asmdef` | 런타임 코드와 Input System 참조를 묶는 어셈블리 |
| `Editor/Babodayo.Editor.asmdef` | 향후 편집 도구가 플레이어 빌드에 포함되지 않도록 Editor 코드 분리 |

추후 구현용 폴더: `Player/StateMachine`, `Combat`, `Enemy`, `Camera`, `Rank`,
`Replay`, `Editor`, `Assets/GameData/Attacks`. 빈 폴더는 `.gitkeep`으로 Git에 유지한다.
빈 State나 미구현 전투 클래스를 미리 만들지 않았다.

추가 파일:

- `Assets/Tests/EditMode/InputPipelineTests.cs`: 링 버퍼, 우선순위, 더블탭, 만료·소비 경계 테스트.
- `Assets/Tests/EditMode/PlayerInputReaderTests.cs`: 가상 키보드/마우스로 짧은 클릭, 복합 입력, 재활성화 테스트.
- `Assets/Tests/EditMode/Babodayo.EditModeTests.asmdef`: Editor 전용 테스트 어셈블리.
- `Tools/Verify-InputFoundation.ps1`, `Tools/Tests/PureInputTestRunner.cs`: Unity 설치 어셈블리로 전체 C# 컴파일 후 엔진 독립 테스트를 실행하는 보조 도구.
- 본 문서, 루트 `README.md`의 문서 링크, Unity `.meta` 파일.

## 데이터 흐름

```text
Unity Input System 이벤트
  → PlayerInputReader (Pressed/Released를 다음 틱까지 보관)
  → CombatClock.InputTick
      → Capture(frame): InputFrame
      → InputHistory.Add
      → CommandBuffer.Expire
      → CommandParser.Parse(history, buffer)
  → CombatClock.SimulationTick
      → PlayerController.StateTick
      → [다음 단계] PlayerStateMachine: Commands.TryConsume(...)
  → CombatClock.CompletedTick
```

기술 선택은 **현재 State + Command**의 책임이다. Parser는 Ground/Air, 적 종류,
적 위치, Facing을 알지 못한다. 상태 전환에는 CommandBuffer를 사용한다.
연속 이동 축은 이미 정리된 `CurrentInput.Horizontal/Vertical`로 전달할 수 있다.
State에서 Input System을 읽거나 Rigidbody2D를 조작하지 않는다.

## 프레임 규약

- `ProjectSettings/TimeManager.asset`의 Fixed Timestep을 **0.016666668초**로 변경했다.
  Clock도 시작 시 이 값을 검사한다. Clock을 하나 생성하고 모든 전투 객체가 참조한다.
- 프레임 0은 초기 기준점이며 첫 FixedUpdate는 프레임 1이다. 렌더 Update 횟수를 세지 않는다.
- Clock의 세 이벤트 안에서 순서를 명시하므로 각 컴포넌트의 FixedUpdate 실행 순서에 의존하지 않는다.
  향후 State/Motor의 논리 갱신도 SimulationTick에서 호출한다.
- Input System은 기본 **Process Events In Dynamic Update**를 사용한다.
  이벤트가 발생한 뒤 첫 전투 틱에 보관된 버튼 에지를 전달한다.
  Dynamic Update가 FixedUpdate 뒤에 실행될 수 있으므로 입력 반영이 다음 물리 틱으로 밀릴 수 있다.
  Fixed Update 모드도 Reader가 처리할 수 있지만, 프로젝트 전체 입력 갱신 정책은 한 곳에서 정해야 한다.
- 렌더 프레임 하나에 FixedUpdate가 여러 번 발생해도 Pressed/Released는 한 번만 나온다.
  버튼을 틱 사이에 눌렀다 떼면 같은 InputFrame에 Pressed와 Released가 함께 남는다.
- 방향은 틱 시점의 -1/0/+1, Held는 캡처 시점 상태다. LMB 커맨드는 **누른 순간**의 방향과 Shift를
  별도 보존하므로 캡처 전에 키를 떼어도 Shift+S+LMB가 LMB로 바뀌지 않는다.
- 한 틱 사이 동일 버튼을 여러 번 누르면 첫 Press 하나로 합친다. 방향의 틱 사이 중간 변화는
  기록하지 않는다. 따라서 더블탭의 Neutral은 적어도 한 전투 틱에 관측되어야 한다.
  이벤트 시각 단위 재현이나 서브프레임 복합 커맨드는 후속 확장 사항이다.
- `CompletedTick`은 **논리 처리 완료**이며 Unity의 자동 2D 물리 시뮬레이션보다 앞선다.
  추후 Recorder가 물리 결과를 기록하려면 별도 post-physics 지점이 필요하다.
- 아직 Hitstop은 없다. 향후 공통 Clock은 계속 전진시키고 전투 객체의 로컬 공격 프레임/물리만
  정지시키는 방식으로 확장한다. Time.timeScale 기반 전체 정지는 UI/카메라 분리 용도로 사용하지 않는다.
- Clock은 고정 프레임 번호를 제공할 뿐 Rigidbody2D의 결정론적 리플레이를 보장하지 않는다.

## 입력과 커맨드 정책

기본 바인딩: WASD, LMB, RMB, Space, 왼쪽 Shift.
Reader Inspector에서 각 InputAction 바인딩을 편집할 수 있다.
Button 액션에 Hold/Tap Interaction을 추가하면 Press 의미가 바뀌므로 사용하지 않는다.
기존 `Assets/InputSystem_Actions.inputactions`는 수정하지 않았다. Reader는 자체 액션을 소유하며
Enable/Disable/Dispose를 관리한다. 동일 플레이어 입력을 Unity `PlayerInput`과 중복 구독할 필요가 없다.

현재 생성하는 커맨드:

- `Shift_S_LMB > Shift_LMB > LMB`: LMB 에지 하나에서 정확히 한 후보.
- `RMB`, `Space`: 독립 버튼이라 같은 프레임에 각각 보관 가능. State가 소비 우선순위를 결정한다.
- `DoubleTapA`, `DoubleTapD`: 같은 방향의 두 Press 사이에 Neutral 필요. 기본 10F 이내,
  반대 방향이 끼면 무효. 오래 누른 방향을 방금 누른 첫 탭으로 오인하지 않는다.
- LMB와 더블탭 완성이 같은 프레임이면 LMB 계열을 우선하고 단독 더블탭은 생성하지 않는다.
  `DoubleTapA_LMB/DoubleTapD_LMB` 기술은 후속 범위다.

Hold, Shift+방향, W+LMB, 더블탭+LMB enum 항목은 **예약만** 해 두었다.
아직 생성하지 않으므로 미구현 커맨드가 기존 LMB를 가로채지 않는다.
기본 3타 콤보도 미래 AttackState가 같은 LMB를 콤보 인덱스에 따라 처리한다.

## History와 Buffer

- History 기본 30F, Inspector 20~30F. 더블탭의 이전 Press와 그 직전 상태를 조회하기 위해
  실제 용량은 최소 `DoubleTapFrames + 2`로 확보한다(최대 30).
  `TryGetRecent(0)`은 최신 프레임이다. 같은/과거 프레임 추가는 오류로 검출한다.
- 공격 6F, 점프 8F, 기타 6F. Inspector에서 변경 가능.
- 수명은 `[CreatedFrame, ExpireFrame)`이다. 프레임 100에 생성한 6F 커맨드는 100~105에서
  소비할 수 있고 106에 만료된다. 상태가 추가 쿼리하더라도 Peek/TryConsume 내부에서 만료를 검사한다.
- TryConsume은 요청한 종류의 가장 오래된 유효 입력 하나만 제거한다.
  아직 처리 불가능한 입력을 먼저 소비하지 말고 상태 조건을 확인한 뒤 호출한다.
- 버퍼 용량 기본 16. 넘치면 가장 오래된 입력을 버린다. None은 저장하지 않는다.
- 비활성화 시 버퍼를 비우고 재활성화 시 History/Parser도 초기화하여 이전 커맨드가 재실행되지 않게 한다.
- History는 커맨드 해석용이며 Replay 저장소가 아니다.

## 씬에서 연결하는 방법

이번 단계에서는 씬/프리팹을 생성하지 않는다. 확인용 객체를 만들 경우:

1. 빈 GameObject 하나에 `CombatClock` 추가.
2. 플레이어 객체에 `PlayerInputReader`, `PlayerController` 추가.
3. Controller의 Clock과 Input Reader 필드에 위 컴포넌트를 직접 할당.
4. Project Settings > Time에서 Fixed Timestep이 1/60인지 확인.
5. State 구현 후 `StateTick`을 구독해 처리하고, 비활성화 시 구독 해제.

```csharp
// 향후 State의 예시. 실제 Jump/State는 이번 단계에 구현하지 않는다.
if (canJump && controller.Commands.TryConsume(
        CombatCommand.Space, frame, out var command))
{
    // PlayerMotor.Jump + State 전환
}
```

Reader와 Controller는 플레이어마다 한 쌍으로 소유한다. Clock은 같은 전투 공간에서 공유한다.
현재는 로컬 키보드/마우스 1인용이며 멀티플레이 장치 페어링은 구현하지 않았다.

## 검증

Unity Test Runner의 EditMode에서 `Babodayo.EditModeTests` 실행.
가상 입력 테스트 3개를 포함한다. 실제 Editor 테스트 실행은 이번 작업 중 MCP 승인 정책으로
차단되어 완료하지 못했다.

Windows 보조 검증:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Verify-InputFoundation.ps1
```

Unity 설치 경로가 다르면 `-UnityEditorData`로 Editor/Data 경로를 지정한다.
전체 런타임/테스트 스크립트를 설치된 Unity DLL에 맞춰 컴파일하고 순수 입력 테스트를 실행한다.
이번 작업에서 C# 컴파일 경고/오류 0개, 순수 입력 테스트 **13개 통과**를 확인했다.
Input System 가상 장치/MonoBehaviour 테스트는 Unity 엔진을 필요로 하므로 이 도구에서 실행하지 않는다.
산출물은 Git에서 제외된 `Temp/InputFoundationChecks`에만 생성한다.

## 다음 단계

1. PlayerMotor와 PlayerGroundDetector: 연속 좌표, Rigidbody2D 조작 단일 책임.
2. Ground/Jump/Fall State와 고정 높이 점프, Coyote 6F, 기존 Jump Buffer 8F 연결.
3. Double Jump, Ground/Air Dash와 이동 보정(적 방향/위치 자동 보정 제외).
4. 이후 우선순위에 따라 AttackData → 공통 AttackState → 피격/더미 → 콤보 순서로 확장.

거리 단위는 1 Tile = 1 Unity Unit. Tilemap/CompositeCollider2D는 맵 충돌용이며
플레이어 좌표나 전투 판정을 Grid에 제한하지 않는다. 적 반응 차이는 향후 Enemy 반응 데이터에 두고,
플레이어 공격 성능은 잡몹/보스 구분 없이 동일하게 유지한다.
