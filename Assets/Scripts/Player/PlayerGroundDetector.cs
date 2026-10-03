using UnityEngine;
namespace Babodayo.Player
{
    public sealed class PlayerGroundDetector : MonoBehaviour
    {
        [Tooltip("Solid player body collider; prototype expects an unrotated BoxCollider2D.")] public BoxCollider2D Body;
        [Tooltip("Only physical terrain layers. Exclude player/enemy/hurtbox layers.")] public LayerMask GroundMask = 1;
        [Range(0,.15f), Tooltip("Extra width per side for forgiving edge support.")] public float LedgeForgiveness = .08f;
        [Range(.01f,.1f), Tooltip("Distance below the feet accepted as support, in units.")] public float ProbeDistance = .05f;
        public bool Grounded { get; private set; }
        public RaycastHit2D Support { get; private set; }
        public bool Sample(float verticalVelocity)
        {
            Grounded = false;
            if (Body == null || verticalVelocity > .01f) return false;
            Bounds b = Body.bounds;
            Support = Physics2D.BoxCast(new Vector2(b.center.x,b.min.y+.035f),
                new Vector2(b.size.x+LedgeForgiveness*2,.02f),0,Vector2.down,ProbeDistance+.035f,GroundMask);
            Grounded = Support.collider != null && Support.normal.y > .65f &&
                Support.point.y <= b.min.y+.04f && Support.distance > 0;
            return Grounded;
        }
        private void OnDrawGizmosSelected()
        {
            if (Body == null) return;
            Bounds b = Body.bounds;
            Gizmos.color = Grounded ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(new Vector3(b.center.x,b.min.y-ProbeDistance*.5f,b.center.z),
                new Vector3(b.size.x+2*LedgeForgiveness,ProbeDistance,.02f));
        }
    }
}
