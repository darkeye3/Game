using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.AI;
using Darkest.Domain.Combat.Commands;
using Darkest.Domain.Combat.Events;
using Darkest.Domain.Combat.Flow;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Status;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using NUnit.Framework;

namespace Darkest.Tests
{
    public class BattleTests
    {
        [Test]
        public void TurnOrder_FasterActsFirst()
        {
            var fast = TestKit.Hero("fast", TestKit.Stats(speed: 20));
            var slow = TestKit.Monster("slow", TestKit.Stats(speed: 0));
            var battle = new BattleSession(TestKit.Party(fast), TestKit.Party(slow), TestKit.Services(new ScriptedRandom()));

            battle.Start();

            Assert.That(battle.IsAwaitingHeroCommand, Is.True);
            Assert.That(battle.CurrentActor, Is.SameAs(fast));
        }

        [Test]
        public void Submit_RejectsInvalidTarget_AndKeepsWaiting()
        {
            var meleeOnly = TestKit.Attack("melee", targets: "1");
            var hero = TestKit.Hero("h", TestKit.Stats(speed: 20), meleeOnly);
            var front = TestKit.Monster("front");
            var back = TestKit.Monster("back");
            var battle = new BattleSession(TestKit.Party(hero), TestKit.Party(front, back), TestKit.Services(new ScriptedRandom()));
            battle.Start();

            string error = battle.Submit(new UseSkillCommand(hero, meleeOnly, back));

            Assert.That(error, Is.Not.Null);
            Assert.That(battle.IsAwaitingHeroCommand, Is.True);
            Assert.That(battle.Context.History.Count, Is.EqualTo(0));
        }

        [Test]
        public void KillingLastMonster_EndsInVictory()
        {
            var hero = TestKit.Hero("h", TestKit.Stats(speed: 20, min: 50, max: 50));
            var monster = TestKit.Monster("m", TestKit.Stats(hp: 10));
            var events = new EventBus();
            var ended = TestKit.Collect<BattleEnded>(events);
            var died = TestKit.Collect<CombatantDied>(events);
            var battle = new BattleSession(TestKit.Party(hero), TestKit.Party(monster), TestKit.Services(new ScriptedRandom(), events));
            battle.Start();

            Assert.That(battle.Submit(new UseSkillCommand(hero, hero.Skills[0], monster)), Is.Null);

            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.Victory));
            Assert.That(ended.Count, Is.EqualTo(1));
            Assert.That(ended[0].Result, Is.EqualTo(BattleResult.Victory));
            Assert.That(died.Count, Is.EqualTo(1));
        }

        [Test]
        public void StunnedMonster_SkipsTurn()
        {
            var stunSkill = TestKit.Attack("stun", damagePercent: 0, effects: new StunEffect(100));
            var hero = TestKit.Hero("h", TestKit.Stats(speed: 20), stunSkill);
            var monster = TestKit.Monster("m", TestKit.Stats(speed: 10));
            var events = new EventBus();
            var skipped = TestKit.Collect<TurnSkipped>(events);
            var battle = new BattleSession(TestKit.Party(hero), TestKit.Party(monster), TestKit.Services(new ScriptedRandom(), events));
            battle.Start();

            battle.Submit(new UseSkillCommand(hero, stunSkill, monster));

            Assert.That(skipped.Count, Is.EqualTo(1));
            Assert.That(skipped[0].Reason, Is.EqualTo(TurnSkipReason.Stunned));
            Assert.That(battle.CurrentActor, Is.SameAs(hero), "다음 라운드 영웅 차례로 돌아온다");
        }

        [Test]
        public void Bleed_DamagesAtTurnStart()
        {
            var hero = TestKit.Hero("h", TestKit.Stats(speed: 20));
            var monster = TestKit.Monster("m", TestKit.Stats(hp: 20, speed: 10));
            monster.Statuses.Add(new StatusEffect(StatusKind.Bleed, 4, 2));
            var battle = new BattleSession(TestKit.Party(hero), TestKit.Party(monster), TestKit.Services(new ScriptedRandom()));
            battle.Start();

            battle.Submit(new PassCommand(hero));

            // 몬스터 턴 시작 시 출혈 4 → 16, 몬스터는 공격 후 다시 영웅 차례.
            Assert.That(monster.CurrentHp, Is.EqualTo(16));
        }

        [Test]
        public void AllyDeath_StressesSurvivors()
        {
            var victim = TestKit.Hero("victim", TestKit.Stats(hp: 1, speed: 0));
            var survivor = TestKit.Hero("survivor", TestKit.Stats(speed: 0));
            var killer = TestKit.Monster("m", TestKit.Stats(speed: 20, min: 50, max: 50), TestKit.Attack("kill", targets: "1"));
            victim.ApplyDamage(1, new ScriptedRandom()); // 이미 죽음의 문턱
            var rules = new Domain.Combat.Resolution.CombatRules();
            var random = new ScriptedRandom();
            var battle = new BattleSession(TestKit.Party(victim, survivor), TestKit.Party(killer), TestKit.Services(random, rules: rules));

            // 스크립트 난수가 비어 있으면 최소값 → 명중 성공. Deathblow 저항 0%라 반드시 사망.
            battle.Start();

            Assert.That(victim.IsDead, Is.True);
            Assert.That(survivor.Stress.Current, Is.GreaterThanOrEqualTo(rules.StressOnAllyDeath));
        }

        [Test]
        public void SameSeed_ReplaysIdentically()
        {
            Assert.That(RunAutoBattle(1234), Is.EqualTo(RunAutoBattle(1234)));
        }

        [Test]
        public void AutoBattles_AlwaysTerminate([Range(1, 40)] int seed)
        {
            var log = RunAutoBattle(seed);
            Assert.That(log, Does.EndWith("Victory").Or.EndWith("Defeat"));
        }

        /// <summary>DefaultContent로 4:4 자동 전투를 끝까지 돌리고 명령 기록 문자열을 돌려준다.</summary>
        private static string RunAutoBattle(int seed)
        {
            var random = new SeededRandom(seed);
            var catalog = DefaultContent.Create();
            var factory = new CombatantFactory(catalog, new SequentialIdGenerator(), 200);
            var heroes = new Formation(Team.Heroes);
            foreach (var heroClass in catalog.HeroClasses) heroes.Add(factory.CreateHero(heroClass, heroClass.DisplayName));
            var monsters = factory.CreateMonsterFormation(catalog.NormalGroups[seed % catalog.NormalGroups.Count]);

            var battle = new BattleSession(heroes, monsters, TestKit.Services(random));
            var autoPilot = new RandomEnemyAI();
            battle.Start();
            int guard = 0;
            while (!battle.IsOver && guard++ < 1000)
            {
                var command = autoPilot.Decide(battle.Context, battle.CurrentActor);
                Assert.That(battle.Submit(command), Is.Null, "자동 명령은 항상 유효해야 한다");
            }

            var lines = new List<string>();
            foreach (var cmd in battle.Context.History) lines.Add(cmd.Describe());
            lines.Add(battle.Phase.ToString());
            return string.Join("\n", lines);
        }
    }
}
