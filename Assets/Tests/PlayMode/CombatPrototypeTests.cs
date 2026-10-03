using System.Collections;
using System.Linq;
using Babodayo.Combat;
using Babodayo.Core;
using Babodayo.Enemy;
using Babodayo.Input;
using Babodayo.Player;
using Babodayo.Player.StateMachine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Babodayo.Tests
{
    public sealed class CombatPrototypeTests
    {
        private CombatTestReferences scene;
        private PlayerController player;
        private PlayerMovementData tuning;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("CombatTestScene",LoadSceneMode.Single);
            yield return null;
            scene=SceneManager.GetActiveScene().GetRootGameObjects().Select(go=>go.GetComponent<CombatTestReferences>()).First(x=>x!=null);
            scene.Clock.enabled=false; player=scene.Player;
            player.GetComponent<PlayerInputReader>().enabled=false;
            player.Facing=1; // Ignore the live cursor before the reader was disabled.
            tuning=Object.Instantiate(player.Motor.Data); player.Motor.Data=tuning;
            scene.Combat.Receiver.Invulnerability.Clear();
            Step(4);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if(scene!=null && scene.Replay.Active) scene.Replay.End(); Object.Destroy(tuning); yield return null; }
        private void Step(int n=1) { for(int i=0;i<n;i++) scene.Clock.Step(); }
        private void Press(CombatCommand command)
        { player.Commands.Push(command,new InputFrame(scene.Clock.CurrentFrame,0,0,InputButtons.None,InputButtons.None,InputButtons.None)); }
        private void Place(Vector2 point)
        { player.Motor.Freeze(false); player.Motor.Teleport(point); player.Motor.SetVelocity(Vector2.zero); Physics2D.SyncTransforms(); Step(2); }

        [TestCase(CombatCommand.Shift_S_LMB_Hold,false,"Shift_S_LMB_Hold")]
        [TestCase(CombatCommand.Shift_Direction_LMB,false,"Shift_Direction_LMB")]
        [TestCase(CombatCommand.W_LMB,false,"W_LMB")]
        [TestCase(CombatCommand.DoubleTapD_LMB,false,"DoubleTap_LMB")]
        [TestCase(CombatCommand.Direction_LMB,true,"Air_Direction_LMB")]
        [TestCase(CombatCommand.Shift_Direction_LMB,true,"Air_Shift_Direction_LMB")]
        public void ExtendedAttackRunsHitsAndReturns(CombatCommand command,bool air,string id)
        {
            Place(new Vector2(1.8f,air ? 2.3f : .81f));
            var dummy=scene.Enemies[0];
            if(id=="W_LMB") dummy.Motor.Teleport(new Vector2(2,1.6f));
            if(id=="Air_Shift_Direction_LMB") dummy.Motor.Teleport(new Vector2(3,2.3f));
            Physics2D.SyncTransforms(); float hp=dummy.Receiver.HP;
            Press(command); Step(); Assert.That(scene.Combat.Current.Data.ID,Is.EqualTo(id));
            bool rose=false,diagonal=false;
            for(int f=0;f<95;f++)
            {
                Step(); rose|=player.Motor.Velocity.y>10;
                diagonal|=player.Motor.Velocity.x>5 && player.Motor.Velocity.y<-10;
            }
            Assert.That(dummy.Receiver.HP,Is.LessThan(hp),id+" must hit");
            Assert.That(scene.Combat.Current,Is.Null);
            if(id=="Shift_S_LMB_Hold") Assert.That(rose,Is.True);
            if(id=="Air_Direction_LMB") Assert.That(diagonal,Is.True);
        }
        [Test] public void DashAttackInterruptsDashAndUsesMouseFacing()
        {
            Place(new Vector2(-14,.81f)); Press(CombatCommand.DoubleTapD); Step(4);
            player.Facing=-1; Press(CombatCommand.DoubleTapD_LMB); Step();
            Assert.That(scene.Combat.Current.Data,Is.EqualTo(scene.Combat.Loadout.DashAttack));
            float x=player.Motor.Position.x; Step(20);
            Assert.That(player.Motor.Position.x,Is.LessThan(x-1));
        }

        [TestCase(false)] [TestCase(true)]
        public void FallingThroughEnemyReachesTerrainAndRestoresActions(bool dive)
        {
            var dummy=scene.Enemies[0];
            Place(new Vector2(dummy.transform.position.x,4));
            if(dive) Press(CombatCommand.Shift_S_LMB);
            Step(100);
            Assert.That(player.Motor.Grounded,Is.True);
            Assert.That(scene.Combat.Current,Is.Null);
            Assert.That(player.Motor.Position.y,Is.LessThan(1));
            Press(CombatCommand.Space); Step();
            Assert.That(player.States.Current,Is.TypeOf<JumpState>());
            Assert.That(player.Motor.Velocity.y,Is.GreaterThan(0));
        }
        [Test] public void CursorFacingOverridesMovementForAttackAndMirrorsHitbox()
        {
            var facing=player.FacingControl;
            Assert.That(facing,Is.Not.Null);
            Vector2 left=facing.ViewCamera.WorldToScreenPoint(player.transform.position+Vector3.left*3);
            var input=new InputFrame(1,1,0,InputButtons.None,InputButtons.None,InputButtons.None,
                pointerPosition:left,hasPointer:true);
            Assert.That(facing.Resolve(input,player.transform.position,1,true),Is.EqualTo(1));
            Assert.That(facing.Resolve(input,player.transform.position,1,false),Is.EqualTo(-1));
            var idle=new InputFrame(2,0,0,InputButtons.None,InputButtons.None,InputButtons.None,
                pointerPosition:left,hasPointer:true);
            Assert.That(facing.Resolve(idle,player.transform.position,1,true),Is.EqualTo(-1));
            Press(CombatCommand.LMB); Step();
            player.Facing=-1; Step(scene.Combat.Current.Data.Frames.Startup+1);
            Assert.That(scene.Combat.Hitbox.GetComponent<BoxCollider2D>().offset.x,Is.LessThan(0));
            player.Facing=1; Step();
            Assert.That(scene.Combat.Hitbox.GetComponent<BoxCollider2D>().offset.x,Is.GreaterThan(0));
        }
        [Test] public void JumpDoubleJumpAndLandingRestoreResources()
        {
            Assert.That(player.Motor.Grounded,Is.True);
            Press(CombatCommand.Space); Step();
            Assert.That(player.Motor.Velocity.y,Is.EqualTo(tuning.JumpVelocity).Within(.01));
            Step(8); Press(CombatCommand.Space); Step();
            Assert.That(player.Motor.Velocity.y,Is.EqualTo(tuning.DoubleJumpVelocity).Within(.01));
            Assert.That(player.Locomotion.AirJumpsRemaining,Is.Zero);
            Step(130);
            Assert.That(player.Motor.Grounded,Is.True);
            Assert.That(player.Locomotion.AirJumpsRemaining,Is.EqualTo(1));
        }
        [Test] public void CoyoteJumpAtSixFramesUsesGroundJump()
        {
            Place(new Vector2(-6.5f,3.81f));
            Assert.That(player.Motor.Grounded,Is.True);
            player.Motor.Teleport(new Vector2(-5.4f,3.81f)); Physics2D.SyncTransforms();
            Step(5); Press(CombatCommand.Space); Step();
            Assert.That(player.Motor.Velocity.y,Is.EqualTo(tuning.JumpVelocity).Within(.01));
            Assert.That(player.Locomotion.AirJumpsRemaining,Is.EqualTo(1));
        }
        [Test] public void LandingBufferJumpsWithoutRecovery()
        {
            tuning.AirJumps=0;
            Place(new Vector2(0,1.05f)); player.Motor.SetVerticalVelocity(-4);
            Press(CombatCommand.Space);
            bool jumped=false;
            for(int i=0;i<8;i++) { Step(); if(player.Motor.Velocity.y>10) { jumped=true; break; } }
            Assert.That(jumped,Is.True);
        }
        [Test] public void GroundAndAirDashHaveFiniteDurationAndAirLimit()
        {
            Press(CombatCommand.DoubleTapD); Step();
            Assert.That(player.States.Current,Is.TypeOf<GroundDashState>());
            Step(tuning.GroundDashStartup+2); Assert.That(player.Motor.Velocity.x,Is.EqualTo(tuning.DashSpeed).Within(.01));
            Step(tuning.GroundDashDuration+2);
            Assert.That(player.States.Current,Is.Not.TypeOf<GroundDashState>());
            Place(new Vector2(-12,4)); Press(CombatCommand.DoubleTapA); Step();
            Assert.That(player.States.Current,Is.TypeOf<AirDashState>()); Assert.That(player.Locomotion.AirDashesRemaining,Is.Zero);
            Step(tuning.AirDashStartup+1); Assert.That(player.Motor.Velocity.y,Is.EqualTo(0).Within(.01));
            Step(130); Assert.That(player.Locomotion.AirDashesRemaining,Is.EqualTo(1));
        }
        [Test] public void OneAttackHitsOnceAndJumpCancelDisablesHitbox()
        {
            Place(new Vector2(1.8f,.81f)); var dummy=scene.Enemies[0]; float hp=dummy.Receiver.HP;
            Press(CombatCommand.LMB); Step(22);
            Assert.That(dummy.Receiver.HP,Is.EqualTo(hp-scene.Combat.Loadout.GroundCombo[0].Hit.Damage).Within(.01));
            Press(CombatCommand.Space); Step();
            Assert.That(player.States.Current,Is.TypeOf<JumpState>());
            Assert.That(scene.Combat.Hitbox.GetComponent<BoxCollider2D>().enabled,Is.False);
        }
        [Test] public void GroundComboConsumesRepeatedLmbAndWhiffMoves()
        {
            Place(new Vector2(-14,.81f));
            float start=player.transform.position.x;
            Press(CombatCommand.LMB); Step();
            Step(5); Press(CombatCommand.LMB); Step(3);
            Assert.That(scene.Combat.Current.ComboIndex,Is.EqualTo(1));
            Step(5); Press(CombatCommand.LMB); Step(4);
            Assert.That(scene.Combat.Current.ComboIndex,Is.EqualTo(2));
            Step(40);
            Assert.That(player.transform.position.x,Is.GreaterThan(start+.1f));
        }
        [Test] public void LauncherLaunchesAndBossNeutralResistsSameMove()
        {
            var launcher=scene.Combat.Loadout.Launcher;
            var dummy=scene.Enemies[0]; var boss=scene.Enemies[2];
            dummy.Receiver.Receive(new HitResult(scene.Combat.Receiver,launcher,1,scene.Clock.CurrentFrame,1,dummy.transform.position));
            boss.Receiver.Receive(new HitResult(scene.Combat.Receiver,launcher,2,scene.Clock.CurrentFrame,1,boss.transform.position));
            Assert.That(dummy.States.Current,Is.EqualTo(EnemyState.Launched));
            Assert.That(boss.States.Current,Is.EqualTo(EnemyState.Neutral));
            Assert.That(boss.Receiver.HP,Is.LessThan(boss.Receiver.MaxHP));
            boss.States.Change(EnemyState.AttackRecovery);
            boss.Receiver.Receive(new HitResult(scene.Combat.Receiver,scene.Combat.Loadout.GroundCombo[0],3,scene.Clock.CurrentFrame,1,boss.transform.position));
            Assert.That(boss.States.Current,Is.EqualTo(EnemyState.HitStun));
        }
        [Test] public void ShotgunHasVisibleSpreadAndSingleIntegratedDamage()
        {
            Place(new Vector2(1,.81f)); float hp=scene.Enemies[0].Receiver.HP;
            Press(CombatCommand.RMB); Step(9);
            Assert.That(scene.Enemies[0].Receiver.HP,Is.EqualTo(hp-scene.Combat.Loadout.GroundShot.Hit.Damage).Within(.01));
            Assert.That(scene.Combat.Hitbox.PelletLines.Count(line=>line.enabled),Is.EqualTo(7));
        }
        [Test] public void HitstopFreezesActorButClockAndInputBufferAdvance()
        {
            scene.Combat.Receiver.Hitstop.Request(4); int frame=scene.Clock.CurrentFrame;
            Vector3 position=player.transform.position;
            Press(CombatCommand.Space); Step(3);
            Assert.That(scene.Clock.CurrentFrame,Is.EqualTo(frame+3)); Assert.That(player.transform.position,Is.EqualTo(position));
            Assert.That(player.Commands.Remaining(CombatCommand.Space,scene.Clock.CurrentFrame),Is.GreaterThan(0));
            Step(2); Assert.That(player.Motor.Velocity.y,Is.GreaterThan(0));
        }
        [Test] public void RecorderCapturesPostPhysicsAndReplayDoesNotRunGameplay()
        {
            Press(CombatCommand.Space); Step(10);
            var frames=scene.Recorder.CopyChronological();
            Assert.That(frames.Last().Player.Position,Is.EqualTo((Vector2)player.transform.position));
            Assert.That(frames.Any(f=>f.Events.Any(e=>e.Kind=="Command")),Is.True);
            Assert.That(frames.Last().Enemies.Length,Is.EqualTo(3));
        }
        [Test] public void LedgeForgivenessSupportsOnlySmallOverhangWithoutSnapping()
        {
            Place(new Vector2(-5.61f,3.81f));
            Assert.That(player.Motor.Grounded,Is.True);
            Assert.That(player.transform.position.x,Is.EqualTo(-5.61f).Within(.001));
            Place(new Vector2(-5.4f,3.81f));
            Assert.That(player.Motor.Grounded,Is.False);
        }
        [Test] public void CornerCorrectionClearsSmallHeadOverlapButNotBroadCeiling()
        {
            Place(new Vector2(-12,2));
            var obstacle=new GameObject("Corner test"); var collider=obstacle.AddComponent<BoxCollider2D>();
            collider.size=new Vector2(1,.3f); obstacle.transform.position=new Vector2(-11.2f,2.98f);
            Physics2D.SyncTransforms(); player.Motor.SetVerticalVelocity(12);
            float before=player.Motor.Position.x; player.Motor.CorrectCorners(false,1);
            Assert.That(player.Motor.Position.x,Is.LessThan(before));
            Assert.That(before-player.Motor.Position.x,Is.LessThanOrEqualTo(tuning.CornerCorrection+.001));
            Object.DestroyImmediate(obstacle);
            player.Motor.Teleport(new Vector2(-12,2));
            obstacle=new GameObject("Broad ceiling"); collider=obstacle.AddComponent<BoxCollider2D>();
            collider.size=new Vector2(3,.3f); obstacle.transform.position=new Vector2(-12,2.98f);
            Physics2D.SyncTransforms(); player.Motor.CorrectCorners(false,1);
            Assert.That(player.Motor.Position.x,Is.EqualTo(-12).Within(.001));
            Object.DestroyImmediate(obstacle);
        }
        [Test] public void DashCornerCorrectionMovesOnlyIntoClearSpace()
        {
            Place(new Vector2(-12,2));
            var obstacle=new GameObject("Dash corner"); var collider=obstacle.AddComponent<BoxCollider2D>();
            collider.size=new Vector2(1,.3f); obstacle.transform.position=new Vector2(-11.05f,1.17f);
            Physics2D.SyncTransforms(); player.Motor.SetHorizontalVelocity(tuning.DashSpeed);
            float before=player.Motor.Position.y; player.Motor.CorrectCorners(true,1);
            Assert.That(player.Motor.Position.y,Is.GreaterThan(before));
            Assert.That(player.Motor.Position.y-before,Is.LessThanOrEqualTo(tuning.DashCornerCorrection+.001));
            Object.DestroyImmediate(obstacle);
        }
        [Test] public void LauncherJumpAirComboAndDiveConnectWithoutTargetAssists()
        {
            Place(new Vector2(1.8f,.81f));
            var dummy=scene.Enemies[0]; float hp=dummy.Receiver.HP;
            Press(CombatCommand.Shift_S_LMB);
            for(int i=0;i<30 && dummy.States.Current!=EnemyState.Launched;i++) Step();
            Assert.That(dummy.States.Current,Is.EqualTo(EnemyState.Launched));
            Press(CombatCommand.Space);
            for(int i=0;i<16 && !(player.States.Current is JumpState);i++) Step();
            Assert.That(player.States.Current,Is.TypeOf<JumpState>());
            Step(10); Press(CombatCommand.LMB); Step();
            Assert.That(scene.Combat.Current.Data.ID,Is.EqualTo("Air_LMB_01"));
            for(int combo=1;combo<=2;combo++)
            {
                for(int i=0;i<30 && scene.Combat.Current!=null && scene.Combat.Current.AttackFrame<4;i++) Step();
                Press(CombatCommand.LMB);
                for(int i=0;i<25 && scene.Combat.Current!=null && scene.Combat.Current.ComboIndex<combo;i++) Step();
                Assert.That(scene.Combat.Current.ComboIndex,Is.EqualTo(combo));
            }
            Step(12); Press(CombatCommand.Shift_S_LMB);
            for(int i=0;i<15 && (scene.Combat.Current==null || scene.Combat.Current.Data!=scene.Combat.Loadout.Dive);i++) Step();
            Assert.That(scene.Combat.Current.Data,Is.SameAs(scene.Combat.Loadout.Dive));
            Step(110); Assert.That(player.Motor.Grounded,Is.True);
            Assert.That(dummy.Receiver.HP,Is.LessThan(hp-scene.Combat.Loadout.Launcher.Hit.Damage-10));
            var hitMoves=scene.Recorder.CopyChronological().SelectMany(f=>f.Events).Where(e=>e.Kind=="Hit").Select(e=>e.MoveID).ToArray();
            foreach(var attack in scene.Combat.Loadout.AirCombo) Assert.That(hitMoves,Does.Contain(attack.ID),"Every air combo hit should connect at the authored spacing.");
        }
        [Test] public void LeftFacingMirrorsMeleeAndAirRmbUsesAirAttack()
        {
            Place(new Vector2(4.2f,.81f)); player.Facing=-1;
            float hp=scene.Enemies[0].Receiver.HP; Press(CombatCommand.LMB); Step(22);
            Assert.That(scene.Enemies[0].Receiver.HP,Is.LessThan(hp));
            Place(new Vector2(1,3)); player.Facing=1;
            scene.Enemies[0].Motor.Freeze(false); scene.Enemies[0].Motor.Teleport(new Vector2(3,3)); Physics2D.SyncTransforms();
            hp=scene.Enemies[0].Receiver.HP; Press(CombatCommand.RMB); Step();
            Assert.That(scene.Combat.Current.Data,Is.SameAs(scene.Combat.Loadout.AirShot));
            Step(8); Assert.That(scene.Enemies[0].Receiver.HP,Is.LessThan(hp));
        }
        [Test] public void InvulnerabilityRejectsRepeatDamageAndDeathStopsActions()
        {
            var source=scene.Enemies[1].Receiver; var attack=scene.Enemies[1].Data.Attacks[0];
            var receiver=scene.Combat.Receiver; float hp=receiver.HP;
            var hit=new HitResult(source,attack,1,scene.Clock.CurrentFrame,1,player.transform.position);
            Assert.That(receiver.Receive(hit),Is.True); Assert.That(receiver.Receive(hit),Is.False);
            Assert.That(receiver.HP,Is.EqualTo(hp-attack.Hit.Damage));
            receiver.Invulnerability.Clear();
            receiver.Receive(new HitResult(source,attack,2,scene.Clock.CurrentFrame,1,player.transform.position,100));
            Assert.That(player.States.Current,Is.TypeOf<DeadState>());
            Press(CombatCommand.LMB); Step(20);
            Assert.That(player.States.Current,Is.TypeOf<DeadState>());
        }
        [Test] public void HighlightGhostPlaybackPausesRealGameplayAndRestoresIt()
        {
            var original=scene.Replay.Settings;
            var settings=Object.Instantiate(original); settings.WindowFrames=3; settings.MinimumRankGain=0;
            scene.Replay.Settings=settings;
            Step(5); Assert.That(scene.Replay.Begin(),Is.True);
            Assert.That(scene.Clock.enabled,Is.False);
            Assert.That(scene.Replay.Originals.All(s=>!s.enabled),Is.True);
            Assert.That(scene.Recorder.Recording,Is.False);
            scene.Replay.Paused=true; scene.Replay.Restart(); Assert.That(scene.Replay.Paused,Is.False);
            scene.Replay.End(); Assert.That(scene.Recorder.Recording,Is.True);
            Assert.That(scene.Replay.Originals.All(s=>s.enabled),Is.True);
            scene.Replay.Settings=original; Object.Destroy(settings);
        }
    }
}
