using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Resolution;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Combat.Status;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using NUnit.Framework;

namespace Darkest.Tests
{
    public class CampAndLightTests
    {
        [Test]
        public void Camp_HealsRelievesStressClearsDots_AndConsumesSupplies()
        {
            var rules = new ExpeditionRules { CampHealPercent = 50, CampStressRelief = 20, CampFoodPerHero = 1, LightAfterCamp = 40 };
            var stress = TestKit.StressService(new ScriptedRandom());
            var hero = TestKit.Hero("a", TestKit.Stats(hp: 20));
            var party = TestKit.Party(hero, TestKit.Hero("b"));
            hero.ApplyDamage(15, new ScriptedRandom());
            hero.Statuses.Add(new StatusEffect(StatusKind.Bleed, 2, 3));
            stress.Apply(hero, 30);
            var inventory = new Inventory(food: 2, torches: 0, firewood: 1);
            var light = new TorchLight(rules);

            Assert.That(new Camp(rules, stress, new EventBus()).TryMake(party, inventory, light), Is.Null);

            Assert.That(hero.CurrentHp, Is.EqualTo(15));
            Assert.That(hero.Stress.Current, Is.EqualTo(10));
            Assert.That(hero.Statuses.All.Count, Is.EqualTo(0));
            Assert.That(inventory.Firewood, Is.EqualTo(0));
            Assert.That(inventory.Food, Is.EqualTo(0));
            Assert.That(light.Value, Is.EqualTo(40));
        }

        [Test]
        public void Camp_NeverBrightensLight()
        {
            var rules = new ExpeditionRules { CampFoodPerHero = 1, LightAfterCamp = 50 };
            var camp = new Camp(rules, TestKit.StressService(new ScriptedRandom()), new EventBus());
            var light = new TorchLight(rules);
            light.Change(20 - light.Value);

            Assert.That(camp.TryMake(TestKit.Party(TestKit.Hero("a")), new Inventory(food: 1, torches: 0, firewood: 1), light), Is.Null);

            Assert.That(light.Value, Is.EqualTo(20), "이미 LightAfterCamp보다 어두우면 그대로");
        }

        [Test]
        public void PartyHazard_DeathsDoorAndAllyDeath_UseCombatStressRules()
        {
            var combat = new CombatRules { StressOnDeathsDoor = 15, StressOnAllyDeath = 20 };
            var random = new ScriptedRandom();
            var stress = TestKit.StressService(random);
            var hazard = new PartyHazard(combat, stress, random);
            var healthy = TestKit.Hero("healthy", TestKit.Stats(hp: 20));
            var fragile = TestKit.Hero("fragile", TestKit.Stats(hp: 4));
            var doomed = TestKit.Hero("doomed", TestKit.Stats(hp: 4, deathblow: 0));
            doomed.ApplyDamage(4, random); // 이미 죽음의 문턱
            var party = TestKit.Party(healthy, fragile, doomed);

            hazard.DamageAll(party, 4);

            Assert.That(fragile.IsAtDeathsDoor, Is.True);
            Assert.That(doomed.IsDead, Is.True);
            Assert.That(healthy.Stress.Current, Is.EqualTo(20), "동료 사망 +20");
            Assert.That(fragile.Stress.Current, Is.EqualTo(15 + 20), "죽음의 문턱 +15, 동료 사망 +20");
        }

        [Test]
        public void Camp_FailsWithoutConsumingWhenShort()
        {
            var rules = new ExpeditionRules();
            var camp = new Camp(rules, TestKit.StressService(new ScriptedRandom()), new EventBus());
            var party = TestKit.Party(TestKit.Hero("a"), TestKit.Hero("b"));

            var noWood = new Inventory(food: 5, torches: 0, firewood: 0);
            Assert.That(camp.TryMake(party, noWood, new TorchLight(rules)), Is.Not.Null);

            var noFood = new Inventory(food: 1, torches: 0, firewood: 1);
            Assert.That(camp.TryMake(party, noFood, new TorchLight(rules)), Is.Not.Null);
            Assert.That(noFood.Firewood, Is.EqualTo(1), "실패 시 장작을 소모하면 안 된다");
            Assert.That(noFood.Food, Is.EqualTo(1));
        }

        [Test]
        public void Darkness_BuffsMonsters_Idempotently()
        {
            var rules = new ExpeditionRules();
            var effects = new LightEffects(rules);
            var monster = TestKit.Monster();
            var monsters = TestKit.Party(monster);
            int baseAccuracy = monster.GetStat(StatType.Accuracy);

            effects.ApplyToMonsters(monsters, LightLevel.Dark);
            effects.ApplyToMonsters(monsters, LightLevel.Dark);
            Assert.That(monster.GetStat(StatType.Accuracy), Is.EqualTo(baseAccuracy + rules.MonsterAccuracyBonusByLight[3]), "중복 적용되면 안 된다");

            effects.ApplyToMonsters(monsters, LightLevel.Radiant);
            Assert.That(monster.GetStat(StatType.Accuracy), Is.EqualTo(baseAccuracy));
        }

        [Test]
        public void Darkness_IncreasesLoot()
        {
            var effects = new LightEffects(new ExpeditionRules { LootBonusPercentByLight = new[] { 0, 10, 25, 50 } });
            Assert.That(effects.ScaleLoot(100, LightLevel.Radiant), Is.EqualTo(100));
            Assert.That(effects.ScaleLoot(100, LightLevel.Dark), Is.EqualTo(150));
        }

        [Test]
        public void LightEffects_RejectsMisconfiguredArrays()
        {
            Assert.Throws<System.InvalidOperationException>(() => new LightEffects(new ExpeditionRules { LootBonusPercentByLight = new[] { 0 } }));
        }
    }
}
