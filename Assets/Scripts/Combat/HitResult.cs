using UnityEngine;
namespace Babodayo.Combat
{
    public readonly struct HitResult
    {
        public readonly DamageReceiver Attacker;
        public readonly AttackData Attack;
        public readonly long InstanceID;
        public readonly float Damage;
        public readonly int Frame,HitstunFrames,HitstopFrames;
        public readonly Vector2 Knockback,HitPosition;
        public readonly bool Launch;
        public readonly float LaunchVelocity;
        public string AttackID => Attack.ID;
        public HitResult(DamageReceiver attacker,AttackData attack,long instance,int frame,int facing,Vector2 point,float multiplier=1)
        {
            Attacker=attacker; Attack=attack; InstanceID=instance; Frame=frame;
            Damage=attack.Hit.Damage*multiplier; HitstunFrames=attack.Hit.Hitstun; HitstopFrames=attack.Hit.Hitstop;
            Knockback=new Vector2(attack.Hit.Knockback.x*facing,attack.Hit.Knockback.y);
            HitPosition=point; Launch=attack.Hit.Launch; LaunchVelocity=attack.Hit.LaunchVelocity;
        }
    }
}
