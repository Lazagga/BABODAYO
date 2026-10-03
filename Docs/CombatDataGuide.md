# Combat Data Guide — 기획자용

게임플레이 수치는 `Assets/GameData`의 ScriptableObject를 Inspector에서 수정한다.
PlayMode 중 변경은 asset에 남을 수 있으므로 정지 상태에서 편집하는 것을 권장한다.
소스 코드나 JSON을 바꿀 필요가 없다.

## 이동: Settings/PlayerMovement.asset

| 필드 | 초기값 | 단위/의미 |
|---|---:|---|
| Speed | 7 | Unit/s |
| Acceleration / ReverseAcceleration / Deceleration | 90 / 150 / 110 | Unit/s² |
| AirControl | 0.8 | 지상 가속도의 비율 |
| JumpVelocity / DoubleJumpVelocity | 12 / 11 | 고정 초기 Unit/s |
| AirJumps / AirDashes | 1 / 1 | 착지 시 복구되는 공중 횟수 |
| CoyoteFrames | 6 | 발판 이탈 후 6F 이내는 1단 점프 |
| Gravity | 30 | Unit/s² |
| ApexGravity / FallGravity | 0.7 / 1.65 | 기본 중력 배율 |
| ApexThreshold | 1 | 절대 Y 속도 <= 1일 때 정점 구간 |
| GroundDashStartup / Duration | 3 / 12 | 준비/이동 프레임 |
| AirDashStartup / Duration | 5 / 12 | 준비/이동 프레임 |
| DashSpeed | 16 | Unit/s |
| AirDashLockY / AirDashY | true / 0 | 수평 대시. false이면 중력 유지 |
| CornerCorrection / DashCornerCorrection | 0.15 / 0.2 | Tile = Unit |
| JumpInvulnerabilityFrames | 4 | 0으로 끌 수 있으며 Jump Cancel과 독립 |

Jump Buffer 8F, 공격 버퍼 6F, 기타 버퍼 6F, Double Tap 10F는 PlayerController Inspector의
Buffer/Parser Settings에서 조절한다. Hitstop 중 버퍼 수명은 보존한다.
Ground Sensor의 LedgeForgiveness는 좌우 각각 0.08 Unit, ProbeDistance는 0.05 Unit이다.
큰 Snap, Step-Up, Variable Jump, 독립 Fast Fall은 없다.

## 공격 asset

Assets > Create > Babodayo > Attack으로 새 공격을 만들고 고유 ID를 지정한다.
`Attacks/PlayerLoadout.asset`에 연결해야 실제 커맨드로 실행된다.

추가 기술은 RisingLauncher / Rush / UpThrust / DashAttack / DiagonalDive / AirPierce 슬롯을 사용한다.
Movement.SelfLaunch는 첫 Active 프레임에 SelfLaunchVelocity를 한 번 적용한다.
Movement.DiveHorizontalSpeed는 Dive 중 수평 속도이며 마우스 Facing에 따라 반전한다.
돌진/관통의 Movement.Curve는 Active 동안만 누적 이동량이 증가하도록 설정되어 있다.
HoldFrames(10F)와 DashAttackFrames(6F)는 PlayerController의 Parser Settings에서 조절한다.
메뉴 Tools > Combat > Add Extended Attacks는 누락된 데이터/연결만 추가하고 기존 튜닝을 유지한다.
전체 조작과 새 기술 수치는 [AttackInventory](AttackInventory.md)를 참고한다.

| asset | Startup / Active / Recovery | Damage | Hitstop | 전진량 |
|---|---|---:|---:|---:|
| LMB_01 | 6 / 4 / 14 | 10 | 4F | 0.12 |
| LMB_02 | 7 / 4 / 14 | 12 | 4F | 0.18 |
| LMB_03 | 9 / 5 / 18 | 18 | 5F | 0.26 |
| Shift_LMB | 18 / 5 / 26 | 28 | 9F | 0.22 |
| Shift_S_LMB | 8 / 5 / 20 | 14 | 8F | 0.08 |
| Air_LMB_01 | 5 / 5 / 12 | 9 | 4F | 0.08 |
| Air_LMB_02 | 5 / 5 / 12 | 10 | 4F | 0.10 |
| Air_LMB_03 | 7 / 5 / 14 | 15 | 5F | 0.16 |
| Air_Shift_S_LMB | 8 / 착지 대기 / 9 | 24 | 10F | 0 |
| RMB / Air_RMB | 5 / 1 / 16 | 14 | 5F | 0 |

