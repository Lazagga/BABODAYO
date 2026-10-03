using System;
using Babodayo.Core;
using UnityEngine;
namespace Babodayo.Rank
{
    public sealed class StyleRankSystem : MonoBehaviour
    {
        [Tooltip("Scene clock.")] public CombatClock Clock;
        [Tooltip("Style tuning asset.")] public StyleRankData Data;
        public float Score { get; private set; }
        public float LastDelta { get; private set; }
        public int ComboCount { get; private set; }
        private int lastHit=-10000,repeat;
        private string lastMove;
        private static readonly string[] names={"D","C","B","A","S","SS","SSS"};
        public string Rank
        {
            get { int index=0; if(Data!=null) for(int i=0;i<Mathf.Min(7,Data.Thresholds.Length);i++) if(Score>=Data.Thresholds[i]) index=i; return names[index]; }
        }
        public event Action<StyleEvent,float> Changed;
        private void OnEnable() { if(Clock!=null) Clock.InputTick+=Tick; }
        private void OnDisable() { if(Clock!=null) Clock.InputTick-=Tick; }
        private void Tick(int frame)
        {
            if(Data==null) return;
            LastDelta=0; Score=Mathf.Max(0,Score-Data.DecayPerSecond/60);
            if(frame-lastHit>Data.ComboTimeout) ComboCount=0;
        }
        public void BeginMove(string id)
        { if(id==lastMove) repeat++; else { repeat=0; lastMove=id; } }
        public void Process(StyleEvent value)
        {
            if(Data==null) return;
            float delta;
            switch(value.Kind)
            {
                case StyleEventKind.DamageDealt:
                    float multiplier=Data.RepeatMultipliers.Length==0 ? 1 : Data.RepeatMultipliers[Mathf.Min(repeat,Data.RepeatMultipliers.Length-1)];
                    delta=value.Value*Data.DamageScale*multiplier; ComboCount++; lastHit=value.Frame; break;
                case StyleEventKind.EnemyKill:delta=Data.KillBonus;break;
                case StyleEventKind.DifferentMove:delta=Data.VarietyBonus;break;
                case StyleEventKind.AirHit:delta=Data.AirBonus;break;
                case StyleEventKind.TookDamage:delta=-Data.HitPenalty;ComboCount=0;break;
                default:delta=Data.SkillBonus;break;
            }
            float old=Score; Score=Mathf.Max(0,Score+delta); LastDelta+=Score-old; Changed?.Invoke(value,Score-old);
        }
    }
}
