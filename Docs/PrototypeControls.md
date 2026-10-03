# Prototype Controls / 검증 방법

## 실행

Unity에서 `Assets/Scenes/CombatTestScene.unity`를 열고 Play.
Game View를 클릭해 포커스를 준다. Input System은 기본 Dynamic Update를 사용한다.
씬이 이미 만들어져 있으므로 생성 도구를 다시 실행할 필요가 없다.

Windows Development 실행파일은 `Builds/CombatPrototype/BABODAYO.exe`에 있다.
외부로 옮길 때는 Data/MonoBleedingEdge/UnityPlayer.dll을 포함한 폴더 전체를 함께 옮긴다.
직접 새로 빌드할 때는 Build Profiles의 Scene List에 CombatTestScene만 포함하거나 첫 씬으로 지정한다.
기존 SampleScene의 등록은 보존되어 있다.

오른쪽 순서: Player(x=0), 자동 회복 Dummy(x=3), Mud Fodder(x=11), Boss(x=25).
왼쪽에는 점프/코요테/모서리 확인용 발판이 있다.
Fodder/Boss는 8 Unit 안으로 들어가면 접근한다.

| 조작 | 동작 |
|---|---|
| A / D | 좌우 이동, 비공격 이동 중 이동 방향 Facing |
| 마우스 커서 | 대기·공격 중 커서 좌우 방향 Facing (공격 중에도 갱신) |
| Space | 고정 높이 점프, 공중 추가 1회 |
| A → 중립 → A / D → 중립 → D | 지상/공중 대시, 기본 더블탭 창 10F |
| LMB | 지상 3타 / 공중 3타 |
| Shift + LMB | 강공격 |
| Shift + S + LMB | 지상 Launcher / 공중 내려찍기 |
| RMB | 지상/공중 산탄총 |
| 지상 Shift + S + LMB 10F Hold | 적과 함께 상승하는 Launcher (짧게 누르면 해제 시 일반 Launcher) |
| 지상 Shift + A/D + LMB | 돌진 공격 |
| W + LMB | 위쪽 찌르기 |
| A,A / D,D 뒤 6F 이내 LMB | 대시 공격 |
| 공중 A/D + LMB | 대각선 내려꽂기 |
| 공중 Shift + A/D + LMB | 공중 관통 공격 |
| 공격 중 Space | Jump Cancel (사용 가능한 점프 필요) |
| F1 | Debug UI 표시/숨김 |
| F2 | 최고 Highlight 선택 / 재생 종료 |

Shift 기본 바인딩은 왼쪽 Shift다. PlayerInputReader Inspector에서 변경할 수 있다.
UI의 Reset training actors는 체력과 위치를 복원한다.
점수는 계속 유지한다. 전체 세션 초기화는 Play를 다시 시작한다.

## 수동 플레이 검증

1. 좌우 빠른 전환 후 정지: Acceleration보다 ReverseAcceleration이 크고 감속도 빠른지 확인.
2. Space를 짧게/길게 눌러 최고 높이가 같은지 확인.
3. 왼쪽 발판 끝을 넘어 6F 이내 점프: 공중 점프 횟수를 쓰지 않고 점프.
4. 공중 점프를 다 쓴 뒤 착지 직전 Space: 8F 버퍼 안이면 즉시 재점프.
5. A,A/D,D를 지상/공중에서 실행. 공중 1회 제한과 착지 후 복구 확인.
6. 발판 모서리에 머리/대시 발끝을 살짝 걸쳐 보정 확인. 넓은 천장/벽은 통과하지 않아야 함.
7. Dummy 앞에서 LMB → LMB → LMB. 창 직전 6F 선입력/너무 늦은 입력을 비교.
8. 빈 공간에서도 동일 공격이 같은 자체 이동을 하는지 확인.
9. Dummy 가까이 Shift+S+LMB → Space → LMB 3회 → Shift+S+LMB.
   위치/높이 자동 보정은 없으므로 수동 spacing이 필요.
10. 공격 중 Jump Cancel로 빨간 Hitbox가 꺼지는지 확인.
11. 지상/공중 RMB에서 노란 확산 ray와 짧은 사거리를 확인.
12. Boss의 노란 예고/빨간 Active를 피하고 파란 Recovery에 반격.
    같은 LMB가 Neutral에서는 피해만, Recovery/Stagger에서는 경직을 주는지 확인.
13. 기술 반복/교체, 공중 타격, 처치, 피격, 비전투 대기로 Rank 변화 확인.
14. 오른쪽 CombatRoom과 공중 콤보에서 카메라의 중심/수직 추적이 과도하지 않은지 확인.
15. 4초 이상 전투하고 F2. 조건을 만족하는 구간이 없으면 안내 문구가 나온다.
    Replay의 Pause/Play, Replay from start, Skip을 확인.
16. Attack Editor에서 수치를 바꾸고 다음 Play에서 반영되는지 확인.

## Debug UI

Combat Frame, Player State, 마지막 실행 Command, Attack ID/Frame, Velocity, Grounded,
Coyote/Jump Buffer 잔여 F, 공중 자원, Enemy State/HP/Hitstun/Gravity/AttackFrame,
Combo Count, Rank를 표시한다.

Editor/Development Build에서 F1 진단을 지원한다.
일반 빌드는 AllowReleaseDebug=false이면 진단 패널이 숨겨지고 Rank/HP 및 F2 결과 UI는 유지된다.
Hitbox.DebugDraw는 Scene Gizmo, Hitbox.Outline/PelletLines는 별도의 프로토타입 Game-view 선이다.

## 자동 검증

Window > General > Test Runner:
- EditMode: Babodayo.EditModeTests
- PlayMode: Babodayo.PlayModeTests

PlayMode 테스트는 생성된 씬을 불러오고 동일 CombatClock.Step/Physics2D.Simulate 경로로 검증한다.
실제 키보드/마우스 callback 테스트도 별도로 포함한다.
테스트 전에 수정 중인 씬을 저장한다.

보조 컴파일/순수 입력 검사:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Verify-InputFoundation.ps1
```
이 스크립트는 모든 런타임/Editor/테스트 C#를 설치된 Unity DLL에 맞춰 컴파일하고,
엔진 독립 입력 테스트 20개를 실행한다. 실제 물리/입력 라이프사이클은 Unity Test Runner로 검증한다.
