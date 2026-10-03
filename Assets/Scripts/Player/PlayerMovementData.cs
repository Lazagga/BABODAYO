using UnityEngine;
namespace Babodayo.Player
{
    [CreateAssetMenu(menuName="Babodayo/Player Movement")]
    public sealed class PlayerMovementData : ScriptableObject
    {
        [Header("Horizontal — Unit/s, Unit/s²")]
        [Min(0), Tooltip("Maximum ground speed in units per second.")] public float Speed = 7;
        [Min(0), Tooltip("Ground acceleration in units per second squared.")] public float Acceleration = 90;
        [Min(0), Tooltip("Acceleration when reversing direction.")] public float ReverseAcceleration = 150;
        [Min(0), Tooltip("Deceleration with no input.")] public float Deceleration = 110;
        [Range(0,1), Tooltip("Air acceleration relative to ground.")] public float AirControl = .8f;
        [Header("Jump — Unit/s and frames")]
        [Min(0), Tooltip("Fixed initial jump velocity. Button hold has no effect.")] public float JumpVelocity = 12;
        [Min(0), Tooltip("Fixed initial double jump velocity.")] public float DoubleJumpVelocity = 11;
        [Min(0), Tooltip("Extra jumps restored on landing.")] public int AirJumps = 1;
        [Min(0), Tooltip("Frames after leaving a ledge that allow a ground jump.")] public int CoyoteFrames = 6;
        [Min(0), Tooltip("Optional jump invulnerability; zero disables it.")] public int JumpInvulnerabilityFrames = 4;
        [Header("Gravity")]
        [Min(0), Tooltip("Downward acceleration in units/s².")] public float Gravity = 30;
        [Min(0), Tooltip("Multiplier near jump apex.")] public float ApexGravity = .7f;
        [Min(0), Tooltip("Multiplier when falling.")] public float FallGravity = 1.65f;
        [Min(0), Tooltip("Absolute Y speed below which apex gravity applies.")] public float ApexThreshold = 1;
        [Header("Dash — frames, Unit/s")]
        [Min(0), Tooltip("Ground dash anticipation frames.")] public int GroundDashStartup = 3;
        [Min(1), Tooltip("Ground dash motion frames.")] public int GroundDashDuration = 12;
        [Min(0), Tooltip("Air dash anticipation frames.")] public int AirDashStartup = 5;
        [Min(1), Tooltip("Air dash motion frames.")] public int AirDashDuration = 12;
        [Min(0), Tooltip("Horizontal dash speed.")] public float DashSpeed = 16;
        [Min(0), Tooltip("Air dashes restored on landing.")] public int AirDashes = 1;
        [Tooltip("When true, air dash locks vertical speed. Otherwise gravity continues.")] public bool AirDashLockY = true;
        [Tooltip("Locked air dash vertical speed.")] public float AirDashY = 0;
        [Header("Correction — Tile = Unit")]
        [Range(0,.2f), Tooltip("Maximum lateral correction for a head corner during ascent.")] public float CornerCorrection = .15f;
        [Range(0,.25f), Tooltip("Maximum upward correction of a small dash obstruction.")] public float DashCornerCorrection = .2f;
    }
}
