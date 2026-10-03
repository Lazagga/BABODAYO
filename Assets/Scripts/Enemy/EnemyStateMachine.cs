namespace Babodayo.Enemy
{
    public enum EnemyState { Idle, Neutral, Approach, AttackStartup, AttackActive, AttackRecovery, HitStun, Launched, AirHit, Knockdown, Stagger, Dead }
    public sealed class EnemyStateMachine
    {
        public EnemyState Current { get; private set; }
        public int Remaining { get; private set; }
        public void Change(EnemyState state,int remaining=0) { Current=state; Remaining=remaining; }
        public bool Advance() { if(Remaining>0) Remaining--; return Remaining==0; }
    }
}
