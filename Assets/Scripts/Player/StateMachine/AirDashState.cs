namespace Babodayo.Player.StateMachine
{
    public sealed class AirDashState : GroundDashState
    {
        protected override bool Air => true;
        public AirDashState(PlayerController p,int direction):base(p,direction) {}
    }
}
