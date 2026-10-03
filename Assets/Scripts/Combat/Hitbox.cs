using System.Collections.Generic;
using UnityEngine;
namespace Babodayo.Combat
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Hitbox : MonoBehaviour
    {
        [Tooltip("Target hurtbox layer, separate from solid body layers.")] public LayerMask TargetMask;
        [Tooltip("Terrain that blocks shotgun rays.")] public LayerMask TerrainMask=1;
        [Tooltip("Show active boxes and shotgun rays in Scene view.")] public bool DebugDraw=true;
        [Tooltip("Optional prototype outline rendered in Game view.")] public LineRenderer Outline;
        [Tooltip("Optional Game-view pellet tracers, one per pellet.")] public LineRenderer[] PelletLines;
        private float tracerUntil;
        private BoxCollider2D box;
        private DamageReceiver owner;
        private AttackData attack;
        private readonly Dictionary<DamageReceiver,int> hitFrames=new Dictionary<DamageReceiver,int>();
        private readonly List<Vector2> rays=new List<Vector2>();
        private Vector2 rayOrigin;
        private int rayThrough;
        private long sequence;
        private int facing;
        public bool ConfirmedHit { get; private set; }
        public void SetFacing(int direction) { facing = direction < 0 ? -1 : 1; }
        public void Begin(DamageReceiver source,AttackData data,int direction)
        {
            if(box==null) box=GetComponent<BoxCollider2D>();
            owner=source; attack=data; facing=direction; sequence++;
            hitFrames.Clear(); ConfirmedHit=false; Disable();
        }
        public void Disable() { if(box==null) box=GetComponent<BoxCollider2D>(); box.enabled=false; if(Outline!=null) Outline.enabled=false; }
        public void Evaluate(int attackFrame,int combatFrame,bool impact=false)
        {
            Disable();
            if(attack==null || attack.Kind==AttackKind.Shotgun) return;
            foreach(var shape in attack.Hitboxes)
            {
                if(!impact && (attackFrame<shape.Start || attackFrame>shape.End ||
                    attack.Frames.Phase(attackFrame)!=AttackPhase.Active)) continue;
                box.isTrigger=true; box.offset=new Vector2(shape.Offset.x*facing,shape.Offset.y); box.size=shape.Size; box.enabled=true;
                Vector2 center=transform.TransformPoint(box.offset);
                if(Outline!=null)
                {
                    Outline.enabled=true; Outline.positionCount=5;
                    Vector2 half=shape.Size*.5f;
                    Outline.SetPositions(new[]{(Vector3)(center+new Vector2(-half.x,-half.y)),(Vector3)(center+new Vector2(half.x,-half.y)),
                        (Vector3)(center+half),(Vector3)(center+new Vector2(-half.x,half.y)),(Vector3)(center-half)});
                }
                foreach(var target in Physics2D.OverlapBoxAll(center,shape.Size,0,TargetMask))
                {
                    var hurt=target.GetComponent<Hurtbox>();
                    if(hurt==null || hurt.Receiver==null) continue;
                    var receiver=hurt.Receiver;
                    if(hitFrames.TryGetValue(receiver,out int previous) && (shape.RepeatFrames<=0 || combatFrame-previous<shape.RepeatFrames)) continue;
                    if(Deliver(receiver,combatFrame,target.ClosestPoint(center))) hitFrames[receiver]=combatFrame;
                }
            }
        }
        private bool Deliver(DamageReceiver receiver,int frame,Vector2 position,float multiplier=1)
        {
            var result=new HitResult(owner,attack,((long)GetInstanceID()<<32) ^ sequence,frame,facing,position,multiplier);
            if(!receiver.Receive(result)) return false;
            ConfirmedHit=true; return true;
        }
        public void Shoot(int combatFrame)
        {
            var data=attack.Shotgun;
            rayOrigin=(Vector2)transform.position+new Vector2(data.Muzzle.x*facing,data.Muzzle.y);
            rays.Clear(); rayThrough=combatFrame+6;
            var counts=new Dictionary<DamageReceiver,int>();
            for(int i=0;i<Mathf.Max(1,data.Pellets);i++)
            {
                float angle=(data.Pellets<=1 ? 0 : (i/(float)(data.Pellets-1)-.5f)*data.Spread)*Mathf.Deg2Rad;
                Vector2 direction=new Vector2(Mathf.Cos(angle)*facing,Mathf.Sin(angle));
                Vector2 end=rayOrigin+direction*data.Range;
                foreach(var hit in Physics2D.RaycastAll(rayOrigin,direction,data.Range,TargetMask|TerrainMask))
                {
                    if(((1<<hit.collider.gameObject.layer)&TerrainMask)!=0) { end=hit.point; break; }
                    var hurt=hit.collider.GetComponent<Hurtbox>();
                    if(hurt==null || hurt.Receiver==null || hurt.Receiver==owner || hurt.Receiver.Team==owner.Team) continue;
                    counts.TryGetValue(hurt.Receiver,out int n); counts[hurt.Receiver]=n+1; end=hit.point; break;
                }
                rays.Add(end); Debug.DrawLine(rayOrigin,end,Color.yellow,.12f);
                if(PelletLines!=null && i<PelletLines.Length && PelletLines[i]!=null)
                { PelletLines[i].enabled=true; PelletLines[i].positionCount=2; PelletLines[i].SetPosition(0,rayOrigin); PelletLines[i].SetPosition(1,end); }
            }
            tracerUntil=Time.unscaledTime+.12f;
            foreach(var pair in counts) Deliver(pair.Key,combatFrame,pair.Key.transform.position,data.DamagePerPellet ? pair.Value : 1);
        }
        private void Update()
        { if(Time.unscaledTime>tracerUntil && PelletLines!=null) foreach(var line in PelletLines) if(line!=null) line.enabled=false; }
        private void OnDrawGizmos()
        {
            if(!DebugDraw) return;
            if(box!=null && box.enabled) { Gizmos.color=Color.red; Gizmos.DrawWireCube(transform.TransformPoint(box.offset),box.size); }
            if(owner!=null && owner.Hitstop!=null && owner.Hitstop.Clock.CurrentFrame>rayThrough) return;
            Gizmos.color=Color.yellow; foreach(var end in rays) Gizmos.DrawLine(rayOrigin,end);
        }
    }
}
