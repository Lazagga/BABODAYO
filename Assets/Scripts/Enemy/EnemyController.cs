using Babodayo.Combat;
using Babodayo.Core;
using UnityEngine;
namespace Babodayo.Enemy
{
    public sealed class EnemyController : MonoBehaviour
    {
        [Tooltip("Shared scene clock.")] public CombatClock Clock;
        [Tooltip("Enemy stats and reaction policy.")] public EnemyData Data;
        [Tooltip("Enemy physics owner.")] public EnemyMotor Motor;
        [Tooltip("Explicit player target; no object searches.")] public Transform Target;
        [Tooltip("Owned health component.")] public DamageReceiver Receiver;
        [Tooltip("Owned attack hitbox.")] public Hitbox Hitbox;
        [Tooltip("Optional attack presentation.")] public AttackPresentation Presentation;
        [Tooltip("Temporary body sprite for phase colors.")] public SpriteRenderer Visual;
        public EnemyStateMachine States { get; }=new EnemyStateMachine();
        public int Facing { get; private set; }=1;
        public int AttackFrame { get; private set; }
        public AttackData CurrentAttack { get; private set; }
        public int AirHits { get; private set; }
        public float GravityMultiplier => Data.Reactions.GravityFor(AirHits);
        public float StaggerMeter { get; private set; }
        public bool LastHitWasPunish { get; private set; }
        private int pattern,lastHit=-10000;
        private Vector2 spawn;
        private Color baseColor;
        private Babodayo.Player.PlayerController targetPlayer;
        private bool evasionObserved;
        private void Start()
        {
            spawn=transform.position; baseColor=Visual!=null ? Visual.color : Color.white;
            if(Target!=null) targetPlayer=Target.GetComponent<Babodayo.Player.PlayerController>();
            Receiver.MaxHP=Data.HP; Receiver.ResetHealth();
            Receiver.Received+=Receive;
            Receiver.Hitstop?.Bind(Motor.Freeze);
            Clock.SimulationTick+=Tick;
            ResetState();
        }
        private void OnDestroy() { if(Clock!=null) Clock.SimulationTick-=Tick; if(Receiver!=null) Receiver.Received-=Receive; }
        private void ResetState() => States.Change(Data.Boss ? EnemyState.Neutral : EnemyState.Idle,Data.IdleFrames);
        private void Receive(HitResult hit)
        {
            lastHit=hit.Frame;
            LastHitWasPunish=States.Current==EnemyState.AttackRecovery;
            if(Receiver.Dead) { CancelAttack(); States.Change(EnemyState.Dead); Motor.SetHorizontal(0); return; }
            HitReaction reaction=Data.Reactions.Resolve(hit.Attack.Kind,States.Current);
            if(reaction==HitReaction.StaggerDamage)
            {
                StaggerMeter+=hit.Attack.Hit.StaggerDamage;
                if(StaggerMeter<Data.Reactions.StaggerThreshold) return;
                StaggerMeter=0; CancelAttack(); Motor.SetHorizontal(0);
                States.Change(EnemyState.Stagger,Data.Reactions.StaggerFrames); return;
            }
            if(reaction==HitReaction.DamageOnly) return;
            CancelAttack();
            bool launch=reaction==HitReaction.Launch;
            if(!Motor.Grounded || launch) AirHits++;
            Motor.SetVelocity(new Vector2(hit.Knockback.x,launch ? hit.LaunchVelocity : Motor.Grounded ? hit.Knockback.y : Motor.Velocity.y+Mathf.Max(0,hit.Knockback.y)));
            States.Change(launch ? EnemyState.Launched : !Motor.Grounded ? EnemyState.AirHit : EnemyState.HitStun,hit.HitstunFrames);
        }
        private void CancelAttack() { CurrentAttack=null; Hitbox?.Disable(); Presentation?.End(); }
        private void Tick(int frame)
        {
            if(Receiver.Hitstop!=null && Receiver.Hitstop.IsStopped) return;
            Motor.Sample();
            if(Motor.Grounded) AirHits=0;
            Motor.Gravity(Data.Gravity*GravityMultiplier);
            if(Data.AutoHealFrames>0 && frame-lastHit>=Data.AutoHealFrames && Receiver.HP<Receiver.MaxHP)
            { Receiver.ResetHealth(); Motor.Teleport(spawn); CancelAttack(); ResetState(); }
            if(Receiver.Dead) { Paint(); return; }
            if(States.Current==EnemyState.Launched || States.Current==EnemyState.AirHit)
            {
                if(Motor.Grounded) States.Change(EnemyState.Knockdown,Data.KnockdownFrames);
                else if(States.Advance()) ResetState();
                Paint(); return;
            }
            if(States.Current==EnemyState.HitStun || States.Current==EnemyState.Knockdown || States.Current==EnemyState.Stagger)
            {
                if(Motor.Grounded) Motor.SetHorizontal(Mathf.MoveTowards(Motor.Velocity.x,0,35*CombatClock.FrameDuration));
                if(States.Advance()) ResetState();
                Paint(); return;
            }
            if(CurrentAttack!=null) { TickAttack(frame); Paint(); return; }
            if(!Data.AI || Target==null) { Motor.SetHorizontal(0); Paint(); return; }
            if(!Motor.Grounded) { Paint(); return; }
            float dx=Target.position.x-transform.position.x;
            if(Mathf.Abs(dx)>Data.AggroRange) { Motor.SetHorizontal(0); Paint(); return; }
            Facing=dx<0 ? -1 : 1;
            if(!States.Advance()) { Motor.SetHorizontal(0); Paint(); return; }
            if(Mathf.Abs(dx)>Data.AttackRange)
            { States.Change(EnemyState.Approach); Motor.SetHorizontal(Facing*Data.MoveSpeed); }
            else if(Data.Attacks!=null && Data.Attacks.Length>0)
            {
                CurrentAttack=Data.Attacks[pattern++%Data.Attacks.Length]; AttackFrame=0; Motor.SetHorizontal(0);
                evasionObserved=false;
                Hitbox.Begin(Receiver,CurrentAttack,Facing); Presentation?.Begin(CurrentAttack);
                Receiver.Events?.PublishSwing(CurrentAttack,transform,Facing);
                States.Change(EnemyState.AttackStartup);
            }
            Paint();
        }
        private void TickAttack(int frame)
        {
            var phase=CurrentAttack.Frames.Phase(AttackFrame);
            if(phase==AttackPhase.Active && targetPlayer!=null && !targetPlayer.GetComponent<DamageReceiver>().Dead &&
                Mathf.Abs(Target.position.x-transform.position.x)<=Data.AttackRange &&
                (targetPlayer.States.Current is Babodayo.Player.StateMachine.JumpState ||
                 targetPlayer.States.Current is Babodayo.Player.StateMachine.GroundDashState))
                evasionObserved=true;
            States.Change(phase==AttackPhase.Startup ? EnemyState.AttackStartup :
                phase==AttackPhase.Active ? EnemyState.AttackActive : EnemyState.AttackRecovery);
            Motor.SetHorizontal(phase==AttackPhase.Active ? Facing*CurrentAttack.Movement.Distance/(Mathf.Max(1,CurrentAttack.Frames.Active)*CombatClock.FrameDuration) : 0);
            Hitbox.Evaluate(AttackFrame,frame); Presentation?.Sample(AttackFrame);
            AttackFrame++;
            if(AttackFrame>=CurrentAttack.Frames.Total)
            {
                if(evasionObserved && !Hitbox.ConfirmedHit) Receiver.Events?.PublishDodge(frame);
                CancelAttack(); Motor.SetHorizontal(0); ResetState();
            }
        }
        private void Paint()
        {
            if(Visual==null) return;
            Visual.color=Receiver.Dead ? Color.gray : States.Current==EnemyState.AttackStartup ? Color.yellow :
                States.Current==EnemyState.AttackActive ? Color.red : States.Current==EnemyState.AttackRecovery ? new Color(.4f,.6f,1) :
                States.Current==EnemyState.Stagger || States.Current==EnemyState.HitStun ? Color.white : baseColor;
        }
        public void ResetForTraining()
        { Receiver.ResetHealth(); Motor.Teleport(spawn); StaggerMeter=0; AirHits=0; CancelAttack(); ResetState(); }
    }
}
