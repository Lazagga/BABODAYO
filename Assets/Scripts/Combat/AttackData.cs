using System;
using System.Collections.Generic;
using UnityEngine;
namespace Babodayo.Combat
{
    public enum AttackKind { Light, Heavy, Launcher, Dive, Shotgun }
    public enum AttackPhase { Startup, Active, Recovery, Complete }
    [Serializable] public sealed class FrameData
    {
        [Min(0), Tooltip("Anticipation frames.")] public int Startup=7;
        [Min(1), Tooltip("Attack active duration in frames.")] public int Active=3;
        [Min(0), Tooltip("Recovery frames.")] public int Recovery=12;
        public int Total => Startup+Active+Recovery;
        public AttackPhase Phase(int frame) => frame<Startup ? AttackPhase.Startup :
            frame<Startup+Active ? AttackPhase.Active : frame<Total ? AttackPhase.Recovery : AttackPhase.Complete;
    }
    [Serializable] public sealed class HitData
    {
        [Min(0), Tooltip("HP damage, unaffected by style repetition.")] public float Damage=10;
        [Min(0), Tooltip("Reaction lock duration in frames.")] public int Hitstun=18;
        [Min(0), Tooltip("Combat motion freeze duration in frames.")] public int Hitstop=4;
        [Tooltip("Velocity applied to target; X is mirrored by facing.")] public Vector2 Knockback=new Vector2(3,0);
        [Tooltip("Whether this attack requests a launch reaction.")] public bool Launch;
        [Min(0), Tooltip("Target initial Y velocity for launch, not additive force.")] public float LaunchVelocity=12;
        [Min(0), Tooltip("Boss stagger meter damage.")] public float StaggerDamage=10;
    }
    [Serializable] public sealed class MovementData
    {
        [Tooltip("Total intrinsic forward travel in Tile/Unit. Independent of enemies.")] public float Distance=.12f;
        [Tooltip("Cumulative progress from 0 to 1 over the attack; endpoints are normalized.")] public AnimationCurve Curve=AnimationCurve.Linear(0,0,1,1);
        [Tooltip("Lock vertical speed while this attack is running.")] public bool LockY;
        [Tooltip("Locked Y velocity, Unit/s.")] public float YVelocity;
        [Range(0,1), Tooltip("Horizontal air control allowed during attack.")] public float AirControl;
        [Tooltip("Use a downward velocity until terrain contact, then enter landing recovery.")] public bool Dive;
        [Min(0), Tooltip("Dive speed, Unit/s.")] public float DiveSpeed=20;
        [Min(1), Tooltip("Maximum dive descent frames before safe recovery in a bottomless area.")] public int MaxDiveFrames=180;
        [Tooltip("Forward speed during descent, mirrored by facing (Unit/s).")] public float DiveHorizontalSpeed;
        [Tooltip("Set player Y velocity once on first Active frame.")] public bool SelfLaunch;
        [Tooltip("Initial self launch Y speed (Unit/s).")] public float SelfLaunchVelocity=12;
    }
    [Serializable] public sealed class CancelData
    {
        [Min(0), Tooltip("Inclusive attack/dash/shoot cancel start frame, zero-based.")] public int Start=9;
        [Min(0), Tooltip("Inclusive cancel end frame.")] public int End=18;
        [Tooltip("Allow another sword attack in the cancel window.")] public bool Attack=true;
        [Tooltip("Allow jump cancel at any attack frame; still requires an available jump.")] public bool Jump=true;
        [Tooltip("Allow dash cancel in the cancel window.")] public bool Dash=true;
        [Tooltip("Allow shotgun cancel in the cancel window.")] public bool Shoot=true;
        [Tooltip("Require a confirmed accepted hit for every cancel including jump.")] public bool OnHitOnly;
        public bool Window(int frame,bool hit) => frame>=Start && frame<=End && (!OnHitOnly || hit);
    }
    [Serializable] public sealed class HitboxData
    {
        [Min(0), Tooltip("First active attack frame, inclusive.")] public int Start=7;
        [Min(0), Tooltip("Last active attack frame, inclusive.")] public int End=9;
        [Tooltip("Local offset in Unit; X mirrored on left facing.")] public Vector2 Offset=new Vector2(.8f,0);
        [Tooltip("Box dimensions in Unit.")] public Vector2 Size=new Vector2(1.4f,1.4f);
        [Min(0), Tooltip("Zero means once per attack. Positive value permits repeat hits after this many frames.")] public int RepeatFrames;
    }
    [Serializable] public sealed class AnimationData
    {
        [Tooltip("Optional attack animation clip, played via Playables.")] public AnimationClip Clip;
    }
    [Serializable] public sealed class VFXData
    {
        [Tooltip("Optional swing effect prefab.")] public GameObject Swing;
        [Tooltip("Optional hit effect prefab.")] public GameObject Hit;
        [Tooltip("Optional trail prefab, owned for the duration of the attack.")] public GameObject Trail;
        [Tooltip("Optional camera impact tuning.")] public CameraImpulseProfile CameraImpulse;
    }
    [Serializable] public sealed class AudioData
    {
        [Tooltip("Optional swing audio.")] public AudioClip Swing;
        [Tooltip("Optional hit audio.")] public AudioClip Hit;
    }
    [Serializable] public sealed class ShotgunData
    {
        [Min(1), Tooltip("Number of evenly spread rays.")] public int Pellets=7;
        [Min(0), Tooltip("Maximum ray distance in Unit.")] public float Range=4;
        [Range(0,90), Tooltip("Total spread cone in degrees.")] public float Spread=28;
        [Tooltip("If false, each target receives one hit regardless of pellet count. If true, damage is multiplied by pellets.")] public bool DamagePerPellet;
        [Tooltip("Muzzle position relative to actor; X mirrors.")] public Vector2 Muzzle=new Vector2(.5f,.1f);
    }
    [CreateAssetMenu(menuName="Babodayo/Attack")]
    public sealed class AttackData : ScriptableObject
    {
        [Tooltip("Stable unique move ID for events, replay and style.")] public string ID="Attack";
        [Tooltip("Reaction category, not an input command.")] public AttackKind Kind;
        [Tooltip("Startup/active/recovery timing.")] public FrameData Frames=new FrameData();
        [Tooltip("Damage and reaction request.")] public HitData Hit=new HitData();
        [Tooltip("Intrinsic movement and air behavior.")] public MovementData Movement=new MovementData();
        [Tooltip("Cancel rules.")] public CancelData Cancel=new CancelData();
        [Tooltip("Per-frame boxes.")] public List<HitboxData> Hitboxes=new List<HitboxData>{new HitboxData()};
        [Tooltip("Optional animation.")] public AnimationData Animation=new AnimationData();
        [Tooltip("Optional effects.")] public VFXData VFX=new VFXData();
        [Tooltip("Optional sound.")] public AudioData Audio=new AudioData();
        [Tooltip("Hitscan data used only by Shotgun.")] public ShotgunData Shotgun=new ShotgunData();
        public IEnumerable<string> Validate()
        {
            if (string.IsNullOrWhiteSpace(ID)) yield return "Error: ID is required.";
            if (Frames.Startup<0 || Frames.Recovery<0 || Frames.Active<=0) yield return "Error: Startup/Recovery >= 0 and Active > 0 required.";
            if (Cancel.Start<0 || Cancel.Start>Cancel.End || Cancel.End>=Frames.Total) yield return "Error: Cancel window is outside the attack or reversed.";
            foreach (var box in Hitboxes)
            {
                if (box.Start<0 || box.End>=Frames.Total || box.Start>box.End) yield return "Error: Hitbox frame outside attack.";
                else if (box.Start<Frames.Startup || box.End>=Frames.Startup+Frames.Active) yield return "Warning: Hitbox extends outside Active and will be gated at runtime.";
                if (box.Size.x<=0 || box.Size.y<=0) yield return "Error: Hitbox size must be positive.";
            }
        }
    }
}
