using UnityEngine;
namespace Babodayo.Combat
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Hurtbox : MonoBehaviour
    {
        [Tooltip("Owner receives HitResult independent of this collider.")] public DamageReceiver Receiver;
        [Tooltip("Show cyan hurtbox in Scene view.")] public bool DebugDraw=true;
        private void OnDrawGizmos()
        {
            if (!DebugDraw) return;
            var box=GetComponent<BoxCollider2D>();
            Gizmos.color=Color.cyan;
            Gizmos.DrawWireCube(transform.TransformPoint(box.offset),Vector3.Scale(box.size,transform.lossyScale));
        }
    }
}
