using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Core;
using Darkest.Domain.Stress;
using NUnit.Framework;

namespace Darkest.Tests
{
    public class StressTests
    {
        [Test]
        public void CrossingThreshold_TriggersAffliction_WithModifiers()
        {
            // 미덕 판정 실패(99) → 고통 목록 0번(공포) 선택(0)
            var random = new ScriptedRandom(TestKit.AlwaysFail, 0);
            var events = new EventBus();
            var triggered = TestKit.Collect<ResolveTriggered>(events);
            var stress = TestKit.StressService(random, events);
            var hero = TestKit.Hero();
            int accuracyBefore = hero.GetStat(StatType.Accuracy);

            stress.Apply(hero, 100);

            Assert.That(hero.Stress.State, Is.EqualTo(ResolveState.Afflicted));
            Assert.That(triggered.Count, Is.EqualTo(1));
            Assert.That(hero.GetStat(StatType.Accuracy), Is.LessThan(accuracyBefore));
        }

        [Test]
        public void Virtue_LowersStress()
        {
            var random = new ScriptedRandom(TestKit.AlwaysSucceed, 0);
            var rules = new StressRules();
            var stress = TestKit.StressService(random, rules: rules);
            var hero = TestKit.Hero();

            stress.Apply(hero, 120);

            Assert.That(hero.Stress.State, Is.EqualTo(ResolveState.Virtuous));
            Assert.That(hero.Stress.Current, Is.EqualTo(rules.StressAfterVirtue));
        }

        [Test]
        public void ResolveIsRolledOnlyOnce()
        {
            var random = new ScriptedRandom(TestKit.AlwaysFail, 0);
            var events = new EventBus();
            var triggered = TestKit.Collect<ResolveTriggered>(events);
            var stress = TestKit.StressService(random, events);
            var hero = TestKit.Hero();

            stress.Apply(hero, 100);
            stress.Apply(hero, 30);

            Assert.That(triggered.Count, Is.EqualTo(1));
        }

        [Test]
        public void MaxStress_HeartAttack_DeathsDoorThenDeath()
        {
            var random = new ScriptedRandom(TestKit.AlwaysFail, 0);
            var rules = new StressRules();
            var stress = TestKit.StressService(random, rules: rules);
            var hero = TestKit.Hero();

            stress.Apply(hero, 999);
            Assert.That(hero.IsAtDeathsDoor, Is.True);
            Assert.That(hero.Stress.Current, Is.EqualTo(rules.StressAfterHeartAttack));

            stress.Apply(hero, 999);
            Assert.That(hero.IsDead, Is.True);
        }

        [Test]
        public void ReachingZero_ClearsResolveAndModifiers()
        {
            var random = new ScriptedRandom(TestKit.AlwaysFail, 0);
            var stress = TestKit.StressService(random);
            var hero = TestKit.Hero();
            int accuracyBefore = hero.GetStat(StatType.Accuracy);

            stress.Apply(hero, 100);
            stress.Apply(hero, -200);

            Assert.That(hero.Stress.State, Is.EqualTo(ResolveState.None));
            Assert.That(hero.GetStat(StatType.Accuracy), Is.EqualTo(accuracyBefore));
        }

        [Test]
        public void AfflictedHero_ActsOut_StressingAlliesButNotSelf()
        {
            var random = new ScriptedRandom(TestKit.AlwaysFail, 0);
            var stress = TestKit.StressService(random);
            var afflicted = TestKit.Hero("a");
            var ally = TestKit.Hero("b");
            stress.Apply(afflicted, 100);
            int selfBefore = afflicted.Stress.Current;

            random.Push(TestKit.AlwaysSucceed); // 폭주 발동
            stress.OnTurnStart(afflicted, new Combatant[] { afflicted, ally });

            Assert.That(ally.Stress.Current, Is.GreaterThan(0));
            Assert.That(afflicted.Stress.Current, Is.EqualTo(selfBefore));
        }

        [Test]
        public void DeadHero_IgnoresStress()
        {
            var stress = TestKit.StressService(new ScriptedRandom());
            var hero = TestKit.Hero(stats: TestKit.Stats(hp: 1));
            hero.ApplyDamage(1, new ScriptedRandom());
            hero.ApplyDamage(1, new ScriptedRandom(TestKit.AlwaysFail));
            Assert.That(hero.IsDead, Is.True);

            stress.Apply(hero, 50);
            Assert.That(hero.Stress.Current, Is.EqualTo(0));
        }
    }
}
