namespace Babodayo.Rank
{
    public enum StyleEventKind { DamageDealt, EnemyKill, DifferentMove, AirHit, TookDamage, Dodge, Counter, Punish }
    public readonly struct StyleEvent
    {
        public readonly StyleEventKind Kind;
        public readonly int Frame;
        public readonly float Value;
        public readonly string MoveID;
        public StyleEvent(StyleEventKind kind,int frame,float value=0,string moveID=null)
        { Kind=kind; Frame=frame; Value=value; MoveID=moveID; }
    }
}
