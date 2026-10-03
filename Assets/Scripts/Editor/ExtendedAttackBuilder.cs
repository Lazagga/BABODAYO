using System;
using Babodayo.Combat;
using Babodayo.Player;
using UnityEditor;
using UnityEngine;
namespace Babodayo.Editor
{
    public static class ExtendedAttackBuilder
    {
        [MenuItem("Tools/Combat/Add Extended Attacks")]
        public static void Build()
        {
            var loadout=AssetDatabase.LoadAssetAtPath<PlayerAttackLoadout>("Assets/GameData/Attacks/PlayerLoadout.asset");
            if(loadout==null) throw new InvalidOperationException("Create the prototype loadout first.");
            if(loadout.RisingLauncher==null) loadout.RisingLauncher=Create("Shift_S_LMB_Hold",AttackKind.Launcher,6,8,16,18,.1f,8,a=>{
                a.Hit.Launch=true; a.Hit.LaunchVelocity=12; a.Hit.Hitstun=48; a.Hit.Knockback=new Vector2(.6f,0);
                a.Movement.SelfLaunch=true; a.Movement.SelfLaunchVelocity=12;
                a.Hitboxes[0].Offset=new Vector2(.6f,.35f); a.Hitboxes[0].Size=new Vector2(1.6f,2.2f);
            });
            if(loadout.Rush==null) loadout.Rush=Create("Shift_Direction_LMB",AttackKind.Heavy,10,10,20,22,3,7,a=>{
                a.Hit.Knockback=new Vector2(7,1); a.Hit.Hitstun=28;
                a.Hitboxes[0].Size=new Vector2(1.8f,1.4f);
            });
            if(loadout.UpThrust==null) loadout.UpThrust=Create("W_LMB",AttackKind.Light,6,6,14,13,0,4,a=>{
                a.Hit.Knockback=new Vector2(.4f,5); a.Hitboxes[0].Offset=new Vector2(.25f,1);
                a.Hitboxes[0].Size=new Vector2(1.1f,2.3f);
            });
            if(loadout.DashAttack==null) loadout.DashAttack=Create("DoubleTap_LMB",AttackKind.Heavy,4,8,18,20,2.4f,6,a=>{
                a.Hit.Knockback=new Vector2(6,1); a.Hit.Hitstun=26;
            });
            if(loadout.DiagonalDive==null) loadout.DiagonalDive=Create("Air_Direction_LMB",AttackKind.Dive,6,4,10,20,0,8,a=>{
                a.Movement.Dive=true; a.Movement.DiveSpeed=16; a.Movement.DiveHorizontalSpeed=10;
                a.Hitboxes[0].Offset=new Vector2(.65f,-.5f); a.Hitboxes[0].Size=new Vector2(1.7f,1.8f);
            });
            if(loadout.AirPierce==null) loadout.AirPierce=Create("Air_Shift_Direction_LMB",AttackKind.Heavy,7,12,15,22,4,7,a=>{
                a.Movement.LockY=true; a.Movement.YVelocity=0; a.Hit.Knockback=new Vector2(4,1);
                a.Hitboxes[0].Size=new Vector2(2,1.3f);
            });
            EditorUtility.SetDirty(loadout); AssetDatabase.SaveAssets();
        }
        private static AttackData Create(string id,AttackKind kind,int startup,int active,int recovery,float damage,float distance,int stop,Action<AttackData> configure)
        {
            string path="Assets/GameData/Attacks/"+id+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<AttackData>(path);
            if(existing!=null) return existing;
            var a=ScriptableObject.CreateInstance<AttackData>(); a.ID=id; a.Kind=kind;
            a.Frames.Startup=startup; a.Frames.Active=active; a.Frames.Recovery=recovery;
            a.Hit.Damage=damage; a.Hit.Hitstop=stop; a.Movement.Distance=distance;
            // Travel only during Active: startup and recovery preserve spacing.
            a.Movement.Curve=new AnimationCurve(new Keyframe(0,0),new Keyframe((float)startup/a.Frames.Total,0),
                new Keyframe((float)(startup+active)/a.Frames.Total,1),new Keyframe(1,1));
            a.Cancel.Start=startup+1; a.Cancel.End=a.Frames.Total-3;
            a.Hitboxes[0].Start=startup; a.Hitboxes[0].End=startup+active-1;
            a.VFX.CameraImpulse=AssetDatabase.LoadAssetAtPath<CameraImpulseProfile>("Assets/GameData/Settings/HeavyImpact.asset");
            configure(a); AssetDatabase.CreateAsset(a,path); EditorUtility.SetDirty(a); return a;
        }
    }
}
