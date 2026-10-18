using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Combat.Status;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using Darkest.Domain.Save;
using NUnit.Framework;

namespace Darkest.Tests
{
    /// <summary>세이브 3버전: 난수 상태, 장신구, 상태이상, 야영 버프/상태 왕복.</summary>
    public class SaveV3Tests
    {
        [Test]
        public void SeededRandom_RestoredState_RepeatsSequence()
        {
            var a = new SeededRandom(7);
            a.Range(0, 100);
            ulong state = a.State;
            int expected1 = a.Range(0, 1000), expected2 = a.Range(0, 1000);

            var b = new SeededRandom(12345);
            b.RestoreState(state);
            Assert.That(b.Range(0, 1000), Is.EqualTo(expected1));
            Assert.That(b.Range(0, 1000), Is.EqualTo(expected2));
        }

        [Test]
        public void Load_WithDifferentSeeds_GivesSameRecruits()
        {
            var save = SessionFixtures.New(3).CaptureSave();
            Assert.That(save.RandomState, Is.Not.Null);
            var a = SessionFixtures.RoundTrip(save, 100);
            var b = SessionFixtures.RoundTrip(save, 200);
            for (int i = 0; i < a.Town.Recruits.Count; i++)
            {
                Assert.That(b.Town.Recruits[i].DisplayName, Is.EqualTo(a.Town.Recruits[i].DisplayName));
                Assert.That(b.Town.Recruits[i].ClassId, Is.EqualTo(a.Town.Recruits[i].ClassId));
            }
        }

        [Test]
        public void MidExpedition_KeepsStatusesTrinketsBuffsAndCamp()
        {
            var session = SessionFixtures.WithStash(4, "trk_sun_ring");
            var hero = session.Town.Roster[0];
            Assert.That(session.TownService.Trinkets.TryEquip(hero, session.Town.Trinkets[0]), Is.Null);
            Assert.That(session.TryEmbark(session.Town.Roster, new Provisions(10, 0, 1)), Is.Null);
            Assert.That(session.TryCamp(), Is.Null);

            var user = (Hero)session.Expedition.Party.Members[1];
            var skill = session.Expedition.ActiveCamp.SkillsOf(user)[0];
            Assert.That(session.TryUseCampSkill(user, skill, user), Is.Null);
            hero.Statuses.Add(new StatusEffect(StatusKind.Bleed, 3, 2));
            hero.Modifiers.Add(new StatModifier(StatType.Speed, 2, CampSkillDefinition.BuffSourceId));
            session.Expedition.Inventory.AddTrinket(session.Catalog.FindTrinket("trk_dead_heart"));
            int accuracy = hero.GetStat(StatType.Accuracy);
            int speed = hero.GetStat(StatType.Speed);

            var loaded = SessionFixtures.RoundTrip(session.CaptureSave(), 77);
            var restored = loaded.Town.FindHero(hero.Id);

            Assert.That(restored.GetTrinket(0).Id, Is.EqualTo("trk_sun_ring"));
            Assert.That(restored.GetStat(StatType.Accuracy), Is.EqualTo(accuracy));
            Assert.That(restored.GetStat(StatType.Speed), Is.EqualTo(speed));
            Assert.That(restored.Statuses.All.Count, Is.EqualTo(1));
            Assert.That(restored.Statuses.All[0].Kind, Is.EqualTo(StatusKind.Bleed));
            Assert.That(restored.Statuses.All[0].RemainingTurns, Is.EqualTo(2));
            Assert.That(loaded.Expedition.Inventory.Trinkets[0].Id, Is.EqualTo("trk_dead_heart"));

            var camp = loaded.Expedition.ActiveCamp;
            Assert.That(camp, Is.Not.Null);
            Assert.That(camp.RespiteRemaining, Is.EqualTo(session.Expedition.ActiveCamp.RespiteRemaining));
            Assert.That(loaded.TryUseCampSkill(loaded.Town.FindHero(user.Id), skill, loaded.Town.FindHero(user.Id)), Is.Not.Null,
                "이미 쓴 야영 스킬 기록도 복원된다");
        }

        [Test]
        public void Version2Save_StillLoads()
        {
            var data = new LineSaveSerializer().Deserialize("darkest-save|2\ntown|500|3|10\nhero|h1|warden|아리아|20|5||0|0|3\n");
            Assert.That(data.RandomState, Is.Null);
            Assert.That(data.Roster[0].Experience, Is.EqualTo(3));
            Assert.That(data.Roster[0].Trinkets, Is.Empty);
        }

        [Test]
        public void HeroDetailBeforeHero_IsRejected()
        {
            Assert.Throws<System.FormatException>(() =>
                new LineSaveSerializer().Deserialize("darkest-save|3\ntown|1|1|1\nstatus|h9|Bleed|1|1\n"));
        }

        [Test]
        public void UnknownTrinket_IsSkipped()
        {
            var data = SessionFixtures.New(5).CaptureSave();
            data.TrinketStash.Add("deleted_trinket");
            var loaded = SessionFixtures.RoundTrip(data, 5);
            Assert.That(loaded.Town.Trinkets, Is.Empty);
        }
    }
}
