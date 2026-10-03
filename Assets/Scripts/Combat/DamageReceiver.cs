using System;
using UnityEngine;
namespace Babodayo.Combat
{
    public enum CombatTeam { Player, Enemy }
    public sealed class DamageReceiver : MonoBehaviour
    {
        [Tooltip("Friendly hits are ignored.")] public CombatTeam Team;
        [Min(1), Tooltip("Starting health; can be set by EnemyData.")] public float MaxHP=100;
        [Min(0), Tooltip("Frames of invulnerability after accepted damage.")] public int AfterHitInvulnerability;
        [Tooltip("Shared scene event publisher.")] public CombatEvents Events;
        [Tooltip("Optional source-aware invulnerability.")] public Invulnerability Invulnerability;
        [Tooltip("Optional actor hitstop controller.")] public HitstopController Hitstop;
        public float HP { get; private set; }
        public bool Dead => HP<=0;
        public event Action<HitResult> Received;
        public event Action Died;
        private void Awake() { HP=MaxHP; }
        public void ResetHealth() { HP=MaxHP; Invulnerability?.Clear(); }
        public bool Receive(HitResult result)
        {
            if (Dead || result.Attacker==this || (result.Attacker!=null && result.Attacker.Team==Team) ||
                (Invulnerability!=null && Invulnerability.IsActive(result.Frame))) return false;
            HP=Mathf.Max(0,HP-result.Damage);
            Invulnerability?.Grant(InvulnerabilityCause.AfterHit,result.Frame,AfterHitInvulnerability);
            Received?.Invoke(result);
            Hitstop?.Request(result.HitstopFrames);
            result.Attacker?.Hitstop?.Request(result.HitstopFrames);
            Events?.PublishHit(result,this);
            if (Dead) { Died?.Invoke(); Events?.PublishKill(result,this); }
            return true;
        }
    }
}