초기 값은 검증용이며 밸런스 확정값이 아니다. 이동량은 적의 존재/종류와 무관하게 적용된다.
Curve는 공격 전체의 누적 전진 진행도이며 0~1 끝점을 기준으로 정규화한다. 벽에는 실제로 막힌다.
공격 종료/캔슬 시 남은 전진을 강제 적용하지 않는다. 즉 캔슬 시점에 따라 실행한 거리까지만 이동한다.

## 프레임과 Cancel

프레임은 **0부터 시작**한다. 예를 들어 Startup 6이면 0~5가 준비, Active 4이면 6~9가 공격이다.
Hitbox Start/End, Cancel Start/End는 양 끝 포함이다. 전체 길이 밖의 값을 쓰지 않는다.
기본 asset의 공격/대시/사격 캔슬은 첫 Active 프레임 다음부터 열린다.
첫 Active와 같은 프레임에 창을 열면 선입력으로 타격 전에 다음 공격으로 넘어갈 수 있으므로 의도한 경우에만 사용한다.

- Attack: 창 안에서 다음 LMB 콤보 또는 다른 sword command.
- Dash: 창 안에서 대시. 공중 횟수 제한을 그대로 적용.
- Shoot: 창 안에서 RMB. Shotgun의 Attack 옵션으로 다시 sword로 전환 가능.
- Jump: 현재 프로토타입은 공격의 모든 프레임에서 허용 여부를 적용한다. 별도 Cancel Start/End 제한이 없다.
- OnHitOnly: 모든 캔슬(점프 포함)에 명중 확인 필요.
- 마지막 LMB 콤보 다음 LMB는 같은 공격 내에서 1타로 무한 순환하지 않는다. 종료 후 새 콤보로 시작한다.

## 피격과 공중 콤보

Hit.Knockback은 목표 속도 기반이며 X는 Facing을 따른다.
Launch가 가능한 반응이면 Y를 LaunchVelocity=12로 설정한다(힘 누적 방식 아님).
플레이어의 1단 JumpVelocity도 12로 시작하며 자동 높이 보정은 하지 않는다.

공중 LMB는 LockY=true, YVelocity=0, AirControl=0.2이다.
적은 공중 Hitstun 중에도 중력 30 × AirGravityMultiplier를 받는다.
AirGravity 기본: 1.0, 1.0, 1.1, 1.25, 1.45, 1.7, 2.0; 착지하면 초기화한다.
공중 LMB의 Knockback Y=3은 피격 시의 상승 속도 변화다. 중력 누적 자체는 초기화하지 않는다.

Dive는 Startup 후 Y=-20으로 하강한다. 지면 센서가 착지를 확인하면 착지 판정을 한 번 평가하고
9F Recovery에 진입한다. 무한 낙하 방지용 최대 descent 180F가 있다.
Dive의 낙하 중 판정과 착지 판정은 같은 공격 인스턴스라 같은 대상에게 중복 피해를 주지 않는다.

## 산탄총

Pellets=7, Range=4 Unit, Spread=28도. 지형에 막히는 히트스캔이다.
DamagePerPellet=false이면 대상당 한 번 피해, true이면 맞은 Pellet 수를 피해 배율로 사용한다.
반응/Hitstop은 대상당 한 번이다. 노란 선으로 실제 ray의 끝점을 확인할 수 있다.
Air_RMB 기본은 체공하지 않는다. 원하면 Movement.LockY를 켠다.

## 적

`Enemies/Dummy.asset`: HP 180, AI 꺼짐, 마지막 피격 후 180F에 자동 회복(사망 포함).
`MudFodder.asset`: HP 55, 접근 속도 1.8, 30F 예고의 돌진 공격.
`PrototypeBoss.asset`: HP 450, 3패턴, 공격 후 35F Neutral 대기.

FodderProfile/BossProfile을 바꾸어 반응을 조절한다.
보스 Heavy의 StaggerDamage는 플레이어 Heavy asset에 25, 임계치는 BossProfile에 40,
Stagger는 90F다. Recovery/Stagger에서만 경직을 허용하는 정책을 변경할 수 있다.
플레이어의 DamageReceiver는 피격 후 무적 45F이며 점프 무적과 원인별로 관리한다.

## Rank / Highlight

StyleRank.asset: D/C/B/A/S/SS/SSS 임계값 0/100/250/450/700/1000/1400.
Damage ×2, Kill +40, DifferentMove +15, AirHit +10, Dodge/Punish +20,
피격 -80, 시간당 감소는 초당 5. 반복 배율 1/.7/.4/.2/.1은 랭크에만 적용한다.

Highlights.asset: 240F(4초), 최소 순 증가 20, 동점 허용차 5.
동점 보조 가중치: 다양한 기술 3, 처치 2, 최대 콤보 1.
히트 후 8F 구간은 0.4배 속도로 재생한다.
