using UnityEngine;
namespace Babodayo.Combat
{
    public sealed class CombatPresentationHooks : MonoBehaviour
    {
        [Tooltip("Shared combat events.")] public CombatEvents Events;
        [Min(.1f), Tooltip("Temporary effect lifetime in seconds.")] public float Lifetime=2;
        private void OnEnable() { Events.Hit+=Hit; Events.Swing+=Swing; }
        private void OnDisable() { Events.Hit-=Hit; Events.Swing-=Swing; }
        private void Hit(HitResult result,DamageReceiver receiver)
        { Spawn(result.Attack.VFX.Hit,result.HitPosition,1); Sound(result.Attack.Audio.Hit,result.HitPosition); }
        private void Swing(AttackData attack,Transform actor,int facing)
        { Spawn(attack.VFX.Swing,actor.position,facing); Sound(attack.Audio.Swing,actor.position); }
        private void Spawn(GameObject prefab,Vector3 point,int facing)
        { if(prefab==null) return; var effect=Instantiate(prefab,point,Quaternion.identity); var scale=effect.transform.localScale; scale.x*=facing; effect.transform.localScale=scale; Destroy(effect,Lifetime); }
        private void Sound(AudioClip clip,Vector3 point) { if(clip!=null) AudioSource.PlayClipAtPoint(clip,point); }
    }
}
