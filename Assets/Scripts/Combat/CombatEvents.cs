using System;
using UnityEngine;
namespace Babodayo.Combat
{
    public sealed class CombatEvents : MonoBehaviour
    {
        public event Action<HitResult,DamageReceiver> Hit;
        public event Action<HitResult,DamageReceiver> Kill;
        public event Action<AttackData,Transform,int> Swing;
        public event Action<Vector2,AttackData> Impact;
        public event Action<int> Dodge;
        public void PublishHit(HitResult hit,DamageReceiver target) => Hit?.Invoke(hit,target);
        public void PublishKill(HitResult hit,DamageReceiver target) => Kill?.Invoke(hit,target);
        public void PublishSwing(AttackData attack,Transform actor,int facing) => Swing?.Invoke(attack,actor,facing);
        public void PublishImpact(Vector2 point,AttackData attack) => Impact?.Invoke(point,attack);
        public void PublishDodge(int frame) => Dodge?.Invoke(frame);
    }
}
