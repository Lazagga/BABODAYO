namespace Babodayo.Player.StateMachine
{
    public sealed class GroundMoveState : PlayerState
    {
        public GroundMoveState(PlayerController p):base(p) {}
        public override void Tick()
        {
            if (Player.TryActions()) return;
            Motor.MoveHorizontal(Player.MoveInput); Motor.Gravity();
            if (!Motor.Grounded) Player.States.Change(new FallState(Player));
            else if (Player.MoveInput==0) Player.States.Change(new GroundIdleState(Player));
        }
    }
}
