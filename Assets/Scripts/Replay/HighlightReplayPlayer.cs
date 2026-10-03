using Babodayo.Core;
using UnityEngine;
namespace Babodayo.Replay
{
    public sealed class HighlightReplayPlayer : MonoBehaviour
    {
        [Tooltip("Recorder source.")] public CombatRecorder Recorder;
        [Tooltip("Highlight selection and slow motion settings.")] public HighlightSettings Settings;
        [Tooltip("Scene clock is disabled while ghosts are played.")] public CombatClock Clock;
        [Tooltip("Actual sprites hidden during playback; order player then enemies.")] public SpriteRenderer[] Originals;
        [Tooltip("Camera follow disabled while replay camera follows the recorded player.")] public MonoBehaviour CameraFollow;
        [Tooltip("Camera used by this demo.")] public UnityEngine.Camera View;
        public bool Active { get; private set; }
        public bool Paused { get; set; }
        public string Status { get; private set; }="F2: select highlight";
        private CombatSnapshot[] frames;
        private SpriteRenderer[] ghosts;
        private HighlightSegment segment;
        private float cursor;
        private Vector3 savedCamera;
        private bool savedClock,savedFollow;
        private bool[] savedVisibility;
        public bool Begin()
        {
            if(Active) return true;
            frames=Recorder.CopyChronological(); segment=HighlightSegmentSelector.Select(frames,Settings);
            if(!segment.Valid) { Status="No qualifying highlight yet"; return false; }
            ghosts=new SpriteRenderer[Originals.Length]; savedVisibility=new bool[Originals.Length];
            for(int i=0;i<Originals.Length;i++)
            {
                var go=new GameObject("ReplayGhost_"+i);
                var sprite=go.AddComponent<SpriteRenderer>(); sprite.sprite=Originals[i].sprite;
                sprite.color=Originals[i].color; sprite.sortingOrder=Originals[i].sortingOrder;
                sprite.transform.localScale=Originals[i].transform.lossyScale;
                savedVisibility[i]=Originals[i].enabled; Originals[i].enabled=false; ghosts[i]=sprite;
            }
            savedClock=Clock.enabled; savedFollow=CameraFollow.enabled; savedCamera=View.transform.position;
            Clock.enabled=false; CameraFollow.enabled=false; Recorder.Recording=false;
            cursor=segment.Start; Active=true; Paused=false; Status="Highlight replay"; Sample(); return true;
        }
        private void Update()
        {
            if(!Active || Paused) return;
            int index=Mathf.Clamp((int)cursor,segment.Start,segment.End);
            bool slow=false;
            for(int i=index;i>=segment.Start && frames[index].Frame-frames[i].Frame<=Settings.SlowMotionFrames;i--)
                foreach(var item in frames[i].Events) if(item.Kind=="Hit") slow=true;
            cursor+=Time.unscaledDeltaTime*60*(slow ? Settings.HitSlowMotion : 1);
            Sample();
            if(cursor>=segment.End) { Paused=true; Status="Replay ended — Replay or Skip"; }
        }
        private void Sample()
        {
            int index=Mathf.Clamp((int)cursor,segment.Start,segment.End);
            var current=frames[index];
            ghosts[0].transform.position=current.Player.Position; ghosts[0].flipX=current.Player.Facing<0;
            for(int i=0;i<current.Enemies.Length && i+1<ghosts.Length;i++)
            { ghosts[i+1].transform.position=current.Enemies[i].Position; ghosts[i+1].flipX=current.Enemies[i].Facing<0; ghosts[i+1].color=current.Enemies[i].HP<=0 ? Color.gray : Originals[i+1].color; }
            View.transform.position=new Vector3(current.Player.Position.x,2.5f,savedCamera.z);
        }
        public void Restart() { if(Active) { cursor=segment.Start; Paused=false; Sample(); } }
        public void End()
        {
            if(!Active) return;
            for(int i=0;i<ghosts.Length;i++) { Destroy(ghosts[i].gameObject); Originals[i].enabled=savedVisibility[i]; }
            Clock.enabled=savedClock; CameraFollow.enabled=savedFollow; View.transform.position=savedCamera;
            Recorder.Recording=true; Active=false; Status="F2: select highlight";
            Recorder.Player.Commands.Clear();
            Recorder.Player.GetComponent<Babodayo.Input.PlayerInputReader>()?.ClearPending();
        }
        private void OnDisable() => End();
    }
}
