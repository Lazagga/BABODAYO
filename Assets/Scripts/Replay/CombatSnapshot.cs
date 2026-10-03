using System;
using UnityEngine;
namespace Babodayo.Replay
{
    [Serializable] public struct ActorSnapshot
    {
        public Vector2 Position,Velocity;
        public string State,Animation;
        public float AnimationTime,HP;
        public int Facing;
    }
    [Serializable] public struct RecordedEvent
    {
        public int Frame;
        public string Kind,MoveID;
        public float Value;
    }
    [Serializable] public sealed class CombatSnapshot
    {
        public int Frame;
        public ActorSnapshot Player;
        public ActorSnapshot[] Enemies;
        public RecordedEvent[] Events;
        public float RankScore,RankDelta;
        public int ComboCount;
    }
}
