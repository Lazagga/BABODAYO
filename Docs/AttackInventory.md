# 현재 공격 구현 목록

| 입력 / 상태 | AttackData | 구현 |
|---|---|---|
| 지상 LMB 반복 | LMB_01 / 02 / 03 | 선입력 3타, 공격 이동 |
| 공중 LMB 반복 | Air_LMB_01 / 02 / 03 | 3타, Y 속도 잠금 |
| Shift + LMB | Shift_LMB | 강공격. 공중에서도 같은 데이터 사용 |
| 지상 Shift + S + LMB | Shift_S_LMB | 고정 초기 Y 속도 Launcher |
| 공중 Shift + S + LMB | Air_Shift_S_LMB | 내려찍기, 지형 착지 Recovery |
| 지상 / 공중 RMB | RMB / Air_RMB | 확산 히트스캔 산탄총 |

플레이어 공격 데이터 17개. 적 공격은 MudCharge, BossQuick, BossHeavy, BossApproach 4개.
DummyAttack은 테스트용이며 플레이어 커맨드에 연결되지 않는다.

## 추가 구현된 기술

| 입력 | 데이터 / 동작 |
|---|---|
| 지상 Shift + S + LMB 10F 유지 | Shift_S_LMB_Hold: 적과 플레이어가 초기 Y 속도 12로 상승 |
| 지상 Shift + A/D + LMB | Shift_Direction_LMB: Active 동안 3 Unit 돌진 |
| W + LMB | W_LMB: 위쪽으로 긴 Hitbox의 찌르기, 지상/공중 가능 |
| A,A 또는 D,D 뒤 LMB | DoubleTap_LMB: Active 동안 2.4 Unit 이동, 지상/공중 가능 |
| 공중 A/D + LMB | Air_Direction_LMB: 수평 10 / 하강 16 Unit/s, 착지 Recovery 10F |
| 공중 Shift + A/D + LMB | Air_Shift_Direction_LMB: Y 속도 0, Active 동안 4 Unit 관통 |

Shift+S+LMB는 10F 전에 떼면 기존 Launcher/내려찍기, 유지하면 Hold 커맨드를 한 번 생성한다.
따라서 일반 Launcher는 버튼을 떼는 시점에 시작한다. 공중 Hold는 기존 수직 내려찍기로 매핑한다.
더블탭 판정은 10F, 두 번째 탭 뒤 LMB 유예는 6F(두 번째 탭 프레임 포함)다.
이미 대시 중이어도 대시 공격으로 전환된다. 같은 프레임에는 대시와 공격을 중복 예약하지 않는다.
우선순위는 Shift+S > Shift+좌우 > Shift > W > 더블탭+LMB > 좌우+LMB > LMB다.
지상 좌우+LMB는 기본 콤보이며 공중에서만 대각선 내려꽂기로 해석한다.
공중 기본 3타를 쓰려면 A/D를 떼고 LMB를 누른다. 방향키는 기술 선택, 마우스는 공격 좌우를 결정한다.
위 값은 초기 튜닝이며 AttackData와 PlayerController의 Parser Settings에서 조절한다.
새 공격도 Jump/Attack/Dash/Shoot Cancel과 동일한 적 반응 프로필을 사용한다.
애니메이션과 최종 VFX는 기존처럼 임시이며, 게임플레이와 판정은 구현되어 있다.

## 방향 및 물리 정책

InputReader가 마우스 화면 좌표를 InputFrame에 기록하고 PlayerFacing이 명시적으로 연결된
카메라로 좌우 방향을 계산한다. 대기와 공격은 커서, 비공격 이동은 이동 방향, 대시는 대시 방향이다.
공격 진입 시 커서 방향으로 즉시 갱신하며 공격 중에도 갱신한다. 커서가 정확히 중심이면 이전 방향 유지.
산탄총은 좌우 수평 사격이며 커서 높이에 따라 조준하지 않는다. 흰 표시가 캐릭터의 앞쪽이다.

PlayerMotor/EnemyMotor의 ActorBodyLayers 기본값은 Layer 8이다. 몸통 Collider의 excludeLayers에
적용하므로 캐릭터끼리 통과하고 서로의 머리 위에 서지 않는다. Layer 0 지형 충돌,
Layer 2 Hurtbox에 대한 공격 쿼리는 유지한다. 새 캐릭터도 몸통은 Layer 8에 배치한다.
수평 Pushbox는 아직 없다. 보스도 동일한 몸통 정책을 사용한다.

격투게임 개발 사례는 밀림 Pushbox와 Hitbox/Hurtbox를 분리한다.
[개발자의 구현 설명](https://ayoublamdaghri.wordpress.com/2019/02/03/my-fighting-game-protorype-part-1-hitbox-hurtbox-pushbox-camera/)
이번 플랫폼 액션에서는 이를 참고해 몸통의 수직 지지 자체를 제거했다.

## 재현 테스트

1. 왼쪽에 커서를 두고 D 이동: 오른쪽을 보며 이동. LMB/RMB 입력 즉시 왼쪽 공격.
2. 공격 중 커서를 반대로 이동: Hitbox 방향 반전. 동일 인스턴스 중복 피해 방지는 유지.
3. Dummy 바로 위에서 일반 낙하/내려찍기: 적을 통과해 지형 착지, 다시 점프 가능.
4. 적 몸통을 통과해도 근접 공격과 산탄총 피해 정상 발생.
