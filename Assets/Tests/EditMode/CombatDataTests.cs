using System.Linq;
using Babodayo.Combat;
using Babodayo.Enemy;
using Babodayo.Player;
using Babodayo.Rank;
using Babodayo.Replay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Babodayo.Tests
{
    public sealed class CombatDataTests
    {
        [Test] public void EveryGeneratedAttackAndLoadoutIsValid()
        {
            var ids=AssetDatabase.FindAssets("t:AttackData",new[]{"Assets/GameData/Attacks"});
            Assert.That(ids.Length,Is.GreaterThanOrEqualTo(16));
            foreach(var guid in ids)
            {
                var asset=AssetDatabase.LoadAssetAtPath<AttackData>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(asset.Validate().Where(s=>s.StartsWith("Error")),Is.Empty,asset.name);
            }
            var loadout=AssetDatabase.LoadAssetAtPath<PlayerAttackLoadout>("Assets/GameData/Attacks/PlayerLoadout.asset");
            Assert.That(loadout.Validate(),Is.Empty);
        }
        [Test] public void SameAttackCategoryHasStateDependentBossReaction()
        {
            var boss=AssetDatabase.LoadAssetAtPath<HitReactionProfile>("Assets/GameData/Enemies/BossProfile.asset");
            var fodder=AssetDatabase.LoadAssetAtPath<HitReactionProfile>("Assets/GameData/Enemies/FodderProfile.asset");
            Assert.That(fodder.Resolve(AttackKind.Light,EnemyState.Idle),Is.EqualTo(HitReaction.Hitstun));
            Assert.That(boss.Resolve(AttackKind.Light,EnemyState.Neutral),Is.EqualTo(HitReaction.DamageOnly));
            Assert.That(boss.Resolve(AttackKind.Light,EnemyState.Stagger),Is.EqualTo(HitReaction.Hitstun));
            Assert.That(boss.Resolve(AttackKind.Light,EnemyState.AttackRecovery),Is.EqualTo(HitReaction.Hitstun));
            Assert.That(boss.Resolve(AttackKind.Launcher,EnemyState.Neutral),Is.EqualTo(HitReaction.DamageOnly));
        }
        [Test] public void AirGravityProgressionClampsAtLastValue()
        {
            var profile=AssetDatabase.LoadAssetAtPath<HitReactionProfile>("Assets/GameData/Enemies/FodderProfile.asset");
            Assert.That(profile.GravityFor(1),Is.EqualTo(1));
            Assert.That(profile.GravityFor(7),Is.EqualTo(2));
            Assert.That(profile.GravityFor(100),Is.EqualTo(2));
        }
        [Test] public void FramePhasesAndCancelEndpointsAreExact()
        {
            var frames=new FrameData{Startup=3,Active=2,Recovery=4};
            Assert.That(frames.Phase(2),Is.EqualTo(AttackPhase.Startup));
            Assert.That(frames.Phase(3),Is.EqualTo(AttackPhase.Active));
            Assert.That(frames.Phase(4),Is.EqualTo(AttackPhase.Active));
            Assert.That(frames.Phase(5),Is.EqualTo(AttackPhase.Recovery));
            Assert.That(frames.Phase(9),Is.EqualTo(AttackPhase.Complete));
            var cancel=new CancelData{Start=3,End=5,OnHitOnly=true};
            Assert.That(cancel.Window(3,false),Is.False); Assert.That(cancel.Window(3,true),Is.True);
            Assert.That(cancel.Window(5,true),Is.True); Assert.That(cancel.Window(6,true),Is.False);
        }
        [Test] public void HighlightUsesNetRankGainAndRejectsBelowMinimum()
        {
            var settings=ScriptableObject.CreateInstance<HighlightSettings>();
            try
            {
                settings.WindowFrames=3; settings.MinimumRankGain=10; settings.TieThreshold=0;
                var frames=Enumerable.Range(0,6).Select(i=>new CombatSnapshot{Frame=i,RankDelta=i>=3 ? 10 : -2,Events=new RecordedEvent[0]}).ToArray();
                var selected=HighlightSegmentSelector.Select(frames,settings);
                Assert.That(selected.Start,Is.EqualTo(3)); Assert.That(selected.End,Is.EqualTo(5)); Assert.That(selected.Gain,Is.EqualTo(30));
                settings.MinimumRankGain=31; Assert.That(HighlightSegmentSelector.Select(frames,settings).Valid,Is.False);
            }
            finally { Object.DestroyImmediate(settings); }
        }
        [Test] public void StyleRepetitionReducesOnlyScore()
        {
            var root=new GameObject("Style test"); var data=ScriptableObject.CreateInstance<StyleRankData>();
            try
            {
                var rank=root.AddComponent<StyleRankSystem>(); rank.Data=data;
                rank.BeginMove("A"); rank.Process(new StyleEvent(StyleEventKind.DamageDealt,1,10,"A")); float first=rank.Score;
                rank.BeginMove("A"); rank.Process(new StyleEvent(StyleEventKind.DamageDealt,2,10,"A")); float second=rank.Score-first;
                Assert.That(second,Is.EqualTo(first*.7f).Within(.001f));
                rank.Process(new StyleEvent(StyleEventKind.TookDamage,3)); Assert.That(rank.Score,Is.LessThan(first+second));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(data); }
        }
    }
}
