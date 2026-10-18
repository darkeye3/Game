using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Content;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Items;
using NUnit.Framework;

namespace Darkest.Tests
{
    public class TrinketTests
    {
        private const string SunRing = "trk_sun_ring";       // 명중 +5
        private const string BloodCharm = "trk_blood_charm"; // 최대 HP -3

        [Test]
        public void Equip_AddsBonus_AndUnequipReturnsToStash()
        {
            var session = SessionFixtures.WithStash(1, SunRing);
            var hero = session.Town.Roster[0];
            var ring = session.Town.Trinkets[0];
            int accuracy = hero.GetStat(StatType.Accuracy);

            Assert.That(session.TownService.Trinkets.TryEquip(hero, ring), Is.Null);
            Assert.That(hero.GetStat(StatType.Accuracy), Is.EqualTo(accuracy + 5));
            Assert.That(hero.GetTrinket(0), Is.SameAs(ring));
            Assert.That(session.Town.Trinkets, Is.Empty);

            Assert.That(session.TownService.Trinkets.TryUnequip(hero, 0), Is.Null);
            Assert.That(hero.GetStat(StatType.Accuracy), Is.EqualTo(accuracy));
            Assert.That(session.Town.Trinkets, Has.Member(ring));
        }

        [Test]
        public void Equip_FailsWhenSlotsFull()
        {
            var session = SessionFixtures.WithStash(2, SunRing, SunRing, SunRing);
            var hero = session.Town.Roster[0];
            var trinkets = session.TownService.Trinkets;
            Assert.That(trinkets.TryEquip(hero, session.Town.Trinkets[0]), Is.Null);
            Assert.That(trinkets.TryEquip(hero, session.Town.Trinkets[0]), Is.Null);
            Assert.That(trinkets.TryEquip(hero, session.Town.Trinkets[0]), Is.Not.Null);
            Assert.That(session.Town.Trinkets.Count, Is.EqualTo(1));
        }

        [Test]
        public void HpPenalty_ClampsCurrentHp()
        {
            var session = SessionFixtures.WithStash(3, BloodCharm);
            var hero = session.Town.Roster[0];
            int maxHp = hero.MaxHp;
            Assert.That(session.TownService.Trinkets.TryEquip(hero, session.Town.Trinkets[0]), Is.Null);
            Assert.That(hero.MaxHp, Is.EqualTo(maxHp - 3));
            Assert.That(hero.CurrentHp, Is.EqualTo(hero.MaxHp));
        }

        [Test]
        public void Sell_GivesGold_AndDismissKeepsTrinkets()
        {
            var session = SessionFixtures.WithStash(4, SunRing, SunRing);
            int gold = session.Town.Gold;
            var ring = session.Town.Trinkets[0];
            Assert.That(session.TownService.Trinkets.TrySell(ring), Is.Null);
            Assert.That(session.Town.Gold, Is.EqualTo(gold + ring.SellValue));

            var hero = session.Town.Roster[0];
            Assert.That(session.TownService.Trinkets.TryEquip(hero, session.Town.Trinkets[0]), Is.Null);
            Assert.That(session.TownService.TryDismiss(hero), Is.Null);
            Assert.That(session.Town.Trinkets.Count, Is.EqualTo(1), "해고한 영웅의 장신구는 보관함으로 돌아온다");
        }

        [Test]
        public void FoundTrinkets_GoToStashOnReturn()
        {
            var session = SessionFixtures.New(5);
            Assert.That(session.TryEmbark(session.Town.Roster, new Provisions(8, 2, 0)), Is.Null);
            var ring = session.Catalog.FindTrinket(SunRing);
            session.Expedition.Inventory.AddTrinket(ring);
            session.Retreat();
            session.ReturnToTown();
            Assert.That(session.Town.Trinkets, Has.Member(ring));
        }

        [Test]
        public void DropTable_UsesWeights()
        {
            var common = new TrinketDefinition("a", "a", TrinketRarity.Common, new[] { new StatBonus(StatType.Speed, 1) }, 0, 3);
            var rare = new TrinketDefinition("b", "b", TrinketRarity.Rare, new[] { new StatBonus(StatType.Speed, 1) }, 0, 1);
            var table = new TrinketDropTable(new[] { common, rare });
            Assert.That(table.Roll(new ScriptedRandom(2)), Is.SameAs(common));
            Assert.That(table.Roll(new ScriptedRandom(3)), Is.SameAs(rare));
            Assert.That(new TrinketDropTable(new TrinketDefinition[0]).Roll(new ScriptedRandom()), Is.Null);
        }

        [Test]
        public void DefaultContent_HasTrinketsAndCampSkillsForEveryClass()
        {
            var catalog = DefaultContent.Create();
            Assert.That(catalog.Trinkets.Count, Is.GreaterThan(0));
            foreach (var heroClass in catalog.HeroClasses)
                Assert.That(catalog.CampSkillsFor(heroClass.Id).Count, Is.GreaterThan(0), heroClass.Id);
        }
    }
}
