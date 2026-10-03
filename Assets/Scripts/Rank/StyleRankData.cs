using UnityEngine;
namespace Babodayo.Rank
{
    [CreateAssetMenu(menuName="Babodayo/Style Rank")]
    public sealed class StyleRankData : ScriptableObject
    {
        [Tooltip("D/C/B/A/S/SS/SSS minimum score thresholds, ascending.")] public float[] Thresholds={0,100,250,450,700,1000,1400};
        [Tooltip("Damage-to-style multiplier.")] public float DamageScale=2;
        [Tooltip("Style points per kill.")] public float KillBonus=40;
        [Tooltip("Style points for a different move.")] public float VarietyBonus=15;
        [Tooltip("Style points per airborne hit.")] public float AirBonus=10;
        [Tooltip("Style points for dodge/counter/punish.")] public float SkillBonus=20;
        [Tooltip("Score lost when player is hit.")] public float HitPenalty=80;
        [Tooltip("Score lost per second.")] public float DecayPerSecond=5;
        [Tooltip("Style multiplier for consecutive repeated attacks. Damage is unchanged.")] public float[] RepeatMultipliers={1,.7f,.4f,.2f,.1f};
        [Min(1), Tooltip("Frames without a hit before combo count resets.")] public int ComboTimeout=120;
    }
}
