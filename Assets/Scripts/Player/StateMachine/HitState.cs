namespace Babodayo.Player.StateMachine
{
    public sealed class HitState : PlayerState
    {
        private int remaining;
        public HitState(PlayerController p,int frames):base(p) { remaining=frames; }
        public override void Tick() { Motor.Gravity(); if(--remaining<=0) Player.ReturnToLocomotion(); }
    }
}
