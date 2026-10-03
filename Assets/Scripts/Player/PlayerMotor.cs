using Babodayo.Core;
using UnityEngine;
namespace Babodayo.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Tooltip("Explicit body reference. All player Rigidbody writes belong here.")] public Rigidbody2D Body;
        [Tooltip("Solid collider, used for safe correction sweeps.")] public BoxCollider2D Shape;
        [Tooltip("Shared designer movement tuning.")] public PlayerMovementData Data;
        [Tooltip("Ground detector for this player.")] public PlayerGroundDetector Ground;
        public Vector2 Velocity => Body != null ? Body.linearVelocity : Vector2.zero;
        public Vector2 Position => Body != null ? Body.position : (Vector2)transform.position;
        public bool Grounded => Ground != null && Ground.Grounded;
        public bool Frozen { get; private set; }
        private Vector2 frozenVelocity;
        [Tooltip("Actor body layers excluded from solid collision; terrain still collides.")] public LayerMask ActorBodyLayers = 1 << 8;
        private void Awake()
        {
            if (Body == null) Body = GetComponent<Rigidbody2D>();
            if (Shape == null) Shape = GetComponent<BoxCollider2D>();
            if (Shape != null) Shape.excludeLayers |= ActorBodyLayers;
            Body.gravityScale = 0;
            Body.freezeRotation = true;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        public void MoveHorizontal(float input) => MoveHorizontal(input,1);
        public void MoveHorizontal(float input,float control)
        {
            float target = Mathf.Clamp(input,-1,1)*Data.Speed;
            float acceleration = Mathf.Abs(input)<.01f ? Data.Deceleration :
                Velocity.x*input<0 ? Data.ReverseAcceleration : Data.Acceleration;
            acceleration *= (Grounded ? 1 : Data.AirControl)*control;
            SetHorizontalVelocity(Mathf.MoveTowards(Velocity.x,target,acceleration*CombatClock.FrameDuration));
        }
        public void SetHorizontalVelocity(float value) => SetVelocity(new Vector2(value,Velocity.y));
        public void SetVerticalVelocity(float value) => SetVelocity(new Vector2(Velocity.x,value));
        public void SetVelocity(Vector2 value) { if (!Frozen) Body.linearVelocity = value; }
        public void AddVelocity(Vector2 value) => SetVelocity(Velocity+value);
        public void StopHorizontal() => SetHorizontalVelocity(0);
        public void Gravity(bool lockY=false,float value=0)
        {
            if (lockY) { SetVerticalVelocity(value); return; }
            if (Grounded && Velocity.y<=0) { SetVerticalVelocity(0); return; }
            float multiplier = Mathf.Abs(Velocity.y)<=Data.ApexThreshold ? Data.ApexGravity :
                Velocity.y<0 ? Data.FallGravity : 1;
            AddVelocity(Vector2.down*(Data.Gravity*multiplier*CombatClock.FrameDuration));
        }
        public void Freeze(bool value)
        {
            if (Frozen == value) return;
            if (value) { frozenVelocity=Velocity; Body.linearVelocity=Vector2.zero; Body.simulated=false; }
            else { Body.simulated=true; Body.linearVelocity=frozenVelocity; }
            Frozen=value;
        }
        public void Teleport(Vector2 position) { Body.position=position; }
        public void CorrectCorners(bool dash,int facing)
        {
            if (Frozen || Shape == null || Ground == null) return;
            Bounds b=Shape.bounds;
            Vector2 size=(Vector2)b.size-Vector2.one*.015f;
            Vector2 direction=dash ? Vector2.right*facing : Vector2.up;
            float travel=(dash ? Mathf.Abs(Velocity.x) : Velocity.y)*CombatClock.FrameDuration+.02f;
            if (travel<=.02f) return;
            var hit=Physics2D.BoxCast(b.center,size,0,direction,travel,Ground.GroundMask);
            if (!hit.collider || hit.distance<=0) return;
            float max=dash ? Data.DashCornerCorrection : Data.CornerCorrection;
            if (dash && hit.point.y>b.min.y+max) return;
            for (float distance=.025f; distance<=max+.001f; distance+=.025f)
            {
                for (int sign=1; sign>=-1; sign-=2)
                {
                    if (dash && sign<0) continue;
                    Vector2 offset=dash ? Vector2.up*distance : Vector2.right*(distance*sign);
                    // Sweep both the correction path and the following motion; never tunnel through a wall.
                    if (Physics2D.BoxCast(b.center,size,0,offset.normalized,offset.magnitude,Ground.GroundMask).collider) continue;
                    if (Physics2D.BoxCast((Vector2)b.center+offset,size,0,direction,travel,Ground.GroundMask).collider) continue;
                    Body.position+=offset; return;
                }
            }
        }
        private void OnDrawGizmosSelected()
        {
            if (Shape==null || Data==null) return;
            Gizmos.color=Color.cyan;
            Bounds b=Shape.bounds;
            Gizmos.DrawWireCube(b.center,b.size+new Vector3(Data.CornerCorrection*2,0,0));
            Gizmos.color=new Color(1,.4f,0);
            Gizmos.DrawWireCube(b.center+Vector3.up*Data.DashCornerCorrection*.5f,
                b.size+Vector3.up*Data.DashCornerCorrection);
        }
    }
}
