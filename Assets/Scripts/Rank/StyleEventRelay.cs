using Babodayo.Combat;
using Babodayo.Enemy;
using UnityEngine;
namespace Babodayo.Rank
{
    public sealed class StyleEventRelay : MonoBehaviour
    {
        [Tooltip("Scene combat publisher.")] public CombatEvents Events;
        [Tooltip("Style event consumer.")] public StyleRankSystem Rank;
        private long lastInstance=long.MinValue;
        private string lastMove;
        private void OnEnable() { Events.Hit+=OnHit; Events.Kill+=OnKill; Events.Dodge+=OnDodge; }
        private void OnDisable() { Events.Hit-=OnHit; Events.Kill-=OnKill; Events.Dodge-=OnDodge; }
        private void OnDodge(int frame) => Rank.Process(new StyleEvent(StyleEventKind.Dodge,frame));
        private void OnHit(HitResult hit,DamageReceiver target)
        {
            if(target.Team==CombatTeam.Player) { Rank.Process(new StyleEvent(StyleEventKind.TookDamage,hit.Frame)); return; }
            if(hit.Attacker==null || hit.Attacker.Team!=CombatTeam.Player) return;
            if(lastInstance!=hit.InstanceID)
            {
                Rank.BeginMove(hit.AttackID);
                if(lastMove!=null && lastMove!=hit.AttackID) Rank.Process(new StyleEvent(StyleEventKind.DifferentMove,hit.Frame,0,hit.AttackID));
                lastMove=hit.AttackID; lastInstance=hit.InstanceID;
            }
            Rank.Process(new StyleEvent(StyleEventKind.DamageDealt,hit.Frame,hit.Damage,hit.AttackID));
            var enemy=target.GetComponent<EnemyController>();
            if(enemy!=null && !enemy.Motor.Grounded) Rank.Process(new StyleEvent(StyleEventKind.AirHit,hit.Frame,0,hit.AttackID));
            if(enemy!=null && enemy.LastHitWasPunish) Rank.Process(new StyleEvent(StyleEventKind.Punish,hit.Frame,0,hit.AttackID));
        }
        private void OnKill(HitResult hit,DamageReceiver target)
        { if(target.Team==CombatTeam.Enemy && hit.Attacker!=null && hit.Attacker.Team==CombatTeam.Player) Rank.Process(new StyleEvent(StyleEventKind.EnemyKill,hit.Frame,0,hit.AttackID)); }
    }
}
