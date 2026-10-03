using UnityEngine;
namespace Babodayo.Combat
{
    public sealed class AttackPreviewObject : MonoBehaviour
    {
        [Tooltip("Attack asset being edited; changes persist to this asset.")] public AttackData Attack;
        [Min(0), Tooltip("Zero-based attack frame to display.")] public int Frame;
        [Tooltip("Mirror preview without changing stored right-facing offsets.")] public bool FaceLeft;
        [Tooltip("Optional child animator for Editor animation preview.")] public Animator Animator;
    }
}
