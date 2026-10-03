using System.Collections.Generic;
using Babodayo.Combat;
using UnityEngine;
namespace Babodayo.Player
{
    [CreateAssetMenu(menuName="Babodayo/Player Attack Loadout")]
    public sealed class PlayerAttackLoadout : ScriptableObject
    {
        [Tooltip("Ground LMB chain, ordered 1-2-3.")] public AttackData[] GroundCombo;
        [Tooltip("Air LMB chain, ordered 1-2-3.")] public AttackData[] AirCombo;
        [Tooltip("Shift+LMB.")] public AttackData Heavy;
        [Tooltip("Ground Shift+S+LMB.")] public AttackData Launcher;
        [Tooltip("Air Shift+S+LMB.")] public AttackData Dive;
        [Tooltip("Ground RMB.")] public AttackData GroundShot;
        [Tooltip("Air RMB.")] public AttackData AirShot;
        [Tooltip("Hold launcher: player rises with the target.")] public AttackData RisingLauncher;
        [Tooltip("Ground Shift+horizontal+LMB.")] public AttackData Rush;
        [Tooltip("W+LMB upward thrust, ground or air.")] public AttackData UpThrust;
        [Tooltip("Double-tap then LMB, ground or air.")] public AttackData DashAttack;
        [Tooltip("Air horizontal+LMB.")] public AttackData DiagonalDive;
        [Tooltip("Air Shift+horizontal+LMB.")] public AttackData AirPierce;
        public IEnumerable<string> Validate()
        {
            if(GroundCombo==null || GroundCombo.Length!=3) yield return "Ground combo requires 3 attacks.";
            else foreach(var a in GroundCombo) if(a==null) yield return "Ground combo has a missing attack.";
            if(AirCombo==null || AirCombo.Length!=3) yield return "Air combo requires 3 attacks.";
            else foreach(var a in AirCombo) if(a==null) yield return "Air combo has a missing attack.";
            if(RisingLauncher==null || Rush==null || UpThrust==null || DashAttack==null || DiagonalDive==null || AirPierce==null) yield return "An extended special attack is missing.";
            if(Heavy==null || Launcher==null || Dive==null || GroundShot==null || AirShot==null) yield return "A required special attack is missing.";
        }
    }
}
