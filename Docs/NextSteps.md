# Next Steps — 정식 개발 전

## 우선 리팩터링

1. Player 이동/보정과 적 반응의 튜닝을 실제 rig/animation에 맞춘다. 현재 BoxCollider 크기와 공격 판정은 임시 도형 기준이다.
2. EnemyController에서 AI 의사결정, 공격 실행, 반응 상태를 분리한다. 공통 공격 실행기를 Player/Enemy가 공유할 수 있게 하되 입력 State와 AI 의도를 섞지 않는다.
3. AttackData에 안정적인 GUID, asset 참조 검증, 버전/마이그레이션 정책을 추가한다. ID 중복도 프로젝트 전체로 검사한다.
4. StageContext/actor 등록으로 고정 Enemy 배열을 대체하고 spawn/despawn의 안정적인 Replay ID를 부여한다.
5. Clock 단일 소유권, 입력/시뮬레이션/물리/기록 phase 계약을 프로젝트 수준으로 고정한다.
6. 물리/판정 쿼리와 VFX/tracer/리플레이 배열을 풀링한다. 프로파일링 결과를 기준으로 최적화한다.

## 전투 확장

- 공중/지상 공격 Animation, Trail, Hit VFX, SFX 연결.
- 공격별 체공/이동 Curve/캔슬 창을 아트와 동기화.
- 잡몹 다수전 Attack Coordinator, 접근 슬롯, 카메라 여러 타깃.
- 보스 실제 패턴 선택, 이동/공격 range, Stagger/Recovery reset 규칙 플레이테스트.
- 선택적으로 Hold/W/방향 조합 커맨드 구현. 명령은 조작 이름을 유지한다.
- Dodge/Counter를 공격 궤적과 invulnerability 사유에 근거한 정확한 이벤트로 확장.
- 피격/무적/사망 모션과 리스폰 정책 정리.

## 제작 도구

- Attack Editor에 실제 player rig/프리팹 preview, 여러 box 트랙, 이동/VFX/SFX 타임라인 추가.
- Inspector 모든 단위/validation의 공통 PropertyDrawer화.
- CSV/Spreadsheet Import/Export는 편의 도구로 추가하되 런타임 원본은 ScriptableObject 유지.
- 씬 생성기는 초기 부트스트랩용으로만 유지하고 실제 콘텐츠는 prefab/scene 저작으로 관리.

## Replay / 배포

- ghost animation/VFX replay, 보간, slow-motion marker 편집.
- 긴 기록용 sliding-window highlight 계산, 버전 있는 snapshot 직렬화.
- 정식 결과 화면과 스테이지 종료 이벤트 연결.
- 게임패드/키 리바인딩, 접근성, 해상도별 HUD 확인.
- 개발용 판정선/Debug UI를 빌드 프로파일별로 끄는 authoring 설정.
- Windows 외 목표 플랫폼에서 입력/물리/성능 검증.

자동 Facing/Position/Vertical Assist는 추가하지 않는다. 잡몹/보스 차이를 플레이어 기술 수치로
만들지 않고 적의 반응과 상태별 Punish 규칙으로 유지한다.

