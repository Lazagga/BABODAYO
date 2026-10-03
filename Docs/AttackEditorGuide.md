# Attack Editor Guide — 기획/아트용

## Inspector

Project 창에서 AttackData asset을 선택한다. 다음 Foldout을 펼쳐 수정한다.

- Gameplay: Frames, Hit, Movement, Cancel, Shotgun
- Hitbox: 프레임 구간, 로컬 Offset, Size, RepeatFrames
- Animation: 선택적인 AnimationClip
- VFX: Swing, Hit, Trail, CameraImpulseProfile
- Audio: Swing, Hit SFX

프레임 단위는 F, 위치/크기/이동은 Tile=Unit, 속도는 Unit/s다.
음수 Startup/Recovery, Active <= 0, 역전/범위 밖 Cancel, 범위 밖 Hitbox,
잘못된 Size에 HelpBox를 표시한다. Hitbox가 Active 밖이면 Warning이며 런타임에서는 Active로 제한된다.
PlayerLoadout Inspector에서도 필수 공격 누락을 검사한다.

## Window > Combat > Attack Editor

1. 왼쪽 목록에서 공격을 선택한다. 새 asset이 보이지 않으면 Refresh attacks.
2. 오른쪽 Inspector에서 데이터를 수정한다.
3. Frame Slider, < / >, Play / Pause로 프레임을 선택한다.
4. Timeline의 노랑은 Startup, 빨강은 Active, 파랑은 Recovery, 녹색은 Cancel Window다.
5. 타임라인을 클릭해도 현재 프레임이 바뀐다.
6. Create / select Scene preview를 눌러 Scene 편집용 객체를 만든다.

창의 Scene preview는 임시 객체이며 씬에 저장하지 않는다. 창을 닫거나 Remove Scene preview로 제거한다.
AttackData 값은 실제 asset에 저장되므로 임시 preview를 없애도 유지된다.

## Scene View Hitbox

AttackPreviewObject를 선택하고 Active 안의 Hitbox 프레임으로 이동한다.
빨간 판정의 Position Handle로 위치를 이동하고 Box Handle로 크기를 바꾼다.
Facing Left 체크는 표시만 좌우 반전하며 저장 값은 오른쪽 기준의 로컬 좌표다.
Undo/Redo를 지원하고 변경 시 AttackData를 dirty 처리한다. 프로젝트 저장(Ctrl+S)으로 디스크 저장한다.

청록색은 기준 Hurtbox 표시다. 실제 런타임 Hurtbox도 청록색, Hitbox는 빨간색이다.
여러 Hitbox 구간을 만들 수 있지만 Game-view의 임시 빨간 외곽선은 마지막 평가 box만 표시한다.
실제 판정은 모든 box를 평가한다.

기본 preview와 보정 쿼리는 회전하지 않은 1배 스케일의 2D 객체를 기준으로 한다.
비균일 스케일/회전 rig을 쓰기 전에는 편집 좌표 변환을 확장한다.

## Animation / 연출 연결

AttackData.Animation.Clip이 있으면 런타임 AttackPresentation이 Playables로 combat frame에 맞춰 샘플링한다.
Hitstop에는 공격 프레임이 진행하지 않아 애니메이션도 멈춘다.
Editor 창에도 AnimationMode 샘플링 연결점이 있다. 기본 preview는 rig이 없는 임시 객체이므로
실제 아트의 바인딩을 보려면 동일한 자식 구조/Animator를 가진 AttackPreviewObject를 사용한다.

Swing/Hit VFX와 SFX는 CombatEvents를 듣는 CombatPresentationHooks가 실행한다.
Trail은 AttackPresentation이 공격 동안 소유한다. 모든 reference는 null이어도 정상 동작한다.
공격 로직에 특정 prefab 이름/리소스 경로를 넣을 필요가 없다.

현재 제공된 아트는 사각형 sprite와 판정선이며 최종 캐릭터 animation, VFX, SFX는 비어 있다.

