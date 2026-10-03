using Babodayo.Input;
using Babodayo.Player.StateMachine;
using UnityEngine;
namespace Babodayo.Player
{
    public sealed class PlayerLocomotion
    {
        private readonly PlayerController player;
        private int lastGroundFrame=-10000;
        private bool groundJumpUsed;
        public int AirJumpsRemaining { get; private set; }
        public int AirDashesRemaining { get; private set; }
        public int CoyoteRemaining => groundJumpUsed ? 0 :
            Mathf.Max(0,player.Motor.Data.CoyoteFrames-(player.CurrentFrame-lastGroundFrame));
        public PlayerLocomotion(PlayerController player) { this.player=player; }
        public void Sample()
        {
            var motor=player.Motor;
            if (motor.Ground.Sample(motor.Velocity.y))
            {
                lastGroundFrame=player.CurrentFrame;
                groundJumpUsed=false;
                AirJumpsRemaining=motor.Data.AirJumps;
                AirDashesRemaining=motor.Data.AirDashes;
            }
        }
        public bool TryJump()
        {
            bool ground=player.Motor.Grounded || (!groundJumpUsed &&
                player.CurrentFrame-lastGroundFrame<=player.Motor.Data.CoyoteFrames);
            if (!ground && AirJumpsRemaining<=0) return false;
            if (!player.Consume(CombatCommand.Space)) return false;
            groundJumpUsed=true;
            if (!ground) AirJumpsRemaining--;
            player.Motor.SetVerticalVelocity(ground ? player.Motor.Data.JumpVelocity : player.Motor.Data.DoubleJumpVelocity);
            player.States.Change(new JumpState(player));
            Jumped?.Invoke(player.Motor.Data.JumpInvulnerabilityFrames);
            return true;
        }
        public event System.Action<int> Jumped;
        public bool TryDash()
        {
            bool ground=player.Motor.Grounded;
            if (!ground && AirDashesRemaining<=0) return false;
            int direction;
            if (player.Consume(CombatCommand.DoubleTapA)) direction=-1;
            else if (player.Consume(CombatCommand.DoubleTapD)) direction=1;
            else return false;
            player.Facing=direction;
            if (!ground) AirDashesRemaining--;
            player.States.Change(ground ? (PlayerState)new GroundDashState(player,direction) : new AirDashState(player,direction));
            return true;
        }
    }
}
