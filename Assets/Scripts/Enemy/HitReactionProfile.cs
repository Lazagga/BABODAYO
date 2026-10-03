using Babodayo.Combat;
using UnityEngine;
namespace Babodayo.Enemy
{
    public enum HitReaction { DamageOnly, Hitstun, Knockback, Launch, StaggerDamage }
    [CreateAssetMenu(menuName="Babodayo/Hit Reaction Profile")]
    public sealed class HitReactionProfile : ScriptableObject
    {
        [Tooltip("Neutral reaction to light attacks.")] public HitReaction Light=HitReaction.Hitstun;
        [Tooltip("Neutral reaction to heavy attacks.")] public HitReaction Heavy=HitReaction.Knockback;
        [Tooltip("Neutral reaction to launch attacks.")] public HitReaction Launcher=HitReaction.Launch;
        [Tooltip("Neutral reaction to dive attacks.")] public HitReaction Dive=HitReaction.Knockback;
        [Tooltip("Neutral reaction to shotgun attacks.")] public HitReaction Shotgun=HitReaction.Knockback;
        [Tooltip("Recovery and Stagger permit hitstun even if neutral resists it.")] public bool Punishable=true;
        [Tooltip("Allow Launch while in Recovery or Stagger. Usually false for bosses.")] public bool LaunchWhenPunished=true;
        [Min(1), Tooltip("Accumulated stagger damage needed to stagger.")] public float StaggerThreshold=40;
        [Min(1), Tooltip("Stagger duration in frames.")] public int StaggerFrames=90;
        [Tooltip("Gravity multipliers for successive airborne hits. Last value is used after the list ends.")] public float[] AirGravity={1,1,1.1f,1.25f,1.45f,1.7f,2};
        public HitReaction Resolve(AttackKind kind,EnemyState state)
        {
            if(Punishable && (state==EnemyState.AttackRecovery || state==EnemyState.Stagger || state==EnemyState.HitStun))
                return kind==AttackKind.Launcher && LaunchWhenPunished ? HitReaction.Launch : HitReaction.Hitstun;
            switch(kind)
            {
                case AttackKind.Heavy:return Heavy;
                case AttackKind.Launcher:return Launcher;
                case AttackKind.Dive:return Dive;
                case AttackKind.Shotgun:return Shotgun;
                default:return Light;
            }
        }
        public float GravityFor(int hits) => AirGravity==null || AirGravity.Length==0 ? 1 : Mathf.Max(.1f,AirGravity[Mathf.Clamp(hits-1,0,AirGravity.Length-1)]);
    }
}
