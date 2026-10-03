using Babodayo.Combat;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
namespace Babodayo.Editor
{
    [CustomEditor(typeof(AttackPreviewObject))]
    public sealed class AttackPreviewObjectEditor : UnityEditor.Editor
    {
        private readonly BoxBoundsHandle handle=new BoxBoundsHandle();
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var preview=(AttackPreviewObject)target;
            if(preview.Attack!=null && GUILayout.Button("Edit asset")) Selection.activeObject=preview.Attack;
        }
        private void OnSceneGUI()
        {
            var preview=(AttackPreviewObject)target;
            if(preview.Attack==null) return;
            float facing=preview.FaceLeft ? -1 : 1;
            Handles.Label(preview.transform.position+Vector3.up*2,preview.Attack.ID+"  Frame "+preview.Frame+"  "+preview.Attack.Frames.Phase(preview.Frame));
            foreach(var box in preview.Attack.Hitboxes)
            {
                if(preview.Frame<box.Start || preview.Frame>box.End) continue;
                Vector3 center=preview.transform.TransformPoint(new Vector3(box.Offset.x*facing,box.Offset.y,0));
                Handles.color=Color.red;
                EditorGUI.BeginChangeCheck();
                Vector3 moved=Handles.PositionHandle(center,Quaternion.identity);
                handle.center=moved; handle.size=new Vector3(box.Size.x,box.Size.y,.02f); handle.SetColor(Color.red);
                handle.DrawHandle();
                if(EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(preview.Attack,"Edit attack hitbox");
                    Vector3 local=preview.transform.InverseTransformPoint(handle.center);
                    box.Offset=new Vector2(local.x*facing,local.y);
                    box.Size=new Vector2(Mathf.Max(.01f,handle.size.x),Mathf.Max(.01f,handle.size.y));
                    EditorUtility.SetDirty(preview.Attack);
                }
            }
            Handles.color=Color.cyan; Handles.DrawWireCube(preview.transform.position,new Vector3(.7f,1.6f,.02f));
        }
    }
}
