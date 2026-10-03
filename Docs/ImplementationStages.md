# 구현 단계 / 변경 파일 / 검증 결과

STEP 2~32를 기존 입력 파이프라인 위에 확장했다. 각 묶음에서 C# 컴파일을 확인한 후 다음 의존 기능을 연결했고,
최종 검증은 Unity의 실제 Editor/PlayMode에서 수행했다. 파일 경로는 `Assets/Scripts/` 기준이다.

| 단계 | 주요 변경 파일/데이터 | 검증 방법 |
|---|---|---|
| 2 | PlayerMotor, PlayerGroundDetector, PlayerMovementData, StateMachine 기본 6종, PlayerController | 실제 물리에서 점프/낙하/착지. A/D 이동은 Game View |
| 3 | PlayerLocomotion, PlayerMotor.Gravity, CommandBuffer | 6F Coyote, 착지 선입력, 0F 착지 전환 테스트 |
| 4 | GroundDashState, AirDashState, PlayerLocomotion | 지상/공중 대시 속도/수명/1회 제한/착지 복구, Double Jump |
| 5 | PlayerMotor.CorrectCorners, PlayerGroundDetector | 작은 머리 모서리 보정, 넓은 천장 통과 금지, 대시 발끝 보정, Ledge Forgiveness |
| 6 | Combat/AttackData, PlayerAttackLoadout, PlayerCombat, AttackState, AttackPresentation | asset validation, 프레임 경계, DummyAttack.asset |
| 7 | Hitbox, Hurtbox, HitResult | 1회 공격 중복 피해 방지, Hitbox 즉시 해제, 좌우 Offset mirror 코드 |
| 8 | EnemyData, EnemyMotor, EnemyStateMachine, EnemyController, DamageReceiver | Launch 반응, HP 감소, 공중 중력 progression data 검사 |
| 9 | LMB_01/02/03.asset, AttackState/PlayerCombat | 연속 LMB 선입력, ComboIndex 0→1→2, Whiff 자체 이동 |
| 10 | Shift_LMB.asset | 18F 준비/9F Hitstop/26F Recovery asset 검사. 전환은 동일 Cancel 경로 |
| 11 | Shift_S_LMB.asset, HitReactionProfile | Launcher→Jump, 같은 Launcher의 보스 Neutral Launch 거부 |
| 12 | Air_LMB_01/02/03.asset | Launcher→Jump→Air 3타가 각각 실제 명중하는 이벤트까지 확인 |
| 13 | Air_Shift_S_LMB.asset, AttackState Dive 경로 | Air 콤보에서 Dive 전환 후 지면 착지 |
| 14 | AttackState, Invulnerability, PlayerCombat | Jump Cancel 후 Hitbox 비활성, Jump 무적과 피격 무적의 별도 원인 |
| 15 | RMB/Air_RMB.asset, Hitbox.Shoot | 7개 tracer, 다중 Pellet의 대상당 통합 피해, 지상/공중 실제 사격 |
| 16 | HitstopController, CombatEvents, CommandBuffer.DeferExpiration | actor 위치 정지 중 Clock 전진/Jump Buffer 보존, 정지 해제 후 점프 |
| 17 | CameraController, CameraRoom | Game View 기본 추적/룸 배치 확인. 공중 추적과 Bounds 수치는 Inspector 노출 |
| 18 | EnemyController, MudFodder.asset, MudCharge.asset | 예고→Active→Recovery 코드 경로. Game View에서 AI 접근/예고 확인 가능 |
| 19 | DamageReceiver, HitState, DeadState, Invulnerability | 피격 HP 감소, 같은 무적 기간 재피격 무시, 사망 시 공격 불가 |
| 20 | StyleEvent/Relay/System/Data | 반복 사용 점수 배율, 피격 감소. Damage 수치는 별개 |
| 21 | FodderProfile/BossProfile, EnemyController | 동일 Light가 Fodder Hitstun / Boss Neutral DamageOnly / Recovery·Stagger Hitstun |
| 22 | BossQuick/BossHeavy/BossApproach.asset, PrototypeBoss.asset | 3개 패턴 asset 검사, Debug UI에서 State/AttackFrame 노출 |
| 23 | Editor/AttackDataEditor, PlayerAttackLoadoutEditor | 잘못된 프레임/캔슬/Size/필수 참조 HelpBox, 모든 기본 asset validation |
| 24 | AttackPreviewObject, AttackPreviewObjectEditor | Scene preview, Frame, Mirror, Undo/dirty 경로. 수동 핸들 사용법은 별도 가이드 |
| 25 | AttackEditorWindow | 목록/Inspector/Timeline/Frame/Play/Pause/AnimationMode 연결 |
| 26 | CombatPrototypeBuilder, CombatTestReferences, CombatDebugUI, CombatTestScene | TilemapCollider+Composite의 실제 충돌을 PlayMode에서 검증 |
| 27 | AttackPresentation, CombatPresentationHooks, CameraImpulseProfile | 모든 연출 참조가 null인 기본 씬으로 실제 공격 테스트 |
| 28 | CombatRecorder, CombatSnapshot, Clock의 post-physics 완료 phase | 스냅샷 위치가 실제 물리 결과와 일치, 커맨드 이벤트와 적 3체 기록 |
| 29 | HighlightSettings, HighlightSegmentSelector | 순 RankDelta가 최대인 구간 선택, 최소 증가량 미달 거부 |
| 30 | HighlightReplayPlayer, Debug UI 결과 overlay | ghost 생성/원본 숨김/Clock 정지, Pause/Restart/End와 기록 재개 |
| 31 | 생성 asset 튜닝, Hitstop 중 버퍼 보존, Tilemap 생성 순서 수정 | 이동→공중 전투→피격→기록의 통합 테스트 및 Game View 확인 |
| 32 | 최종 문서 6종, 테스트 코드, Windows 빌드 검증 | 아래 결과와 KnownIssues 참조 |

