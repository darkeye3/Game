using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Resolution;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Combat.Status;
using NUnit.Framework;

namespace Darkest.Tests
{
    public class CombatCoreTests
    {
        [Test]
        public void StatModifiers_SumAndRemoveBySource()
        {
            var hero = TestKit.Hero(stats: TestKit.Stats(dodge: 10));
            hero.Modifiers.Add(new StatModifier(StatType.Dodge, 5, "buff"));
            hero.Modifiers.Add(new StatModifier(StatType.Dodge, -3, "debuff"));
            Assert.That(hero.GetStat(StatType.Dodge), Is.EqualTo(12));

            hero.Modifiers.RemoveBySource("buff");
            Assert.That(hero.GetStat(StatType.Dodge), Is.EqualTo(7));
        }

        [Test]
        public void RankMask_ParseAndContains()
        {
            var mask = RankMaskExtensions.Parse("1, 3");
            Assert.That(mask.Contains(1), Is.True);
            Assert.That(mask.Contains(2), Is.False);
            Assert.That(mask.Contains(3), Is.True);
            Assert.That(mask.Contains(5), Is.False);
            Assert.Throws<System.FormatException>(() => RankMaskExtensions.Parse("0,5"));
        }

        [Test]
        public void Formation_MoveShiftsOthers_AndRemoveDeadCompacts()
        {
            var a = TestKit.Hero("a"); var b = TestKit.Hero("b"); var c = TestKit.Hero("c");
            var party = TestKit.Party(a, b, c);

            Assert.That(party.Move(a, 2), Is.EqualTo(2));
            Assert.That(party.RankOf(b), Is.EqualTo(1));
            Assert.That(party.RankOf(c), Is.EqualTo(2));
            Assert.That(party.RankOf(a), Is.EqualTo(3));

            Assert.That(party.Move(a, 5), Is.EqualTo(0), "끝에서 더 뒤로는 못 간다");

            var monster = TestKit.Monster();
            var monsters = TestKit.Party(monster, TestKit.Monster("m2"));
            monster.ApplyDamage(999, new ScriptedRandom());
            var removed = new List<Combatant>();
            monsters.RemoveDead(removed);
            Assert.That(removed, Is.EquivalentTo(new[] { monster }));
            Assert.That(monsters.AtRank(1).Id, Is.EqualTo("m2"));
        }

        [Test]
        public void Formation_RejectsWrongTeamAndOverflow()
        {
            var party = new Formation(Team.Heroes);
            Assert.Throws<System.InvalidOperationException>(() => party.Add(TestKit.Monster()));
            for (int i = 0; i < Formation.MaxRanks; i++) party.Add(TestKit.Hero("h" + i));
            Assert.Throws<System.InvalidOperationException>(() => party.Add(TestKit.Hero("extra")));
        }

        [Test]
        public void HitResolver_ClampsChance_AndSupportSkillsAlwaysHit()
        {
            var rules = new CombatRules();
            var resolver = new DefaultHitResolver(rules);
            var attacker = TestKit.Hero();
            var evasive = TestKit.Monster(stats: TestKit.Stats(dodge: 500));
            var heal = new SkillDefinition("heal", "heal", RankMask.All, TargetSide.Ally, RankMask.All, false, 0, 0, 0, 0, null);

            Assert.That(resolver.GetHitChance(attacker, evasive, TestKit.Attack()), Is.EqualTo(rules.MinHitChance));
            Assert.That(resolver.GetHitChance(attacker, TestKit.Monster(), TestKit.Attack(accuracy: 500)), Is.EqualTo(rules.MaxHitChance));
            Assert.That(resolver.GetHitChance(attacker, attacker, heal), Is.EqualTo(100));
        }

        [Test]
        public void DamageCalculator_CritUsesMaxTimesMultiplier_ThenProtection()
        {
            var calc = new DefaultDamageCalculator(new CombatRules { CritMultiplierPercent = 150 });
            var attacker = TestKit.Hero(stats: TestKit.Stats(min: 4, max: 10));
            var armored = TestKit.Monster(stats: TestKit.Stats(protection: 20));

            // 10 * 1.5 = 15, 방어 20% → 12
            Assert.That(calc.Calculate(attacker, armored, TestKit.Attack(), true, new ScriptedRandom()), Is.EqualTo(12));
            // 일반: 굴림 4(스크립트 최소값) * 100% → 방어 20% → 3
            Assert.That(calc.Calculate(attacker, armored, TestKit.Attack(), false, new ScriptedRandom()), Is.EqualTo(3));
            // 최소 피해 1 보장
            var weak = TestKit.Hero(stats: TestKit.Stats(min: 1, max: 1));
            Assert.That(calc.Calculate(weak, armored, TestKit.Attack(damagePercent: 10), false, new ScriptedRandom()), Is.EqualTo(1));
        }

        [Test]
        public void Hero_DeathsDoor_ThenDeathblowCheck()
        {
            var hero = TestKit.Hero(stats: TestKit.Stats(hp: 10, deathblow: 50));

            Assert.That(hero.ApplyDamage(15, new ScriptedRandom()), Is.EqualTo(DamageOutcome.EnteredDeathsDoor));
            Assert.That(hero.IsAtDeathsDoor && hero.IsAlive && hero.CurrentHp == 0, Is.True);

            // 저항 판정 성공(0 < 50) → 생존
            Assert.That(hero.ApplyDamage(3, new ScriptedRandom(TestKit.AlwaysSucceed)), Is.EqualTo(DamageOutcome.SurvivedDeathblow));
            // 저항 판정 실패(99 >= 50) → 사망
            Assert.That(hero.ApplyDamage(3, new ScriptedRandom(TestKit.AlwaysFail)), Is.EqualTo(DamageOutcome.Killed));
            Assert.That(hero.IsDead, Is.True);
            Assert.That(hero.Heal(10), Is.EqualTo(0), "사망자는 회복 불가");
        }

        [Test]
        public void Hero_HealLeavesDeathsDoor()
        {
            var hero = TestKit.Hero(stats: TestKit.Stats(hp: 10));
            hero.ApplyDamage(10, new ScriptedRandom());
            Assert.That(hero.IsAtDeathsDoor, Is.True);
            Assert.That(hero.Heal(4), Is.EqualTo(4));
            Assert.That(hero.IsAtDeathsDoor, Is.False);
        }

        [Test]
        public void Monster_DiesImmediatelyAtZero()
        {
            var monster = TestKit.Monster(stats: TestKit.Stats(hp: 5));
            Assert.That(monster.ApplyDamage(5, new ScriptedRandom()), Is.EqualTo(DamageOutcome.Killed));
            Assert.That(monster.IsDead, Is.True);
        }

        [Test]
        public void Statuses_DotTicksAndExpires_StunConsumedOnceAndDoesNotStack()
        {
            var set = new StatusEffectSet();
            set.Add(new StatusEffect(StatusKind.Bleed, 2, 2));
            set.Add(new StatusEffect(StatusKind.Blight, 3, 1));
            set.Add(new StatusEffect(StatusKind.Stun, 0, 1));
            set.Add(new StatusEffect(StatusKind.Stun, 0, 1));

            Assert.That(set.TickDamageOverTime(), Is.EqualTo(5));
            Assert.That(set.TickDamageOverTime(), Is.EqualTo(2));
            Assert.That(set.TickDamageOverTime(), Is.EqualTo(0));

            Assert.That(set.TryConsumeStun(), Is.True);
            Assert.That(set.TryConsumeStun(), Is.False, "기절은 중첩되지 않는다");
        }
    }
}
