using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using Darkest.Domain.Hamlet;
using NUnit.Framework;

namespace Darkest.Tests
{
    public class TownBuildingTests
    {
        [Test]
        public void Upgrade_SpendsGold_UntilMaxLevel()
        {
            var config = new GameConfig();
            config.Town.StartingGold = 100000;
            var session = SessionFixtures.New(1, config);
            var buildings = session.TownService.Buildings;
            int gold = session.Town.Gold;
            int cost = buildings.UpgradeCost(BuildingType.Tavern);

            Assert.That(buildings.TryUpgrade(BuildingType.Tavern), Is.Null);
            Assert.That(buildings.Level(BuildingType.Tavern), Is.EqualTo(1));
            Assert.That(session.Town.Gold, Is.EqualTo(gold - cost));

            while (!buildings.IsMaxed(BuildingType.Tavern)) Assert.That(buildings.TryUpgrade(BuildingType.Tavern), Is.Null);
            Assert.That(buildings.TryUpgrade(BuildingType.Tavern), Is.Not.Null);
            Assert.That(buildings.UpgradeCost(BuildingType.Tavern), Is.EqualTo(-1));
        }

        [Test]
        public void Upgrade_FailsWithoutGold()
        {
            var config = new GameConfig();
            config.Town.StartingGold = 0;
            var session = SessionFixtures.New(2, config);
            Assert.That(session.TownService.Buildings.TryUpgrade(BuildingType.Guild), Is.Not.Null);
            Assert.That(session.TownService.Buildings.Level(BuildingType.Guild), Is.EqualTo(0));
        }

        [Test]
        public void Effects_ChangeTavernProvisionsAndRecruits()
        {
            var config = new GameConfig();
            config.Town.StartingGold = 100000;
            var session = SessionFixtures.New(3, config);
            var town = session.TownService;
            var rules = config.Town;

            town.Buildings.TryUpgrade(BuildingType.Tavern);
            Assert.That(town.TavernCost, Is.EqualTo(rules.TavernCost - rules.Buildings.TavernDiscountPerLevel));
            Assert.That(town.Buildings.TavernStressRelief, Is.EqualTo(rules.TavernStressRelief + rules.Buildings.TavernReliefPerLevel));

            town.Buildings.TryUpgrade(BuildingType.Provisioner);
            Assert.That(town.FoodPrice, Is.EqualTo(rules.FoodPrice * (100 - rules.Buildings.ProvisionerDiscountPercentPerLevel) / 100));

            town.Buildings.TryUpgrade(BuildingType.Stagecoach);
            town.RefreshRecruits();
            Assert.That(session.Town.Recruits.Count, Is.EqualTo(rules.RecruitPoolSize + rules.Buildings.StagecoachRecruitsPerLevel));
            Assert.That(town.Buildings.RosterCapacity, Is.EqualTo(rules.MaxRosterSize + rules.Buildings.StagecoachRosterPerLevel));
        }

        [Test]
        public void Guild_AddsExperienceOnReturn()
        {
            var config = new GameConfig();
            config.Town.StartingGold = 100000;
            var session = SessionFixtures.New(4, config);
            session.TownService.Buildings.TryUpgrade(BuildingType.Guild);
            var hero = session.Town.Roster[0];

            Assert.That(session.TryEmbark(new[] { hero }, new Provisions(4, 0, 0)), Is.Null);
            session.Retreat();
            session.ReturnToTown();

            int expected = config.Progression.XpOnExpeditionRetreated + config.Town.Buildings.GuildXpPerLevel;
            Assert.That(hero.Experience, Is.EqualTo(expected));
        }

        [Test]
        public void BuildingLevels_SurviveSave()
        {
            var config = new GameConfig();
            config.Town.StartingGold = 100000;
            var session = SessionFixtures.New(5, config);
            session.TownService.Buildings.TryUpgrade(BuildingType.Stagecoach);
            session.TownService.Buildings.TryUpgrade(BuildingType.Stagecoach);

            var loaded = SessionFixtures.RoundTrip(session.CaptureSave(), 9, config);
            Assert.That(loaded.TownService.Buildings.Level(BuildingType.Stagecoach), Is.EqualTo(2));
            Assert.That(loaded.TownService.Buildings.Level(BuildingType.Tavern), Is.EqualTo(0));
        }
    }
}
