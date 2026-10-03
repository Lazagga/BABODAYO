using Babodayo.Combat;
using Babodayo.Enemy;
using Babodayo.Player;
using Babodayo.Rank;
using Babodayo.Replay;
using UnityEngine;
namespace Babodayo.Core
{
    public sealed class CombatTestReferences : MonoBehaviour
    {
        [Tooltip("Scene clock for training and tests.")] public CombatClock Clock;
        [Tooltip("Player instance.")] public PlayerController Player;
        [Tooltip("Player combat instance.")] public PlayerCombat Combat;
        [Tooltip("Stable actor list: Dummy, Fodder, Boss.")] public EnemyController[] Enemies;
        [Tooltip("Rank instance.")] public StyleRankSystem Rank;
        [Tooltip("Snapshot recorder.")] public CombatRecorder Recorder;
        [Tooltip("Replay controller.")] public HighlightReplayPlayer Replay;
    }
}
