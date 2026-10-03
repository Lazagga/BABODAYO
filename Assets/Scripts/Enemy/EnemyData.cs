using Babodayo.Combat;
using UnityEngine;
namespace Babodayo.Enemy
{
    [CreateAssetMenu(menuName="Babodayo/Enemy")]
        public sealed class EnemyData : ScriptableObject
    {
        [Min(1), Tooltip("Maximum HP.")] public float HP=60;
        [Tooltip("Reaction policy; player attacks never change for this enemy.")] public HitReactionProfile Reactions;
        [Tooltip("Enable approach and telegraphed attacks. Disable for dummy.")] public bool AI;
        [Tooltip("Boss uses Neutral/Startup/Active/Recovery and the pattern list.")] public bool Boss;
        [Min(0), Tooltip("AI remains idle beyond this distance from player.")] public float AggroRange=8;
        [Min(0), Tooltip("Approach velocity, Unit/s.")] public float MoveSpeed=2;
        [Min(0), Tooltip("Distance to begin preparing an attack.")] public float AttackRange=2.5f;
        [Min(0), Tooltip("Frames between attacks after recovery.")] public int IdleFrames=45;
        [Tooltip("Ordered attack patterns; all values live in AttackData assets.")] public AttackData[] Attacks;
        [Min(0), Tooltip("Downward acceleration, Unit/s².")] public float Gravity=30;
        [Tooltip("Dummy restores HP (including after death) after this many quiet frames. Zero disables.")] public int AutoHealFrames;
        [Min(1), Tooltip("Grounded knockdown frames after launch.")] public int KnockdownFrames=20;
    }
}
