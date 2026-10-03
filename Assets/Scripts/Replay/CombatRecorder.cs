using System.Collections.Generic;
using Babodayo.Combat;
using Babodayo.Core;
using Babodayo.Enemy;
using Babodayo.Input;
using Babodayo.Player;
using Babodayo.Rank;
using UnityEngine;
namespace Babodayo.Replay
{
    public sealed class CombatRecorder : MonoBehaviour
    {
        [Tooltip("Shared clock; completed tick records post-physics.")] public CombatClock Clock;
        [Tooltip("Player state source.")] public PlayerController Player;
        [Tooltip("Player attack/health source.")] public PlayerCombat Combat;
        [Tooltip("Stable enemy order used also by replay ghosts.")] public EnemyController[] Enemies;
        [Tooltip("Rank state source.")] public StyleRankSystem Rank;
        [Tooltip("Combat event publisher.")] public CombatEvents Events;
        [Min(60), Tooltip("Maximum retained snapshots, at 60 frames/s.")] public int Capacity=3600;
        public bool Recording { get; set; }=true;
        private CombatSnapshot[] frames;
        private int next,count;
        private float lastScore;
        private readonly List<RecordedEvent> pending=new List<RecordedEvent>();
        private void Start()
        {
            frames=new CombatSnapshot[Mathf.Max(60,Capacity)];
            Clock.CompletedTick+=Capture; Player.CommandExecuted+=Command;
            Events.Hit+=Hit; Events.Kill+=Kill; Rank.Changed+=Style;
        }
        private void OnDestroy()
        {
            if(Clock!=null) Clock.CompletedTick-=Capture;
            if(Player!=null) Player.CommandExecuted-=Command;
            if(Events!=null) { Events.Hit-=Hit; Events.Kill-=Kill; }
            if(Rank!=null) Rank.Changed-=Style;
        }
        private void Command(CombatCommand command,int frame) { if(Recording) pending.Add(new RecordedEvent{Frame=frame,Kind="Command",MoveID=command.ToString()}); }
        private void Hit(HitResult hit,DamageReceiver target) { if(Recording) pending.Add(new RecordedEvent{Frame=hit.Frame,Kind="Hit",MoveID=hit.AttackID,Value=hit.Damage}); }
        private void Kill(HitResult hit,DamageReceiver target) { if(Recording) pending.Add(new RecordedEvent{Frame=hit.Frame,Kind="Kill",MoveID=hit.AttackID}); }
        private void Style(StyleEvent value,float delta) { if(Recording) pending.Add(new RecordedEvent{Frame=value.Frame,Kind="RankDelta",MoveID=value.MoveID,Value=delta}); }
        private void Capture(int frame)
        {
            if(!Recording) return;
            var snapshot=new CombatSnapshot { Frame=frame, RankScore=Rank.Score,RankDelta=Rank.Score-lastScore,ComboCount=Rank.ComboCount,
                Player=new ActorSnapshot {Position=Player.transform.position,Velocity=Player.Motor.Velocity,State=Player.States.Name,
                    Facing=Player.Facing,HP=Combat.Receiver.HP,Animation=Combat.Current?.Data.ID,
                    AnimationTime=(Combat.Current?.AttackFrame ?? 0)/60f },
                Enemies=new ActorSnapshot[Enemies.Length],Events=pending.ToArray() };
            for(int i=0;i<Enemies.Length;i++)
            {
                var enemy=Enemies[i]; if(enemy==null) continue;
                snapshot.Enemies[i]=new ActorSnapshot{Position=enemy.transform.position,Velocity=enemy.Motor.Velocity,
                    State=enemy.States.Current.ToString(),HP=enemy.Receiver.HP,Facing=enemy.Facing,
                    Animation=enemy.CurrentAttack?.ID,AnimationTime=enemy.AttackFrame/60f};
            }
            lastScore=Rank.Score; pending.Clear();
            frames[next]=snapshot; next=(next+1)%frames.Length; count=Mathf.Min(count+1,frames.Length);
        }
        public CombatSnapshot[] CopyChronological()
        {
            var result=new CombatSnapshot[count];
            for(int i=0;i<count;i++) result[i]=frames[(next-count+i+frames.Length)%frames.Length];
            return result;
        }
    }
}
