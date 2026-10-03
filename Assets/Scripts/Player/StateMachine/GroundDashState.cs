using UnityEngine;
namespace Babodayo.Player.StateMachine
{
    public class GroundDashState : PlayerState
    {
        private readonly int direction;
        private int elapsed;
        protected virtual bool Air => false;
        public GroundDashState(PlayerController p,int direction):base(p) { this.direction=direction; }
        public override void Tick()
        {
            var data=Motor.Data;
            int startup=Air ? data.AirDashStartup : data.GroundDashStartup;
            int duration=Air ? data.AirDashDuration : data.GroundDashDuration;
            if (Player.Locomotion.TryJump()) return;
            if ((Player.Commands.Remaining(Babodayo.Input.CombatCommand.DoubleTapA_LMB,Player.CurrentFrame)>0 ||
                 Player.Commands.Remaining(Babodayo.Input.CombatCommand.DoubleTapD_LMB,Player.CurrentFrame)>0) &&
                (Player.CombatAction?.Invoke() ?? false)) return;
            Motor.SetHorizontalVelocity(elapsed>=startup ? direction*data.DashSpeed : 0);
            Motor.Gravity(Air && data.AirDashLockY,data.AirDashY);
            Motor.CorrectCorners(true,direction);
            elapsed++;
            if (elapsed>=startup+duration) Player.ReturnToLocomotion();
        }
        public override void Exit() => Motor.SetHorizontalVelocity(Mathf.Clamp(Motor.Velocity.x,-Motor.Data.Speed,Motor.Data.Speed));
    }
}
