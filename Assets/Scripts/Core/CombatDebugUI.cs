using Babodayo.Combat;
using Babodayo.Enemy;
using Babodayo.Input;
using Babodayo.Player;
using Babodayo.Rank;
using Babodayo.Replay;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Babodayo.Core
{
    public sealed class CombatDebugUI : MonoBehaviour
    {
        [Tooltip("Clock to display.")] public CombatClock Clock;
        [Tooltip("Player state source.")] public PlayerController Player;
        [Tooltip("Attack/health source.")] public PlayerCombat Combat;
        [Tooltip("Enemies shown in diagnostic panel.")] public EnemyController[] Enemies;
        [Tooltip("Style grade always appears at screen top.")] public StyleRankSystem Rank;
        [Tooltip("Highlight results controls.")] public HighlightReplayPlayer Replay;
        [Tooltip("Enable diagnostic panel in non-development builds too.")] public bool AllowReleaseDebug;
        [Tooltip("Initial diagnostic panel visibility.")] public bool Visible=true;
        private readonly InputAction toggle=new InputAction("Debug",InputActionType.Button,"<Keyboard>/f1");
        private readonly InputAction highlight=new InputAction("Highlight",InputActionType.Button,"<Keyboard>/f2");
        private void OnEnable() { toggle.Enable(); highlight.Enable(); }
        private void OnDisable() { toggle.Disable(); highlight.Disable(); }
        private void OnDestroy() { toggle.Dispose(); highlight.Dispose(); }
        private void Update()
        {
            if(toggle.WasPressedThisFrame()) Visible=!Visible;
            if(highlight.WasPressedThisFrame()) { if(Replay.Active) Replay.End(); else Replay.Begin(); }
        }
        private void OnGUI()
        {
            if(Player==null || Player.States==null) return;
            GUI.Label(new Rect(Screen.width/2-120,12,420,28),"STYLE "+Rank.Rank+"   "+Rank.Score.ToString("0")+"   HP "+Combat.Receiver.HP.ToString("0"));
            if(Replay.Active)
            {
                GUILayout.BeginArea(new Rect(Screen.width/2-190,50,380,160),GUI.skin.box);
                GUILayout.Label(Replay.Status);
                if(GUILayout.Button(Replay.Paused ? "Play" : "Pause")) Replay.Paused=!Replay.Paused;
                if(GUILayout.Button("Replay from start")) Replay.Restart();
                if(GUILayout.Button("Skip / return to training")) Replay.End();
                GUILayout.EndArea(); return;
            }
            if(!Visible || (!Application.isEditor && !Debug.isDebugBuild && !AllowReleaseDebug)) return;
            GUILayout.BeginArea(new Rect(12,40,400,Screen.height-50),GUI.skin.box);
            GUILayout.Label("BABODAYO — Combat Training");
            GUILayout.Label("Frame "+Clock.CurrentFrame+"   State "+Player.States.Name+"   Command "+Player.LastCommand);
            GUILayout.Label("Attack "+(Combat.Current?.Data.ID ?? "-")+"   Attack frame "+(Combat.Current?.AttackFrame ?? 0));
            GUILayout.Label("Velocity "+Player.Motor.Velocity+"  Grounded "+Player.Motor.Grounded);
            GUILayout.Label("Coyote "+Player.Locomotion.CoyoteRemaining+"F   Jump buffer "+Player.Commands.Remaining(CombatCommand.Space,Clock.CurrentFrame)+"F");
            GUILayout.Label("Air jumps "+Player.Locomotion.AirJumpsRemaining+"   Air dashes "+Player.Locomotion.AirDashesRemaining);
            GUILayout.Label("Combo "+Rank.ComboCount+"   Rank delta "+Rank.LastDelta.ToString("0.0"));
            foreach(var enemy in Enemies)
                GUILayout.Label(enemy.name+": "+enemy.States.Current+"  HP "+enemy.Receiver.HP.ToString("0")+"  Stun "+enemy.States.Remaining+"F  Gravity x"+enemy.GravityMultiplier.ToString("0.00")+"  Attack F"+enemy.AttackFrame);
            GUILayout.Space(8);
            GUILayout.Label("A/D move • Space jump/double jump • A,A / D,D dash");
            GUILayout.Label("LMB sword • Shift+LMB heavy • Shift+S+LMB launcher/dive • RMB shotgun");
            GUILayout.Label("F1 debug • F2 highlight • Yellow enemy = startup, red = active, blue = recovery");
            GUILayout.Label(Replay.Status);
            if(GUILayout.Button("Show best highlight")) Replay.Begin();
            if(GUILayout.Button("Reset training actors"))
            {
                Player.Motor.Freeze(false); Combat.Receiver.ResetHealth(); Player.Motor.Teleport(new Vector2(0,1));
                Player.Motor.SetVelocity(Vector2.zero); Player.Commands.Clear(); Player.ReturnToLocomotion();
                foreach(var enemy in Enemies) enemy.ResetForTraining();
            }
            GUILayout.EndArea();
        }
    }
}
