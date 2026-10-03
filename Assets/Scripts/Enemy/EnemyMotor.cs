using Babodayo.Core;
using UnityEngine;
namespace Babodayo.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyMotor : MonoBehaviour
    {
        [Tooltip("All enemy body writes are restricted to this motor.")] public Rigidbody2D Body;
        [Tooltip("Solid body bounds for terrain queries.")] public BoxCollider2D Shape;
        [Tooltip("Physical terrain layers.")] public LayerMask GroundMask=1;
        public Vector2 Velocity => Body.linearVelocity;
        public bool Grounded { get; private set; }
        private bool frozen;
        private Vector2 cached;
        [Tooltip("Actor body layers excluded from solid collision; combat queries are separate.")] public LayerMask ActorBodyLayers = 1 << 8;
        private void Awake()
        {
            if(Body==null) Body=GetComponent<Rigidbody2D>();
            if(Shape==null) Shape=GetComponent<BoxCollider2D>();
            if(Shape!=null) Shape.excludeLayers |= ActorBodyLayers;
            Body.gravityScale=0; Body.freezeRotation=true; Body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
        }
        public void Sample()
        {
            var b=Shape.bounds;
            var hit=Physics2D.BoxCast(new Vector2(b.center.x,b.min.y+.035f),new Vector2(b.size.x-.02f,.02f),0,Vector2.down,.075f,GroundMask);
            Grounded=Velocity.y<=.01f && hit.collider!=null && hit.normal.y>.65f && hit.distance>0;
        }
        public void SetVelocity(Vector2 value) { if(!frozen) Body.linearVelocity=value; }
        public void SetHorizontal(float value) => SetVelocity(new Vector2(value,Velocity.y));
        public void Gravity(float acceleration)
        { if(!frozen) SetVelocity(new Vector2(Velocity.x,Grounded && Velocity.y<=0 ? 0 : Velocity.y-acceleration*CombatClock.FrameDuration)); }
        public void Freeze(bool value)
        {
            if(frozen==value) return;
            if(value) { cached=Velocity; Body.linearVelocity=Vector2.zero; Body.simulated=false; }
            else { Body.simulated=true; Body.linearVelocity=cached; }
            frozen=value;
        }
        public void Teleport(Vector2 position) { Body.position=position; Body.linearVelocity=Vector2.zero; }
    }
}
