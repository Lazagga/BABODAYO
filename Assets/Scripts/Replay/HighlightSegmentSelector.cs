using System.Collections.Generic;
using UnityEngine;
namespace Babodayo.Replay
{
    public readonly struct HighlightSegment
    {
        public readonly int Start,End;
        public readonly float Gain;
        public bool Valid => Start>=0 && End>=Start;
        public HighlightSegment(int start,int end,float gain) { Start=start; End=end; Gain=gain; }
    }
    public static class HighlightSegmentSelector
    {
        public static HighlightSegment Select(CombatSnapshot[] frames,HighlightSettings settings)
        {
            var best=new HighlightSegment(-1,-1,0); float bestTie=float.MinValue;
            if(frames==null || settings==null) return best;
            int start=0;
            for(int end=0;end<frames.Length;end++)
            {
                while(start<end && frames[end].Frame-frames[start].Frame>=settings.WindowFrames) start++;
                if(frames[end].Frame-frames[start].Frame+1<settings.WindowFrames) continue;
                float gain=0; int kills=0,combo=0; var moves=new HashSet<string>();
                for(int i=start;i<=end;i++)
                {
                    gain+=frames[i].RankDelta; combo=Mathf.Max(combo,frames[i].ComboCount);
                    foreach(var item in frames[i].Events)
                    { if(item.Kind=="Hit" && item.MoveID!=null) moves.Add(item.MoveID); if(item.Kind=="Kill") kills++; }
                }
                if(gain<settings.MinimumRankGain) continue;
                float tie=moves.Count*settings.VarietyWeight+kills*settings.KillWeight+combo*settings.ComboWeight;
                if(!best.Valid || gain>best.Gain+settings.TieThreshold || (Mathf.Abs(gain-best.Gain)<=settings.TieThreshold && tie>bestTie))
                { best=new HighlightSegment(start,end,gain); bestTie=tie; }
            }
            return best;
        }
    }
}
