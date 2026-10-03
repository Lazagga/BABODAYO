# BABODAYO 전투 프로토타입 구조

Unity 6000.3.11f1 / Input System 1.19.0 / 2D / 60 Combat Frames per second.
실행 씬: `Assets/Scenes/CombatTestScene.unity`.
이 문서가 현재 구조의 기준이며 `CombatArchitecture.md`는 1단계 당시 기록이다.

## 설계 경계

- 입력 커맨드는 조작법이다. 지상/공중 State와 로드아웃이 실제 기술을 선택한다.
- 적 위치를 읽어 플레이어 Facing, 위치, 높이를 자동 조정하지 않는다.
- PlayerInputReader → InputFrame.PointerPosition → PlayerFacing 경로로 커서 좌우 방향을 결정한다.
  비공격 이동/대시는 이동 방향, 대기·공격은 커서 방향이다. State는 Input System을 읽지 않는다.
- PlayerMotor/EnemyMotor의 몸통 Collider는 ActorBodyLayers(Layer 8)를 충돌에서 제외한다.
  지형 충돌과 Hurtbox 쿼리는 유지한다. 캐릭터가 서로를 바닥처럼 받치지 않는다.
  현재 기술 매핑과 입력 우선순위는 [AttackInventory](AttackInventory.md)를 참고한다.
- PlayerMotor와 EnemyMotor가 각각 자기 Rigidbody2D의 유일한 쓰기 소유자다.
- 전투 수치는 ScriptableObject가 기준이다. 초기 샘플 수치는 Editor 전용 생성기가 asset에 기록한다.
- Player AttackData를 적 종류에 따라 복제/변경하지 않는다. HitReactionProfile이 적의 반응을 결정한다.
- Singleton과 FindObjectOfType 계열 검색을 사용하지 않는다. 씬 참조와 이벤트 연결은 명시적이다.
- 스테이지 Tilemap은 맵 충돌/표시용이다. 캐릭터와 판정은 연속 좌표이고 1 Tile = 1 Unit이다.

## 스크립트 구조

| 폴더 | 클래스와 책임 |
|---|---|
| Core | CombatClock: 전투·물리 순서. CombatDebugUI: 진단/결과 UI. CombatTestReferences: 씬의 명시적 테스트 참조 |
| Input | PlayerInputReader: Input System 수집. InputHistory/InputFrame: 30F 원형 기록. CombatCommand: 조작 enum. CommandParser: 우선순위/더블탭. CommandBuffer: 수명/소비 |
| Player | PlayerController: 파이프라인과 State 구동. PlayerMotor: 물리/보정. PlayerGroundDetector: 지면 센서. PlayerLocomotion: 점프·대시 자원/코요테. PlayerMovementData: 튜닝 |
| Player | PlayerCombat: 커맨드→AttackData, 피격→State 연결. PlayerAttackLoadout: 지상/공중 기술 매핑 |
| Player/StateMachine | PlayerState, PlayerStateMachine, GroundIdleState, GroundMoveState, JumpState, FallState, GroundDashState, AirDashState, AttackState, HitState, DeadState |
| Combat | AttackData: 프레임/피격/이동/캔슬/판정/연출 데이터. Hitbox: 판정/히트스캔/중복 방지. Hurtbox: 피격 연결. HitResult: 결과 전달 |
| Combat | DamageReceiver: 체력/무적/이벤트. Invulnerability: 원인별 만료. HitstopController: 로컬 정지. CombatEvents: 씬 이벤트 |
| Combat | AttackPresentation: 프레임 기반 Playables/Trail. CombatPresentationHooks: VFX/SFX 소비. CameraImpulseProfile: 카메라 충격. AttackPreviewObject: 편집 대상 |
| Enemy | EnemyData: 스탯/AI 패턴. HitReactionProfile: 상태별 반응. EnemyMotor: 물리. EnemyStateMachine: 상태/타이머. EnemyController: AI·피격 상태 진행 |
| Camera | CameraController: Dead Zone/Look Ahead/Soft Room/공중 추적. CameraRoom: 방 중심/경계 |
| Rank | StyleEvent, StyleEventRelay, StyleRankData, StyleRankSystem: 전투 결과를 랭크 이벤트로 변환·평가 |
| Replay | CombatSnapshot: 상태/이벤트 값. CombatRecorder: 최근 3600F Ring Buffer. HighlightSettings, HighlightSegmentSelector, HighlightReplayPlayer |
| Editor | AttackDataEditor, AttackPreviewObjectEditor, AttackEditorWindow, CombatPrototypeBuilder |
| Tests | EditMode: 입력·데이터·반응·선택기. PlayMode: 실제 Input System 및 씬/2D 물리 통합 테스트 |

Runtime, Editor, EditModeTests, PlayModeTests를 asmdef로 분리했다.
Editor/Tests 코드는 일반 Player 빌드에 포함되지 않는다.

## 프레임 처리 순서

```text
Input System callback → Reader의 에지 보관
CombatClock.Step (FixedUpdate, 1/60초)
  1. CurrentFrame 증가
  2. InputTick
     - Reader Capture → History → Parser → Buffer
     - Hitstop 종료/입력 만료 조정, Rank decay
  3. SimulationTick
     - Player State / Enemy State / Motor / Attack / Hitbox
  4. Physics2D.SyncTransforms → Physics2D.Simulate(1/60)
  5. CompletedTick
     - 실제 물리 결과의 CombatSnapshot 기록
```

