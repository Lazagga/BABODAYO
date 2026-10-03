using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace Babodayo.Combat
{
    public sealed class AttackPresentation : MonoBehaviour
    {
        [Tooltip("Optional Animator; clip timing is driven by combat frames.")] public Animator Animator;
        private PlayableGraph graph;
        private AnimationClipPlayable playable;
        private GameObject trail;
        public void Begin(AttackData attack)
        {
            End();
            if(attack.VFX.Trail!=null) trail=Instantiate(attack.VFX.Trail,transform);
            if(Animator==null || attack.Animation.Clip==null) return;
            graph=PlayableGraph.Create("Attack preview runtime");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            playable=AnimationClipPlayable.Create(graph,attack.Animation.Clip);
            AnimationPlayableOutput.Create(graph,"Attack",Animator).SetSourcePlayable(playable);
            graph.Play();
        }
        public void Sample(int frame)
        { if(graph.IsValid()) { playable.SetTime(frame/60.0); graph.Evaluate(0); } }
        public void End() { if(graph.IsValid()) graph.Destroy(); if(trail!=null) Destroy(trail); }
        private void OnDisable() => End();
    }
}
