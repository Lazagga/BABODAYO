using Babodayo.Combat;
using Babodayo.Core;
using UnityEngine;
namespace Babodayo.Player.StateMachine
{
    public sealed class AttackState : PlayerState
    {
        private readonly PlayerCombat combat;
        public AttackData Data { get; }
        public int AttackFrame { get; private set; }
        public int ComboIndex { get; }
        public bool Air { get; }
        public AttackPhase Phase => Data.Frames.Phase(AttackFrame);
        private int facing => Player.Facing;
        private int descentFrames;
        private bool landed,shot,selfLaunched;
        public override string Name => "Attack";
        public AttackState(PlayerController p,PlayerCombat combat,AttackData data,int combo,bool air):base(p)
        { this.combat=combat; Data=data; ComboIndex=combo; Air=air; }
        public override void Enter()
        {
            combat.Hitbox.Begin(combat.Receiver,Data,facing);
            combat.Presentation?.Begin(Data);
            combat.Receiver.Events?.PublishSwing(Data,Player.transform,facing);
        }
        public override void Tick()
        {
            combat.Hitbox.SetFacing(facing);
            bool hit=combat.Hitbox.ConfirmedHit;
            if((!Data.Cancel.OnHitOnly || hit) && Data.Cancel.Jump && Player.Locomotion.TryJump()) return;
            if(Data.Cancel.Window(AttackFrame,hit))
            {
                if(Data.Cancel.Dash && Player.Locomotion.TryDash()) return;
                if(combat.TryAttack(ComboIndex<0 ? 0 : ComboIndex+1,Data.Cancel.Attack,Data.Cancel.Shoot)) return;
            }
            if(Data.Movement.Dive && AttackFrame>=Data.Frames.Startup && !landed)
            {
                Motor.SetHorizontalVelocity(Data.Movement.DiveHorizontalSpeed*facing);
                if(Motor.Grounded)
                {
                    landed=true; Motor.SetVerticalVelocity(0);
                    combat.Hitbox.Evaluate(AttackFrame,Player.CurrentFrame,true);
                    combat.Receiver.Events?.PublishImpact(Player.transform.position,Data);
                    AttackFrame=Data.Frames.Startup+Data.Frames.Active;
                }
                else
                {
                    Motor.SetVerticalVelocity(-Data.Movement.DiveSpeed);
                    combat.Hitbox.Evaluate(Data.Frames.Startup,Player.CurrentFrame);
                    if(++descentFrames<Data.Movement.MaxDiveFrames) return;
                    landed=true; AttackFrame=Data.Frames.Startup+Data.Frames.Active;
                }
            }
            float total=Mathf.Max(1,Data.Frames.Total);
            var curve=Data.Movement.Curve;
            float start=curve.Evaluate(0), range=curve.Evaluate(1)-start;
            float before=Mathf.Abs(range)<.0001f ? AttackFrame/total : (curve.Evaluate(AttackFrame/total)-start)/range;
            float after=Mathf.Abs(range)<.0001f ? (AttackFrame+1)/total : (curve.Evaluate((AttackFrame+1)/total)-start)/range;
            float velocity=(after-before)*Data.Movement.Distance/CombatClock.FrameDuration*facing;
            Motor.SetHorizontalVelocity(velocity + (Air ? Player.MoveInput*Motor.Data.Speed*Data.Movement.AirControl : 0));
            Motor.Gravity(Data.Movement.LockY && !landed,Data.Movement.YVelocity);
            if(Data.Movement.SelfLaunch && !selfLaunched && Phase==AttackPhase.Active)
            { Motor.SetVerticalVelocity(Data.Movement.SelfLaunchVelocity); selfLaunched=true; }
            if(Data.Kind==AttackKind.Shotgun && Phase==AttackPhase.Active && !shot)
            { shot=true; combat.Hitbox.Shoot(Player.CurrentFrame); }
            else combat.Hitbox.Evaluate(AttackFrame,Player.CurrentFrame);
            combat.Presentation?.Sample(AttackFrame);
            AttackFrame++;
            if(AttackFrame>=Data.Frames.Total) Player.ReturnToLocomotion();
        }
        public override void Exit() { combat.Hitbox.Disable(); combat.Presentation?.End(); Motor.StopHorizontal(); }
    }
}
