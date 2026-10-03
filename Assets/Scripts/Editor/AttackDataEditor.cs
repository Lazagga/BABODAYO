using Babodayo.Combat;
using Babodayo.Player;
using UnityEditor;
using UnityEngine;
namespace Babodayo.Editor
{
    [CustomEditor(typeof(AttackData))]
    public sealed class AttackDataEditor : UnityEditor.Editor
    {
        private bool gameplay=true,hitbox=true,animation,vfx,audio;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ID"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Kind"));
            gameplay=EditorGUILayout.Foldout(gameplay,"Gameplay — frames / Tile (1 Unit)",true);
            if(gameplay)
            {
                foreach(string name in new[]{"Frames","Hit","Movement","Cancel","Shotgun"})
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(name),true);
                EditorGUILayout.HelpBox("Frames are zero-based. Cancel endpoints inclusive. Movement distance: Tile = Unity Unit. Jump cancel is independent of invulnerability.",MessageType.Info);
            }
            Draw("Hitboxes","Hitbox — offset/size in Tile",ref hitbox);
            Draw("Animation","Animation",ref animation); Draw("VFX","VFX",ref vfx); Draw("Audio","Audio",ref audio);
            serializedObject.ApplyModifiedProperties();
            foreach(string message in ((AttackData)target).Validate())
                EditorGUILayout.HelpBox(message,message.StartsWith("Error") ? MessageType.Error : MessageType.Warning);
            if(GUILayout.Button("Open Attack Editor")) AttackEditorWindow.Open((AttackData)target);
        }
        private void Draw(string property,string label,ref bool open)
        { open=EditorGUILayout.Foldout(open,label,true); if(open) EditorGUILayout.PropertyField(serializedObject.FindProperty(property),true); }
    }
    [CustomEditor(typeof(PlayerAttackLoadout))]
    public sealed class PlayerAttackLoadoutEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        { DrawDefaultInspector(); foreach(var message in ((PlayerAttackLoadout)target).Validate()) EditorGUILayout.HelpBox(message,MessageType.Error); }
    }
}