Clock은 Physics2D의 Script simulation을 소유하고 파괴 시 이전 모드로 복구한다.
**실행 전투 씬에 활성 Clock은 하나만 둔다.** 다른 코드에서 Physics2D.Simulate를 중복 호출하지 않는다.
`Step()`은 테스트에서도 같은 경로를 사용한다. 기록은 이제 post-physics 시점이다.

Input System 기본 Dynamic Update 이벤트는 다음 전투 틱에 반영한다. FixedUpdate가 여러 번
진행되어도 버튼 에지는 한 번만 처리된다. 한 전투 틱 사이 같은 버튼의 여러 Press는 합쳐진다.
LMB를 누른 시점의 Shift/방향은 보존한다. 방향 더블탭의 중립은 최소 한 전투 틱에 관측되어야 한다.

## 이동과 State

```text
Idle ↔ Move
Ground → Jump → Fall → Ground (착지 Recovery 0F)
Air + Space → Jump (남은 공중 점프 사용)
Ground/Air + DoubleTapA/D → GroundDash/AirDash → Locomotion
Locomotion + 공격 커맨드 → 공통 AttackState
Attack + Jump Cancel → Jump
피격 → Hit → Locomotion / HP 0 → Dead
```

PlayerLocomotion이 1단 점프/코요테/공중 자원을 구분한다. 상승 중에는 Ground로 판정하지 않는다.
Coyote 성공은 AirJump를 소비하지 않는다. Fall에서 착지한 틱에 먼저 버퍼 명령을 확인한다.
점프 버튼 유지/해제는 높이에 관여하지 않는다.

## 공격과 피격

```text
CommandBuffer.TryConsume → PlayerCombat + Loadout → AttackState(AttackData)
  → intrinsic movement (PlayerMotor)
  → Active frame의 Hitbox overlap / Shotgun raycast
  → HitResult → DamageReceiver
  → EnemyController + HitReactionProfile / Player HitState
  → CombatEvents → Hitstop / StyleEventRelay / VFX·SFX / Camera / Recorder
```

Hitbox는 물리 충돌용 BoxCollider와 별도의 자식 Trigger다. 기본적으로 같은 공격 인스턴스가 같은
DamageReceiver를 한 번만 때린다. 여러 Hurtbox도 Receiver 기준으로 통합한다. RepeatFrames > 0이면
재타격 간격을 허용한다. 산탄총은 여러 Raycast를 거리순으로 처리하고 지형에 막힌다.
기본 피해는 대상당 한 번이며 DamagePerPellet 옵션에서 적중 Pellet 수만큼 배율을 준다.

Hitstop은 양쪽 actor의 motor simulation을 끈다. 공격 프레임과 Playables 애니메이션도 진행하지 않는다.
공통 Clock/입력/카메라/UI는 계속 진행한다. 기존 Command의 ExpireFrame을 정지 틱마다 1F 연장해
Hitstop 중 누른 Jump/공격이 정지 시간 때문에 사라지지 않게 한다. History는 계속 기록한다.

## 잡몹과 보스

| 상황 | 잡몹 | 보스 |
|---|---|---|
| Light | Hitstun | Neutral/Startup/Active에서 Damage only |
| Heavy | Knockback | 기본적으로 Stagger meter 증가 |
| Launcher | 고정 초기 Y 속도로 Launch | 기본적으로 Damage only |
| Recovery / Stagger | Hitstun 가능 | Hitstun 가능, Launcher로 항상 뜨지는 않음 |
| 공중 피격 | 중력 누적 유지 | 프로토타입 보스는 기본 Launch 불가 |

보스는 Quick / Heavy / Approach 세 AttackData를 순환한다. Heavy의 42F Startup, 60F Recovery가
회피 후 반격을 검증하는 긴 창이다. AI는 타깃을 참조하지만 플레이어는 타깃 기반 보정을 하지 않는다.

## 랭크와 리플레이

전투 코드는 Rank 점수를 직접 수정하지 않는다. Relay가 DamageDealt, EnemyKill, DifferentMove,
AirHit, TookDamage, Dodge, Punish 이벤트를 만들고 RankSystem이 튜닝 데이터를 적용한다.
Dodge는 가까운 적 Active 구간에서 Jump/Dash를 관측하고 공격 종료까지 명중하지 않았을 때
지급하는 단순 프로토타입 기준이다. Counter는 이벤트 확장점이며 패리 시스템은 없다.

Recorder는 player/enemy 위치·속도·상태·HP·Facing·공격 ID/시간, 실행 커맨드, Hit/Kill/RankDelta를 저장한다.
HighlightSelector는 기본 240F(4초)의 순 랭크 증가량을 비교하고 다양성/킬/콤보로 동점을 판정한다.
Replay는 별도 Sprite ghost에 상태를 적용한다. Clock과 실제 카메라 추적을 정지하므로 전투 로직을
다시 실행하지 않는다. 완료 시 결과 패널에서 재생/일시정지/처음부터/Skip을 사용할 수 있다.

## 사용 문서

- 기획 수치: [CombatDataGuide.md](CombatDataGuide.md)
- 판정 편집: [AttackEditorGuide.md](AttackEditorGuide.md)
- 조작과 테스트: [PrototypeControls.md](PrototypeControls.md)
- 단계별 변경/검증: [ImplementationStages.md](ImplementationStages.md)
- 한계: [KnownIssues.md](KnownIssues.md)
- 정식 개발 전 정리: [NextSteps.md](NextSteps.md)
