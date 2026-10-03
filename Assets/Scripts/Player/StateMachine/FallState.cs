namespace Babodayo.Player.StateMachine
{
    public sealed class FallState : PlayerState
    {
        public FallState(PlayerController p):base(p) {}
        public override void Tick()
        {
            // Commands are checked on the landing tick, with no landing recovery state.
            if (Player.TryActions()) return;
            Motor.MoveHorizontal(Player.MoveInput); Motor.Gravity();
            if (Motor.Grounded) Player.ReturnToLocomotion();
        }
    }
}
