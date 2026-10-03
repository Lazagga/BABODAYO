namespace Babodayo.Player.StateMachine
{
    public sealed class DeadState : PlayerState
    {
        public DeadState(PlayerController p):base(p) {}
        public override void Enter() { Motor.StopHorizontal(); Player.Commands.Clear(); }
        public override void Tick() { Motor.StopHorizontal(); Motor.Gravity(); }
    }
}
