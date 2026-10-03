using System;
using System.IO;
using System.Linq;
using Babodayo.CameraSystem;
using Babodayo.Combat;
using Babodayo.Core;
using Babodayo.Enemy;
using Babodayo.Input;
using Babodayo.Player;
using Babodayo.Rank;
using Babodayo.Replay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
namespace Babodayo.Editor
{
    public static class CombatPrototypeBuilder
    {
        public const string ScenePath="Assets/Scenes/CombatTestScene.unity";
        private static Sprite square;
        private static Material lineMaterial;
        [MenuItem("Tools/Combat/Create Prototype Assets and Scene")]
        public static void Build()
        {
            if(Application.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            if(File.Exists(ScenePath)) { Debug.Log("CombatTestScene already exists. Existing designer assets and scene were preserved."); return; }
            foreach(var directory in new[]{"Assets/Scenes","Assets/GameData/Attacks","Assets/GameData/Enemies","Assets/GameData/Settings","Assets/Art/Prototype"})
                Directory.CreateDirectory(directory);
            CreateArt();
            var movement=Asset<PlayerMovementData>("Assets/GameData/Settings/PlayerMovement.asset");
            var rankData=Asset<StyleRankData>("Assets/GameData/Settings/StyleRank.asset");
            var highlights=Asset<HighlightSettings>("Assets/GameData/Settings/Highlights.asset");
            var impulse=Asset<CameraImpulseProfile>("Assets/GameData/Settings/LightImpact.asset");
            var heavyImpulse=Asset<CameraImpulseProfile>("Assets/GameData/Settings/HeavyImpact.asset");
            heavyImpulse.Amplitude=.12f; heavyImpulse.Duration=.16f;
            var loadout=Asset<PlayerAttackLoadout>("Assets/GameData/Attacks/PlayerLoadout.asset");
            loadout.GroundCombo=new[]{
                Attack("LMB_01",AttackKind.Light,6,4,14,10,.12f,4),
                Attack("LMB_02",AttackKind.Light,7,4,14,12,.18f,4),
                Attack("LMB_03",AttackKind.Light,9,5,18,18,.26f,5)};
            loadout.AirCombo=new[]{
                Attack("Air_LMB_01",AttackKind.Light,5,5,12,9,.08f,4),
                Attack("Air_LMB_02",AttackKind.Light,5,5,12,10,.1f,4),
                Attack("Air_LMB_03",AttackKind.Light,7,5,14,15,.16f,5)};
            foreach(var attack in loadout.AirCombo)
            { attack.Movement.LockY=true; attack.Movement.AirControl=.2f; attack.Hit.Knockback=new Vector2(.6f,3); attack.Hit.Hitstun=24; }
            loadout.Heavy=Attack("Shift_LMB",AttackKind.Heavy,18,5,26,28,.22f,9);
            loadout.Heavy.Hit.Knockback=new Vector2(7,2); loadout.Heavy.Hit.Hitstun=32; loadout.Heavy.Hit.StaggerDamage=25;
            loadout.Launcher=Attack("Shift_S_LMB",AttackKind.Launcher,8,5,20,14,.08f,8);
            loadout.Launcher.Hit.Launch=true; loadout.Launcher.Hit.LaunchVelocity=12; loadout.Launcher.Hit.Knockback=new Vector2(.8f,0); loadout.Launcher.Hit.Hitstun=48;
            loadout.Dive=Attack("Air_Shift_S_LMB",AttackKind.Dive,8,3,9,24,0,10);
            loadout.Dive.Movement.Dive=true; loadout.Dive.Movement.DiveSpeed=20;
            loadout.Dive.Hit.Knockback=new Vector2(3,0);
            loadout.Dive.Hitboxes[0].Offset=new Vector2(.25f,-.4f); loadout.Dive.Hitboxes[0].Size=new Vector2(2.4f,1.5f);
            loadout.GroundShot=Attack("RMB",AttackKind.Shotgun,5,1,16,14,0,5);
            loadout.AirShot=Attack("Air_RMB",AttackKind.Shotgun,5,1,16,14,0,5);
            loadout.GroundShot.Hit.Knockback=loadout.AirShot.Hit.Knockback=new Vector2(5,1);
            ExtendedAttackBuilder.Build();
            Attack("DummyAttack",AttackKind.Light,7,3,12,1,.1f,3);
            var fodderReaction=Asset<HitReactionProfile>("Assets/GameData/Enemies/FodderProfile.asset");
            var bossReaction=Asset<HitReactionProfile>("Assets/GameData/Enemies/BossProfile.asset");
            bossReaction.Light=HitReaction.DamageOnly; bossReaction.Heavy=HitReaction.StaggerDamage;
            bossReaction.Launcher=HitReaction.DamageOnly; bossReaction.Dive=HitReaction.StaggerDamage;
            bossReaction.Shotgun=HitReaction.DamageOnly; bossReaction.LaunchWhenPunished=false;
            var dummy=Asset<EnemyData>("Assets/GameData/Enemies/Dummy.asset");
            dummy.HP=180; dummy.Reactions=fodderReaction; dummy.AutoHealFrames=180; dummy.AI=false;
            var fodder=Asset<EnemyData>("Assets/GameData/Enemies/MudFodder.asset");
            fodder.HP=55; fodder.Reactions=fodderReaction; fodder.AI=true; fodder.MoveSpeed=1.8f;
            fodder.Attacks=new[]{Attack("MudCharge",AttackKind.Light,30,12,32,8,2.4f,4)};
            var boss=Asset<EnemyData>("Assets/GameData/Enemies/PrototypeBoss.asset");
            boss.HP=450; boss.Reactions=bossReaction; boss.AI=true; boss.Boss=true; boss.AttackRange=3; boss.AggroRange=8; boss.IdleFrames=35;
            boss.Attacks=new[]{
                Attack("BossQuick",AttackKind.Light,14,5,24,10,.3f,4),
                Attack("BossHeavy",AttackKind.Heavy,42,8,60,22,.5f,8),
                Attack("BossApproach",AttackKind.Heavy,28,14,40,14,3,6)};
            foreach(var attack in boss.Attacks) { attack.Hitboxes[0].Size=new Vector2(2.4f,2); attack.Hitboxes[0].Offset=new Vector2(1.1f,0); }
            foreach(var guid in AssetDatabase.FindAssets("t:AttackData",new[]{"Assets/GameData/Attacks"}))
            {
                var attack=AssetDatabase.LoadAssetAtPath<AttackData>(AssetDatabase.GUIDToAssetPath(guid));
                attack.VFX.CameraImpulse=attack.Kind==AttackKind.Heavy || attack.Kind==AttackKind.Dive ? heavyImpulse : impulse;
                EditorUtility.SetDirty(attack);
            }
            foreach(var asset in new UnityEngine.Object[]{movement,rankData,highlights,impulse,heavyImpulse,loadout,fodderReaction,bossReaction,dummy,fodder,boss}) EditorUtility.SetDirty(asset);
            var previous=SceneManager.GetActiveScene();
            bool replaceEmpty=string.IsNullOrEmpty(previous.path) && !previous.isDirty;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,replaceEmpty ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var systems=new GameObject("Combat Systems"); systems.SetActive(false);
                var clock=systems.AddComponent<CombatClock>(); var events=systems.AddComponent<CombatEvents>();
                var rank=systems.AddComponent<StyleRankSystem>(); rank.Clock=clock; rank.Data=rankData;
                var relay=systems.AddComponent<StyleEventRelay>(); relay.Events=events; relay.Rank=rank;
                var hooks=systems.AddComponent<CombatPresentationHooks>(); hooks.Events=events;
                BuildTerrain();
                var player=BuildPlayer(clock,events,movement,loadout);
                var enemies=new[]{BuildEnemy("Dummy",new Vector2(3,.85f),dummy,clock,events,player.transform,new Color(.4f,.7f,.8f)),
                    BuildEnemy("Mud Fodder",new Vector2(11,.85f),fodder,clock,events,player.transform,new Color(.5f,.35f,.22f)),
                    BuildEnemy("Prototype Boss",new Vector2(25,1.05f),boss,clock,events,player.transform,new Color(.6f,.25f,.5f))};
                var combat=player.GetComponent<PlayerCombat>();
                var cameraObject=new GameObject("Main Camera");
                cameraObject.tag="MainCamera";
                var camera=cameraObject.AddComponent<UnityEngine.Camera>(); camera.orthographic=true; camera.orthographicSize=5;
                camera.backgroundColor=new Color(.055f,.075f,.11f); camera.clearFlags=CameraClearFlags.SolidColor;
                camera.transform.position=new Vector3(1,2.5f,-10); cameraObject.AddComponent<AudioListener>();
                player.FacingControl=player.gameObject.AddComponent<PlayerFacing>();
                player.FacingControl.ViewCamera=camera;
                player.FacingControl.Visual=player.GetComponentInChildren<SpriteRenderer>();
                var marker=new GameObject("Facing Marker"); marker.transform.SetParent(player.transform,false);
                marker.transform.localPosition=new Vector3(.28f,.35f,-.01f); marker.transform.localScale=new Vector3(.12f,.12f,1);
                var markerSprite=marker.AddComponent<SpriteRenderer>(); markerSprite.sprite=player.FacingControl.Visual.sprite;
                markerSprite.color=Color.white; markerSprite.sortingOrder=6; player.FacingControl.DirectionMarker=marker.transform;
                var roomObject=new GameObject("Boss Combat Room"); roomObject.transform.position=new Vector3(24,3,0);
                var room=roomObject.AddComponent<CameraRoom>(); room.Size=new Vector2(22,12); room.CenterWeight=.8f;
                var follow=cameraObject.AddComponent<CameraController>(); follow.Player=player; follow.Combat=combat; follow.Events=events; follow.Rooms=new[]{room};
                var recorder=systems.AddComponent<CombatRecorder>(); recorder.Clock=clock; recorder.Player=player; recorder.Combat=combat;
                recorder.Enemies=enemies; recorder.Rank=rank; recorder.Events=events;
                var replay=systems.AddComponent<HighlightReplayPlayer>(); replay.Recorder=recorder; replay.Settings=highlights; replay.Clock=clock; replay.View=camera; replay.CameraFollow=follow;
                replay.Originals=new[]{player.GetComponentInChildren<SpriteRenderer>()}.Concat(enemies.Select(e=>e.Visual)).ToArray();
                var ui=systems.AddComponent<CombatDebugUI>(); ui.Clock=clock; ui.Player=player; ui.Combat=combat; ui.Enemies=enemies; ui.Rank=rank; ui.Replay=replay;
                var references=systems.AddComponent<CombatTestReferences>(); references.Clock=clock; references.Player=player; references.Combat=combat;
                references.Enemies=enemies; references.Rank=rank; references.Recorder=recorder; references.Replay=replay;
                systems.SetActive(true);
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            finally { if(!replaceEmpty) { EditorSceneManager.CloseScene(scene,true); if(previous.IsValid()) SceneManager.SetActiveScene(previous); } }
            if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))
                EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Combat prototype created: "+ScenePath);
        }
        private static T Asset<T>(string path) where T:ScriptableObject
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);
            if(asset!=null) return asset;
            asset=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset,path); return asset;
        }
        private static AttackData Attack(string id,AttackKind kind,int startup,int active,int recovery,float damage,float distance,int hitstop)
        {
            string path="Assets/GameData/Attacks/"+id+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<AttackData>(path);
            if(existing!=null) return existing;
            var data=Asset<AttackData>(path); data.ID=id; data.Kind=kind;
            data.Frames.Startup=startup; data.Frames.Active=active; data.Frames.Recovery=recovery;
            data.Hit.Damage=damage; data.Hit.Hitstop=hitstop; data.Movement.Distance=distance;
            data.Cancel.Start=startup+1; data.Cancel.End=startup+active+recovery-3;
            data.Hitboxes[0].Start=startup; data.Hitboxes[0].End=startup+active-1;
            EditorUtility.SetDirty(data); return data;
        }
        private static void CreateArt()
        {
            string path="Assets/Art/Prototype/Square.png";
            if(!File.Exists(path))
            {
                var texture=new Texture2D(16,16); texture.SetPixels(Enumerable.Repeat(Color.white,256).ToArray()); texture.Apply();
                File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
            }
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite;
            importer.spritePixelsPerUnit=16; importer.filterMode=FilterMode.Point; importer.SaveAndReimport();
            square=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            const string materialPath="Assets/Art/Prototype/CombatLines.mat";
            lineMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(lineMaterial==null) { lineMaterial=new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(lineMaterial,materialPath); }
        }
        private static SpriteRenderer Visual(Transform parent,Vector2 size,Color color)
        {
            var child=new GameObject("Visual"); child.transform.SetParent(parent,false);
            child.transform.localScale=new Vector3(size.x,size.y,1);
            var sprite=child.AddComponent<SpriteRenderer>(); sprite.sprite=square; sprite.color=color; sprite.sortingOrder=2;
            return sprite;
        }
        private static DamageReceiver Receiver(GameObject actor,CombatClock clock,CombatEvents events,CombatTeam team,Vector2 size)
        {
            var receiver=actor.AddComponent<DamageReceiver>(); receiver.Team=team; receiver.Events=events;
            receiver.Invulnerability=actor.AddComponent<Invulnerability>(); receiver.Hitstop=actor.AddComponent<HitstopController>(); receiver.Hitstop.Clock=clock;
            var child=new GameObject("Hurtbox"); child.layer=2; child.transform.SetParent(actor.transform,false);
            var collider=child.AddComponent<BoxCollider2D>(); collider.size=size; collider.isTrigger=true;
            child.AddComponent<Hurtbox>().Receiver=receiver;
            return receiver;
        }
        private static Hitbox Box(Transform parent)
        {
            var child=new GameObject("Hitbox"); child.layer=2; child.transform.SetParent(parent,false);
            var collider=child.AddComponent<BoxCollider2D>(); collider.isTrigger=true; collider.enabled=false;
            var hitbox=child.AddComponent<Hitbox>(); hitbox.TargetMask=1<<2; hitbox.TerrainMask=1;
            hitbox.Outline=Line(child.transform,"Active Box",Color.red);
            hitbox.PelletLines=new LineRenderer[7];
            for(int i=0;i<7;i++) hitbox.PelletLines[i]=Line(child.transform,"Pellet "+i,Color.yellow);
            return hitbox;
        }
        private static LineRenderer Line(Transform parent,string name,Color color)
        {
            var child=new GameObject(name); child.transform.SetParent(parent,false);
            var line=child.AddComponent<LineRenderer>(); line.sharedMaterial=lineMaterial; line.startColor=line.endColor=color;
            line.startWidth=line.endWidth=.025f; line.sortingOrder=5; line.useWorldSpace=true; line.enabled=false; return line;
        }
        private static PlayerController BuildPlayer(CombatClock clock,CombatEvents events,PlayerMovementData movement,PlayerAttackLoadout loadout)
        {
            var actor=new GameObject("Player"); actor.SetActive(false); actor.layer=8; actor.transform.position=new Vector3(0,.85f,0);
            var body=actor.AddComponent<Rigidbody2D>(); var shape=actor.AddComponent<BoxCollider2D>(); shape.size=new Vector2(.7f,1.6f);
            var ground=actor.AddComponent<PlayerGroundDetector>(); ground.Body=shape; ground.GroundMask=1;
            var motor=actor.AddComponent<PlayerMotor>(); motor.Body=body; motor.Shape=shape; motor.Data=movement; motor.Ground=ground;
            var reader=actor.AddComponent<PlayerInputReader>(); var player=actor.AddComponent<PlayerController>(); player.Configure(clock,reader,motor);
            var receiver=Receiver(actor,clock,events,CombatTeam.Player,shape.size); receiver.AfterHitInvulnerability=45;
            var combat=actor.AddComponent<PlayerCombat>(); combat.Player=player; combat.Loadout=loadout; combat.Receiver=receiver; combat.Hitbox=Box(actor.transform);
            combat.Presentation=actor.AddComponent<AttackPresentation>();
            Visual(actor.transform,shape.size,new Color(.2f,.8f,.95f));
            actor.SetActive(true); return player;
        }
        private static EnemyController BuildEnemy(string name,Vector2 position,EnemyData data,CombatClock clock,CombatEvents events,Transform target,Color color)
        {
            var actor=new GameObject(name); actor.SetActive(false); actor.layer=8; actor.transform.position=position;
            Vector2 size=data.Boss ? new Vector2(1.1f,2) : new Vector2(.8f,1.6f);
            var body=actor.AddComponent<Rigidbody2D>(); var shape=actor.AddComponent<BoxCollider2D>(); shape.size=size;
            var motor=actor.AddComponent<EnemyMotor>(); motor.Body=body; motor.Shape=shape; motor.GroundMask=1;
            var receiver=Receiver(actor,clock,events,CombatTeam.Enemy,size); receiver.MaxHP=data.HP;
            var enemy=actor.AddComponent<EnemyController>(); enemy.Clock=clock; enemy.Data=data; enemy.Motor=motor; enemy.Target=target; enemy.Receiver=receiver;
            enemy.Hitbox=Box(actor.transform); enemy.Visual=Visual(actor.transform,size,color); enemy.Presentation=actor.AddComponent<AttackPresentation>();
            actor.SetActive(true); return enemy;
        }
        private static void BuildTerrain()
        {
            var grid=new GameObject("Stage Grid"); grid.AddComponent<Grid>();
            var terrain=new GameObject("Tilemap Ground"); terrain.transform.SetParent(grid.transform,false);
            var tilemap=terrain.AddComponent<Tilemap>(); terrain.AddComponent<TilemapRenderer>();
            var body=terrain.AddComponent<Rigidbody2D>(); body.bodyType=RigidbodyType2D.Static;
            var composite=terrain.AddComponent<CompositeCollider2D>(); composite.geometryType=CompositeCollider2D.GeometryType.Polygons;
            var collider=terrain.AddComponent<TilemapCollider2D>();
            var tile=Asset<Tile>("Assets/Art/Prototype/GroundTile.asset"); tile.sprite=square; tile.color=new Color(.2f,.27f,.34f); tile.colliderType=Tile.ColliderType.Grid;
            for(int x=-18;x<=38;x++) for(int y=-3;y<0;y++) tilemap.SetTile(new Vector3Int(x,y,0),tile);
            // Elevated training ledges, with open corners for correction/coyote checks.
            for(int x=-10;x<=-7;x++) tilemap.SetTile(new Vector3Int(x,2,0),tile);
            for(int x=-4;x<=-2;x++) tilemap.SetTile(new Vector3Int(x,1,0),tile);
            for(int y=0;y<8;y++) { tilemap.SetTile(new Vector3Int(-19,y,0),tile); tilemap.SetTile(new Vector3Int(39,y,0),tile); }
            tilemap.RefreshAllTiles(); collider.ProcessTilemapChanges();
            collider.compositeOperation=Collider2D.CompositeOperation.Merge; composite.GenerateGeometry();
            EditorUtility.SetDirty(tile);
        }
    }
}
