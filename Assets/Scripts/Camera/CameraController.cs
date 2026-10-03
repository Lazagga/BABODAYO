using Babodayo.Combat;
using Babodayo.Player;
using UnityEngine;
namespace Babodayo.CameraSystem
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        [Tooltip("Explicit player follow target.")] public PlayerController Player;
        [Tooltip("Optional combat state used to reduce vertical follow.")] public PlayerCombat Combat;
        [Tooltip("Ordered rooms; first containing room wins.")] public CameraRoom[] Rooms;
        [Tooltip("Shared impact events.")] public CombatEvents Events;
        [Tooltip("Dead zone half-width/half-height, units.")] public Vector2 DeadZone=new Vector2(1,2);
        [Min(0), Tooltip("Facing look-ahead in units.")] public float LookAhead=1.4f;
        [Min(.01f), Tooltip("Smooth follow time, seconds.")] public float SmoothTime=.2f;
        [Range(0,1), Tooltip("Vertical tracking strength during airborne attacks.")] public float AirVerticalStrength=.2f;
        private Vector3 smoothVelocity,basePosition;
        private float impactRemaining,impactDuration,amplitude;
        private UnityEngine.Camera view;
        private void Start() { view=GetComponent<UnityEngine.Camera>(); basePosition=transform.position; if(Events!=null) { Events.Hit+=OnHit; Events.Impact+=OnImpact; } }
        private void OnDestroy() { if(Events!=null) { Events.Hit-=OnHit; Events.Impact-=OnImpact; } }
        private void OnHit(HitResult hit,DamageReceiver target) => OnImpact(hit.HitPosition,hit.Attack);
        private void OnImpact(Vector2 point,AttackData attack)
        {
            var profile=attack.VFX.CameraImpulse;
            if(profile==null) return;
            amplitude=Mathf.Max(amplitude,profile.Amplitude);
            impactRemaining=impactDuration=profile.Duration;
        }
        private void LateUpdate()
        {
            if(Player==null) return;
            Vector3 target=Player.transform.position+Vector3.right*(Player.Facing*LookAhead);
            CameraRoom room=null;
            if(Rooms!=null) foreach(var candidate in Rooms) if(candidate!=null && candidate.Contains(Player.transform.position)) { room=candidate; break; }
            if(room!=null) target=Vector3.Lerp(target,room.transform.position,room.CenterWeight);
            Vector3 desired=basePosition;
            float dx=target.x-basePosition.x,dy=target.y-basePosition.y;
            desired.x+=Mathf.Sign(dx)*Mathf.Max(0,Mathf.Abs(dx)-DeadZone.x);
            desired.y+=Mathf.Sign(dy)*Mathf.Max(0,Mathf.Abs(dy)-DeadZone.y)*(Combat!=null && Combat.Current!=null && Combat.Current.Air ? AirVerticalStrength : 1);
            if(room!=null && room.Constrain)
            {
                var b=room.Bounds; float h=view.orthographicSize,w=h*view.aspect;
                desired.x=b.size.x<=w*2 ? b.center.x : Mathf.Clamp(desired.x,b.min.x+w,b.max.x-w);
                desired.y=b.size.y<=h*2 ? b.center.y : Mathf.Clamp(desired.y,b.min.y+h,b.max.y-h);
            }
            basePosition=Vector3.SmoothDamp(basePosition,desired,ref smoothVelocity,SmoothTime,Mathf.Infinity,Time.unscaledDeltaTime);
            Vector3 shake=Vector3.zero;
            if(impactRemaining>0)
            {
                impactRemaining-=Time.unscaledDeltaTime;
                float scale=Mathf.Clamp01(impactRemaining/Mathf.Max(.001f,impactDuration))*amplitude;
                shake=new Vector3(Mathf.Sin(Time.unscaledTime*83),Mathf.Cos(Time.unscaledTime*97),0)*scale;
            }
            else amplitude=0;
            transform.position=basePosition+shake;
        }
    }
}
