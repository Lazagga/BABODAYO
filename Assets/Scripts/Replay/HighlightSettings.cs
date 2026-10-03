using UnityEngine;
namespace Babodayo.Replay
{
    [CreateAssetMenu(menuName="Babodayo/Highlight Settings")]
    public sealed class HighlightSettings : ScriptableObject
    {
        [Min(1), Tooltip("Window duration in combat frames.")] public int WindowFrames=240;
        [Min(0), Tooltip("Minimum net rank gain.")] public float MinimumRankGain=20;
        [Min(0), Tooltip("Gains within this range use secondary criteria.")] public float TieThreshold=5;
        [Tooltip("Weight of unique successful moves in tied windows.")] public float VarietyWeight=3;
        [Tooltip("Weight of kills in tied windows.")] public float KillWeight=2;
        [Tooltip("Weight of maximum combo in tied windows.")] public float ComboWeight=1;
        [Range(.05f,1), Tooltip("Playback speed near recorded hits.")] public float HitSlowMotion=.4f;
        [Min(0), Tooltip("Frames after a hit played slowly.")] public int SlowMotionFrames=8;
    }
}
