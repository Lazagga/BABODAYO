namespace Babodayo.Player.StateMachine
{
    public sealed class GroundIdleState : PlayerState
    {
        public GroundIdleState(PlayerController p):base(p) {}
        public override void Tick()
        {
            if (Player.TryActions()) return;
            Motor.MoveHorizontal(Player.MoveInput); Motor.Gravity();
            if (!Motor.Grounded) Player.States.Change(new FallState(Player));
            else if (Player.MoveInput!=0) Player.States.Change(new GroundMoveState(Player));
        }
    }
}
