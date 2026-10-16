using System;
using Darkest.Domain.Combat;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Hamlet;
using Darkest.Domain.Save;
using Darkest.Domain.Stress;
using NUnit.Framework;

namespace Darkest.Tests
{
    public class HamletAndSaveTests
    {
        private static TownService CreateTown(int gold, out StressService stress, out CombatantFactory factory)
        {
            var random = new SeededRandom(1);
            var events = new EventBus();
            var catalog = DefaultContent.Create();
            stress = TestKit.StressService(random, events);
            factory = new CombatantFactory(catalog, new SequentialIdGenerator(), 200);
            var service = new TownService(new Town(gold, 1), new TownRules(), stress, factory, random, events);
            service.SetupNewGame();
            return service;
        }

        [Test]
        public void NewGame_HasStartingRosterAndRecruits()
        {
            var service = CreateTown(1000, out _, out _);
            Assert.That(service.Town.Roster.Count, Is.EqualTo(service.Rules.StartingHeroes));
            Assert.That(service.Town.Recruits.Count, Is.EqualTo(service.Rules.RecruitPoolSize));
        }

        [Test]
        public void Recruit_CostsGold_AndFailsWhenBroke()
        {
            var service = CreateTown(new TownRules().RecruitCost, out _, out _);
            var candidate = service.Town.Recruits[0];

            Assert.That(service.TryRecruit(candidate), Is.Null);
            Assert.That(service.Town.Gold, Is.EqualTo(0));
            Assert.That(service.Town.FindHero(candidate.Id), Is.SameAs(candidate));
            Assert.That(service.TryRecruit(service.Town.Recruits[0]), Is.EqualTo("골드가 부족합니다."));
        }

        [Test]
        public void Tavern_RelievesStress()
        {
            var service = CreateTown(1000, out var stress, out _);
            var hero = service.Town.Roster[0];
            stress.Apply(hero, 60);

            Assert.That(service.TryTreatStress(hero), Is.Null);
            Assert.That(hero.Stress.Current, Is.EqualTo(60 - service.Rules.TavernStressRelief));
            Assert.That(service.Town.Gold, Is.EqualTo(1000 - service.Rules.TavernCost));
        }

        [Test]
        public void Provisions_CostPerItem()
        {
            var service = CreateTown(1000, out _, out _);
            Assert.That(service.TryBuyProvisions(4, 2, out var inventory), Is.Null);
            Assert.That(inventory.Food, Is.EqualTo(4));
            Assert.That(service.Town.Gold, Is.EqualTo(1000 - service.ProvisionCost(4, 2)));
            Assert.That(service.TryBuyProvisions(1000, 0, out _), Is.Not.Null);
        }

        [Test]
        public void Serializer_RoundTrip_WithEscapedCharacters()
        {
            var data = new GameSaveData { Gold = 1234, Week = 7, NextId = 42 };
            data.Roster.Add(new HeroSaveData { Id = "hero-1", ClassId = "warden", Name = "이상한|이름\\줄\n바꿈", Hp = 12, Stress = 88, ResolveId = "aff_fearful" });
            var serializer = new LineSaveSerializer();

            var restored = serializer.Deserialize(serializer.Serialize(data));

            Assert.That(restored.Gold, Is.EqualTo(1234));
            Assert.That(restored.Week, Is.EqualTo(7));
            Assert.That(restored.NextId, Is.EqualTo(42));
            Assert.That(restored.Roster.Count, Is.EqualTo(1));
            Assert.That(restored.Roster[0].Name, Is.EqualTo("이상한|이름\\줄\n바꿈"));
            Assert.That(restored.Roster[0].ResolveId, Is.EqualTo("aff_fearful"));
        }

        [Test]
        public void Serializer_BrokenData_ThrowsWithLineNumber()
        {
            var serializer = new LineSaveSerializer();
            var ex = Assert.Throws<FormatException>(() => serializer.Deserialize("darkest-save|1\ntown|abc|1|1\n"));
            Assert.That(ex.Message, Does.Contain("2번째 줄"));
            Assert.Throws<FormatException>(() => serializer.Deserialize(""));
            Assert.Throws<FormatException>(() => serializer.Deserialize("darkest-save|999\ntown|1|1|1\n"));
        }

        [Test]
        public void Mapper_RoundTrip_RestoresHeroStateAndResolve()
        {
            var service = CreateTown(500, out var stress, out var factory);
            var hero = service.Town.Roster[0];
            hero.ApplyDamage(5, new ScriptedRandom());
            stress.Apply(hero, 30);
            var catalog = DefaultContent.Create();
            var affliction = catalog.Afflictions[0];

            var mapper = new SaveMapper(catalog, factory, stress, NullLogger.Instance);
            var data = mapper.Capture(service.Town);
            data.Roster[0].ResolveId = affliction.Id;

            var text = new LineSaveSerializer().Serialize(data);
            var freshFactory = new CombatantFactory(catalog, new SequentialIdGenerator(), 200);
            var restoredTown = new SaveMapper(catalog, freshFactory, stress, NullLogger.Instance)
                .Restore(new LineSaveSerializer().Deserialize(text));

            var restored = restoredTown.FindHero(hero.Id);
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.CurrentHp, Is.EqualTo(hero.CurrentHp));
            Assert.That(restored.Stress.Current, Is.EqualTo(30));
            Assert.That(restored.Stress.State, Is.EqualTo(ResolveState.Afflicted));
            Assert.That(restoredTown.Gold, Is.EqualTo(service.Town.Gold));
            Assert.That(freshFactory.Ids.PeekNext, Is.GreaterThanOrEqualTo(data.NextId), "복원 후 새 ID가 기존 ID와 겹치면 안 된다");
        }

        [Test]
        public void Mapper_SkipsUnknownClass_InsteadOfCrashing()
        {
            var catalog = DefaultContent.Create();
            var logger = new MemoryLogger();
            var mapper = new SaveMapper(catalog, new CombatantFactory(catalog, new SequentialIdGenerator(), 200),
                TestKit.StressService(new ScriptedRandom()), logger);
            var data = new GameSaveData { Gold = 1, Week = 1, NextId = 1 };
            data.Roster.Add(new HeroSaveData { Id = "x", ClassId = "deleted_class", Name = "유령", Hp = 1 });

            var town = mapper.Restore(data);

            Assert.That(town.Roster.Count, Is.EqualTo(0));
            Assert.That(logger.Lines, Has.Some.Contains("deleted_class"));
        }
    }
}
