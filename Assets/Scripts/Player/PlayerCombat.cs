using Babodayo.Combat;
using Babodayo.Input;
using Babodayo.Player.StateMachine;
using UnityEngine;
namespace Babodayo.Player
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Tooltip("Owning player.")] public PlayerController Player;
        [Tooltip("Attack asset mapping by state and control.")] public PlayerAttackLoadout Loadout;
        [Tooltip("Owned combat hitbox.")] public Hitbox Hitbox;
        [Tooltip("Player health, team and event endpoint.")] public DamageReceiver Receiver;
        [Tooltip("Optional clip/trail presentation.")] public AttackPresentation Presentation;
        public AttackState Current => Player?.States?.Current as AttackState;
        private void Start()
        {
            Player.CombatAction=()=>TryAttack();
            Player.IsFrozen=()=>Receiver.Hitstop!=null && Receiver.Hitstop.IsStopped;
            Receiver.Hitstop?.Bind(Player.Motor.Freeze);
            Player.Locomotion.Jumped+=OnJump;
            Receiver.Received+=OnHit;
        }
        private void OnDestroy()
        {
            if(Player?.Locomotion!=null) Player.Locomotion.Jumped-=OnJump;
            if(Receiver!=null) Receiver.Received-=OnHit;
        }
        private void OnJump(int duration) => Receiver.Invulnerability?.Grant(InvulnerabilityCause.Jump,Player.CurrentFrame,duration);
        private void OnHit(HitResult result)
        {
            Hitbox.Disable();
            Player.States.Change(Receiver.Dead ? (PlayerState)new DeadState(Player) : new HitState(Player,result.HitstunFrames));
            Player.Motor.SetVelocity(result.Knockback);
        }
        public bool TryAttack(int nextCombo=0,bool allowSword=true,bool allowShot=true)
        {
            if(Loadout==null) return false;
            bool air=!Player.Motor.Grounded;
            if(allowSword)
            {
                if(Try(CombatCommand.Shift_S_LMB_Hold,air ? Loadout.Dive : Loadout.RisingLauncher,-1,air)) return true;
                if(Try(CombatCommand.Shift_Direction_LMB,air ? Loadout.AirPierce : Loadout.Rush,-1,air)) return true;
                if(Try(CombatCommand.W_LMB,Loadout.UpThrust,-1,air)) return true;
                if(Try(CombatCommand.DoubleTapA_LMB,Loadout.DashAttack,-1,air) || Try(CombatCommand.DoubleTapD_LMB,Loadout.DashAttack,-1,air)) return true;
                if(air && Try(CombatCommand.Direction_LMB,Loadout.DiagonalDive,-1,true)) return true;
                if(Try(CombatCommand.Shift_S_LMB,air ? Loadout.Dive : Loadout.Launcher,-1,air)) return true;
                if(Try(CombatCommand.Shift_LMB,Loadout.Heavy,-1,air)) return true;
                var combo=air ? Loadout.AirCombo : Loadout.GroundCombo;
                if(combo!=null && nextCombo>=0 && nextCombo<combo.Length &&
                    (Try(CombatCommand.LMB,combo[nextCombo],nextCombo,air) || (!air && Try(CombatCommand.Direction_LMB,combo[nextCombo],nextCombo,false)))) return true;
            }
            return allowShot && Try(CombatCommand.RMB,air ? Loadout.AirShot : Loadout.GroundShot,-1,air);
        }
        private bool Try(CombatCommand command,AttackData attack,int combo,bool air)
        {
            if(attack==null || !Player.Consume(command)) return false;
            Player.UpdateFacing(false);
            Player.States.Change(new AttackState(Player,this,attack,combo,air));
            return true;
        }
    }
}
