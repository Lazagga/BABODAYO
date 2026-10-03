# Known Issues / 현재 제한

## 프로토타입으로 남긴 부분

- 캐릭터 몸통끼리는 통과한다. 수평 Pushbox는 미구현이다. 적 위에 올라서는 동작은 지원하지 않는다.
- 공격 방향은 커서 좌우만 사용한다. 산탄총 자유각 조준은 미구현이다. 상세 기술 현황은 [AttackInventory](AttackInventory.md).

- 사각형 캐릭터와 임시 판정선이다. 최종 Animation/VFX/SFX asset은 없다. hook과 null-safe 실행 경로만 제공한다.
- 카메라는 Cinemachine 없이 구현한 가벼운 2D 추적이다. 복잡한 다중 룸 전환, 카메라 연출 우선순위는 없다.
- Dummy/Fodder/Boss 각 1체, 간단한 접근/순환 패턴 AI다. 다수 적 Attack Coordinator, 경로 탐색, 절벽 회피는 없다.
- Boss 반복 패턴과 잡몹 체력은 검증용 값이다. 핵앤슬래시/격투게임의 최종 타격감·난도는 플레이테스트로 조정해야 한다.
- EnemyStateMachine은 프로토타입 크기의 상태 enum/타이머다. AI가 커지면 행동 모듈을 분리한다.
- Dodge 보너스는 가까운 Active 중 Jump/Dash 관측 + 공격 Whiff라는 단순 휴리스틱이다.
  정확한 공격 궤적 회피 판정이나 패리/Counter 시스템은 미구현이다.
- 이동/보정은 회전 없는 BoxCollider2D, 정적 Tilemap을 기준으로 검증했다. 이동 발판/일방통행/경사 전용 로직은 없다.
- Ledge Forgiveness는 작은 센서 여유 안에서 Y 하강을 멈추는 방식이다. 강한 수직/수평 Snap은 없다.
- 근접 Hitbox는 지형 occlusion을 검사하지 않는다. Shotgun ray는 벽에 막힌다.
- Hitbox는 OverlapBoxAll, shotgun은 RaycastAll을 사용한다. 대규모 전투 전 NonAlloc/풀링을 검토한다.
- Game-view의 빨간 Hitbox 외곽선은 여러 box 중 마지막 것만 표시한다. 판정 자체는 모든 box를 처리한다.
- Shotgun 데이터의 Pellet 수는 확장 가능하지만 샘플 씬의 tracer LineRenderer는 7개다.
  7개를 넘기면 추가 LineRenderer를 Inspector에 연결해야 모두 보인다.
- Shotgun의 DamagePerPellet은 피해만 배율 적용하며 Hitstun/Knockback/Hitstop은 대상당 한 번이다.
- 실제 입력의 서브프레임 순서를 모두 재현하지 않는다. 한 틱의 중복 버튼 Press는 합치며,
  더블탭 중립은 한 전투 틱에 보여야 한다. 기본 60Hz에서는 일반 조작용으로 충분하지만
  낮은 렌더 FPS/고급 커맨드 확장 시 이벤트 타임스탬프 기록을 검토한다.
- 추가 특수기 6종은 구현되어 있으나 최종 애니메이션/효과와 밸런스 튜닝은 필요하다. [AttackInventory](AttackInventory.md) 참고.
- Input System은 로컬 키보드/마우스 1인용이다. 컨트롤러 페어링/리바인딩 저장 UI는 없다.

## Replay / Editor 제한

- Replay는 상태 스냅샷 기반 Sprite ghost다. 물리/피해/AI를 재실행하지 않는다.
- 애니메이션 ID/시간은 기록하지만 임시 ghost는 위치/Facing/사망 색상만 재생한다.
  실제 rig clip 연결, VFX/SFX 재생, 보간, 적 생성/파괴 ID 매핑은 후속 작업이다.
- 현재 Recorder는 고정 enemy 배열과 최근 3600F(60초) Ring Buffer를 사용한다. 파일 저장/로드/영상 녹화는 없다.
- Highlight 선택기는 모든 후보 구간을 순회하는 간단한 구현이다. 더 긴 기록에서는 sliding aggregate로 최적화한다.
- 결과 화면은 F2 또는 UI 버튼으로 호출하는 training overlay다. 정식 스테이지 결과 flow는 없다.
- 기본 Attack Editor preview에는 rig이 없다. AnimationClip의 실제 바인딩 확인에는 맞는 계층을 가진 Preview Object가 필요하다.
- Preview Handles는 1배 스케일/회전 없는 preview를 기준으로 한다.
- Undo와 dirty 처리를 지원하지만, 최종 disk 저장은 Unity 프로젝트 저장 작업에 따른다.

## 운영 규약

- Scene당 CombatClock 하나가 2D 물리 수동 시뮬레이션을 소유한다. 다른 시스템이 중복 Simulate하면 안 된다.
- Input/Simulation 이벤트 구독 중 던지는 예외는 후속 처리를 중단한다. 예외 격리 프레임워크는 넣지 않았다.
- 생성 도구는 기존 CombatTestScene이 있으면 덮어쓰지 않는다. 이미 튜닝한 asset/scene을 보존한다.
- ProjectSettings에는 사용자가 작업 전부터 수정한 항목이 있다. 기존 변경을 되돌리지 않았다.
