using Babodayo.Combat;
using UnityEditor;
using UnityEngine;
namespace Babodayo.Editor
{
    public sealed class AttackEditorWindow : EditorWindow
    {
        private AttackData selected;
        private UnityEditor.Editor inspector;
        private Vector2 listScroll,detailScroll;
        private int frame;
        private bool playing,mirror;
        private double lastTime;
        private GameObject previewObject;
        private AttackPreviewObject preview;
        private string[] assets;
        [MenuItem("Window/Combat/Attack Editor")]
        public static void ShowWindow() => GetWindow<AttackEditorWindow>("Attack Editor");
        public static void Open(AttackData attack) { var window=GetWindow<AttackEditorWindow>("Attack Editor"); window.Select(attack); }
        private void OnEnable() { RefreshList(); EditorApplication.update+=Advance; lastTime=EditorApplication.timeSinceStartup; }
        private void OnDisable()
        {
            EditorApplication.update-=Advance;
            if(inspector!=null) DestroyImmediate(inspector);
            StopPreview();
        }
        private void RefreshList() => assets=AssetDatabase.FindAssets("t:AttackData");
        private void Select(AttackData attack)
        {
            selected=attack; frame=0;
            if(inspector!=null) DestroyImmediate(inspector);
            inspector=UnityEditor.Editor.CreateEditor(selected);
            if(preview!=null) preview.Attack=attack;
        }
        private void Advance()
        {
            double now=EditorApplication.timeSinceStartup;
            if(playing && selected!=null && now-lastTime>=1.0/60)
            { frame=(frame+1)%Mathf.Max(1,selected.Frames.Total); lastTime=now; Sample(); Repaint(); }
        }
        private void OnGUI()
        {
            using(new EditorGUILayout.HorizontalScope())
            {
                using(new EditorGUILayout.VerticalScope(GUILayout.Width(180)))
                {
                    if(GUILayout.Button("Refresh attacks")) RefreshList();
                    listScroll=EditorGUILayout.BeginScrollView(listScroll);
                    if(assets!=null) foreach(var guid in assets)
                    {
                        var asset=AssetDatabase.LoadAssetAtPath<AttackData>(AssetDatabase.GUIDToAssetPath(guid));
                        if(GUILayout.Button(asset.name)) Select(asset);
                    }
                    EditorGUILayout.EndScrollView();
                }
                using(new EditorGUILayout.VerticalScope())
                {
                    if(selected==null) { EditorGUILayout.HelpBox("Select an AttackData asset.",MessageType.Info); return; }
                    using(new EditorGUILayout.HorizontalScope())
                    {
                        if(GUILayout.Button(playing ? "Pause" : "Play")) { playing=!playing; lastTime=EditorApplication.timeSinceStartup; }
                        if(GUILayout.Button("<")) { frame=Mathf.Max(0,frame-1); Sample(); }
                        if(GUILayout.Button(">")) { frame=Mathf.Min(selected.Frames.Total-1,frame+1); Sample(); }
                        mirror=GUILayout.Toggle(mirror,"Facing Left");
                    }
                    int next=EditorGUILayout.IntSlider("Frame",frame,0,Mathf.Max(0,selected.Frames.Total-1));
                    if(next!=frame) { frame=next; Sample(); }
                    DrawTimeline();
                    if(GUILayout.Button("Create / select Scene preview")) CreatePreview();
                    if(GUILayout.Button("Remove Scene preview")) StopPreview();
                    if(preview!=null) { preview.Frame=frame; preview.FaceLeft=mirror; SceneView.RepaintAll(); }
                    detailScroll=EditorGUILayout.BeginScrollView(detailScroll);
                    inspector.OnInspectorGUI();
                    EditorGUILayout.EndScrollView();
                }
            }
        }
        private void DrawTimeline()
        {
            Rect rect=GUILayoutUtility.GetRect(100,42);
            float unit=rect.width/Mathf.Max(1,selected.Frames.Total);
            EditorGUI.DrawRect(new Rect(rect.x,rect.y,unit*selected.Frames.Startup,25),new Color(.8f,.7f,.2f));
            EditorGUI.DrawRect(new Rect(rect.x+unit*selected.Frames.Startup,rect.y,unit*selected.Frames.Active,25),new Color(.9f,.2f,.2f));
            EditorGUI.DrawRect(new Rect(rect.x+unit*(selected.Frames.Startup+selected.Frames.Active),rect.y,unit*selected.Frames.Recovery,25),new Color(.2f,.4f,.9f));
            EditorGUI.DrawRect(new Rect(rect.x+unit*selected.Cancel.Start,rect.y+28,unit*(selected.Cancel.End-selected.Cancel.Start+1),8),Color.green);
            EditorGUI.DrawRect(new Rect(rect.x+frame*unit,rect.y,2,40),Color.white);
            if(Event.current.type==EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            { frame=Mathf.Clamp((int)((Event.current.mousePosition.x-rect.x)/unit),0,selected.Frames.Total-1); Sample(); Event.current.Use(); }
        }
        private void CreatePreview()
        {
            if(previewObject==null)
            {
                previewObject=new GameObject("Attack Preview (temporary)");
                previewObject.hideFlags=HideFlags.DontSave;
                preview=previewObject.AddComponent<AttackPreviewObject>();
                preview.Animator=previewObject.AddComponent<Animator>();
            }
            preview.Attack=selected; preview.Frame=frame; preview.FaceLeft=mirror;
            Selection.activeGameObject=previewObject;
            SceneView.lastActiveSceneView?.Frame(new Bounds(previewObject.transform.position,Vector3.one*4),false);
            Sample();
        }
        private void Sample()
        {
            if(preview==null || selected==null) return;
            preview.Frame=frame;
            if(selected.Animation.Clip!=null)
            {
                if(!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(previewObject,selected.Animation.Clip,frame/60f);
                AnimationMode.EndSampling();
            }
            SceneView.RepaintAll();
        }
        private void StopPreview()
        {
            if(AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            if(previewObject!=null) DestroyImmediate(previewObject);
        }
    }
}
