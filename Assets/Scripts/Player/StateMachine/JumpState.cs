namespace Babodayo.Player.StateMachine
{
    public sealed class JumpState : PlayerState
    {
        public JumpState(PlayerController p):base(p) {}
        public override void Tick()
        {
            if (Player.TryActions()) return;
            Motor.MoveHorizontal(Player.MoveInput); Motor.Gravity(); Motor.CorrectCorners(false,Player.Facing);
            if (Motor.Velocity.y<=0) Player.States.Change(new FallState(Player));
        }
    }
}
