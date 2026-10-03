using System;
using Babodayo.Core;
using UnityEngine;
namespace Babodayo.Combat
{
    public sealed class HitstopController : MonoBehaviour
    {
        [Tooltip("Shared combat clock; input keeps advancing during local hitstop.")] public CombatClock Clock;
        private int through=-1;
        private bool frozen;
        private Action<bool> motion;
        public bool IsStopped => Clock!=null && Clock.CurrentFrame<=through;
        public void Bind(Action<bool> value) { motion=value; }
        private void OnEnable() { if(Clock!=null) Clock.InputTick+=Tick; }
        private void OnDisable() { if(Clock!=null) Clock.InputTick-=Tick; if(frozen) motion?.Invoke(false); frozen=false; through=-1; }
        public void Request(int frames)
        {
            if(frames<=0 || Clock==null) return;
            through=Mathf.Max(through,Clock.CurrentFrame+frames);
            if(!frozen) { frozen=true; motion?.Invoke(true); }
        }
        private void Tick(int frame)
        { if(frozen && !IsStopped) { frozen=false; motion?.Invoke(false); } }
    }
}
