namespace Babodayo.Player.StateMachine
{
    public abstract class PlayerState
    {
        protected readonly PlayerController Player;
        protected PlayerMotor Motor => Player.Motor;
        public virtual string Name => GetType().Name.Replace("State","");
        protected PlayerState(PlayerController player) { Player=player; }
        public virtual void Enter() {}
        public abstract void Tick();
        public virtual void Exit() {}
    }
}
