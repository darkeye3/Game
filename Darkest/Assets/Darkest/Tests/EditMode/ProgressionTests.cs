using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Core;
using Darkest.Domain.Progression;
using Darkest.Domain.Save;
using NUnit.Framework;

namespace Darkest.Tests
{
    public class ProgressionTests
    {
        private static ProgressionRules Rules() => new ProgressionRules
        {
            XpThresholds = new[] { 0, 2, 5 },
            MaxHpPerLevel = 3,
            AccuracyPerLevel = 2,
            DamagePerLevel = 1,
            DodgePerLevel = 1
        };

        [Test]
        public void Grant_LevelsUp_AppliesStatBonusOnce_AndPublishesEachLevel()
        {
            var events = new EventBus();
            var levels = TestKit.Collect<HeroLeveledUp>(events);
            var progression = TestKit.Progression(events, Rules());
            var hero = TestKit.Hero("h", TestKit.Stats(hp: 20, min: 4, max: 6));

            progression.Grant(hero, 5);

            Assert.That(hero.Level, Is.EqualTo(2));
            Assert.That(levels.Count, Is.EqualTo(2), "한 번에 두 레벨 오르면 이벤트도 두 번");
            Assert.That(hero.MaxHp, Is.EqualTo(26));
            Assert.That(hero.GetStat(StatType.DamageMin), Is.EqualTo(6));
            Assert.That(hero.GetStat(StatType.Dodge), Is.EqualTo(2));

            progression.Grant(hero, 10);
            Assert.That(hero.Level, Is.EqualTo(2), "최대 레벨에서 멈춘다");
            Assert.That(hero.MaxHp, Is.EqualTo(26), "보너스가 중복으로 붙으면 안 된다");
            Assert.That(progression.XpToNextLevel(hero), Is.EqualTo(0));
        }

        [Test]
        public void Grant_IgnoresDeadHeroes()
        {
            var progression = TestKit.Progression(rules: Rules());
            var hero = TestKit.Hero("h", TestKit.Stats(hp: 1));
            var random = new ScriptedRandom(TestKit.AlwaysFail);
            hero.ApplyDamage(1, random);
            hero.ApplyDamage(1, random);
            Assert.That(hero.IsDead, Is.True);

            progression.Grant(hero, 5);
            Assert.That(hero.Experience, Is.EqualTo(0));
        }

        [Test]
        public void Rules_RejectNonIncreasingThresholds()
        {
            Assert.Throws<System.InvalidOperationException>(() =>
                new HeroProgression(new ProgressionRules { XpThresholds = new[] { 0, 3, 3 } }, new EventBus()));
        }

        [Test]
        public void Save_RoundTrip_KeepsExperienceLevelAndHpAboveBaseMax()
        {
            var catalog = Domain.Content.DefaultContent.Create();
            var progression = TestKit.Progression(rules: Rules());
            var stress = TestKit.StressService(new ScriptedRandom());
            var factory = new Domain.Content.CombatantFactory(catalog, new Domain.Content.SequentialIdGenerator(), 200);
            var random = new ScriptedRandom();
            var service = new Domain.Hamlet.TownService(new Domain.Hamlet.Town(0, 1), new Domain.Hamlet.TownRules(), stress, factory,
                progression, random, new EventBus());
            service.SetupNewGame();
            var town = service.Town;
            var hero = town.Roster[0];
            progression.Grant(hero, 5);
            hero.Heal(hero.MaxHp);

            var mapper = new SaveMapper(catalog, factory, stress, progression, NullLogger.Instance);
            var serializer = new LineSaveSerializer();
            var restored = mapper.Restore(serializer.Deserialize(serializer.Serialize(mapper.Capture(town)))).Roster[0];

            Assert.That(restored.Id, Is.EqualTo(hero.Id));
            Assert.That(restored.Experience, Is.EqualTo(5));
            Assert.That(restored.Level, Is.EqualTo(2));
            Assert.That(restored.CurrentHp, Is.EqualTo(hero.MaxHp), "레벨 보너스 HP까지 복원돼야 한다");
        }
    }
}
