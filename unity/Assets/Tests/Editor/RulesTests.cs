using System;
using NUnit.Framework;

namespace Memeopoly.Tests
{
    public class RulesTests
    {
        private static GameSave Fresh() { return MemeopolyRules.NewGame(1000000); }

        [Test]
        public void ExactlySixtySpacesAndThirtyBuilds()
        {
            Assert.AreEqual(60, MemeopolyRules.TileKinds.Length);
            Assert.AreEqual(6, MemeopolyRules.Landmarks.Length);
            Assert.AreEqual(500, MemeopolyRules.UpgradeCost(Fresh(), 0));
        }

        [Test]
        public void TwoActualDiceAdvanceTokenAndWrapCorrectly()
        {
            var s=Fresh();
            s.position=58;
            var result=MemeopolyRules.Roll(s,1,6,4,new Random(123));
            Assert.AreEqual(10,result.steps);
            Assert.AreEqual(8,result.destination);
            Assert.AreEqual(1,result.laps);
            Assert.AreEqual(8,s.position);
            Assert.AreEqual(6,s.lastDieA);
            Assert.AreEqual(4,s.lastDieB);
            Assert.AreEqual(29,s.dice);
        }

        [Test]
        public void UpgradeBuildsFiveDistinctStagesAndNeverSix()
        {
            var s=Fresh();
            s.coins=100000;
            for(int i=0;i<5;i++)
            {
                Assert.IsTrue(MemeopolyRules.CanUpgrade(s,0));
                MemeopolyRules.Upgrade(s,0);
                Assert.AreEqual(i+1,s.buildings[0]);
            }
            Assert.IsFalse(MemeopolyRules.CanUpgrade(s,0));
            Assert.AreEqual(-1,MemeopolyRules.UpgradeCost(s,0));
        }

        [Test]
        public void CompletionPaysOnceAndResetsBuildingsOnlyOnNewWorld()
        {
            var s=Fresh();
            s.coins=1000000;
            for(int i=0;i<6;i++)
                for(int j=0;j<5;j++) MemeopolyRules.Upgrade(s,i);
            Assert.IsTrue(MemeopolyRules.Completed(s));
            int rewardCoins=s.coins;
            Assert.IsFalse(MemeopolyRules.CanUpgrade(s,0));
            Assert.AreEqual(rewardCoins,s.coins);
            Assert.IsTrue(MemeopolyRules.NextWorld(s));
            Assert.AreEqual(1,s.worldIndex);
            Assert.AreEqual(0,MemeopolyRules.TotalUpgrades(s));
            Assert.AreEqual(rewardCoins,s.coins);
        }

        [Test]
        public void EnergyRestoresFiveMinuteIntervals()
        {
            var s=Fresh();
            s.dice=2;
            MemeopolyRules.RegenerateDice(s,1000000+600000);
            Assert.AreEqual(4,s.dice);
        }

        [Test]
        public void ImportedBrowserSaveKeepsTheSixBuildings()
        {
            string json = @"{""worldIndex"":2,""position"":9,""coins"":9300,""dice"":16,""landmarks"":[{""name"":""Sharko Surf Shack"",""level"":3}],""cards"":[""Doge""]}";
            GameSave s;
            Assert.IsTrue(MemeopolyRules.TryImportLegacy(json,1000000,out s));
            Assert.AreEqual(3,s.buildings[0]);
            Assert.AreEqual(9,s.position);
            Assert.AreEqual("Doge",s.cards[0]);
        }
    }
}