## 실제 적용 프레임

- CombatClock: 60F/s, Fixed Timestep 0.016666668초.
- Coyote 6F, Jump Buffer 8F, 일반 공격/기타 Buffer 6F, Double Tap 10F.
- Ground Dash Startup 3F / Duration 12F, Air Dash 5F / 12F.
- 일반 착지 0F, Dive 착지 Recovery 9F.
- Jump 무적 4F, 피격 후 무적 45F. 둘은 별도 원인으로 관리.
- Light Hitstop 4~5F, Heavy 9F, Launcher 8F, Dive 10F, Shotgun 5F.
- Hitstop 중 Clock/InputHistory는 계속 진행하고 CommandBuffer의 남은 수명은 보존.

## 자동 테스트 결과

2026-10-02, Unity 6000.3.11f1:

- EditMode: **19 / 19 통과**, 실패/스킵 0.
- PlayMode: **20 / 20 통과**, 실패/스킵 0.
- 전체 Runtime/Editor/Test C#의 설치 Unity DLL 기반 별도 컴파일: 오류/경고 0.
- 별도 순수 입력 검사: **13 / 13 통과** (EditMode와 중복이므로 39개에 더하지 않음).
- Game View 화면 확인 및 Unity Console 오류 0.
- 최종 Windows x64 Development Build: **성공, 오류 0 / 경고 0**, 162.35 MB.
  실행파일: `Builds/CombatPrototype/BABODAYO.exe` (전체 출력 폴더는 Git 제외).
- Standalone headless 초기 구동을 8초 실행해 예외가 없음을 확인했다. 이는 수동 플레이/그래픽 검증을 대체하지 않는다.

테스트는 `Assets/Tests/EditMode`와 `Assets/Tests/PlayMode`에 있다.
최종 EditMode 실행은 MCP 완료 callback이 누락되었지만 Unity가 작성한 TestResults.xml에서
19 passed / 0 failed를 확인했다. 복사본은 `Temp/PrototypeVerification/EditModeResults.xml`이다.
모서리 보정 검증은 물리 시뮬레이션 전 Rigidbody 위치를 검사한다. Transform 반영은 다음 physics step에 일어난다.
처음 검출한 Terrain 경로 0개 문제는 Composite를 먼저 생성하고 TilemapCollider의 tile 변경을 처리한 뒤
Merge/GenerateGeometry 하도록 고쳐 해결했다.
공중 콤보는 명령 전환뿐 아니라 세 기술 각각의 Hit 이벤트를 검사한다. 초기 캔슬 창이 첫 Active와 같아
선입력이 타격을 건너뛰던 문제를 데이터의 Cancel.Start=Startup+1로 수정했다. 좌측 근접/공중 RMB도 검증했다.

## 수동 검증과 자동 검증의 구분

반응/프레임/물리/커맨드/스냅샷의 수치 조건은 테스트로 검증했다.
대시의 체감, 잡몹을 쓸어버리는 속도, 보스의 재미, 카메라의 장시간 가독성은 자동 assertion으로
확정하지 않았다. `PrototypeControls.md` 순서로 플레이테스트하고 asset을 튜닝해야 한다.
Editor Handle의 실제 드래그/Undo, 최종 rig animation binding도 아트 연결 후 수동 확인 대상이다.

## 변경 범위

Runtime/Editor 코드, 테스트, GameData asset, 임시 sprite/material/tile, CombatTestScene, 문서를 추가했다.
TimeManager는 60Hz, Build Settings에는 CombatTestScene을 추가했다.
기존에 사용자가 수정한 ProjectSettings는 되돌리지 않았다. Unity가 열기/빌드 중 직렬화한
URP/Editor 설정 변경도 변경 목록에서 확인할 수 있다.
