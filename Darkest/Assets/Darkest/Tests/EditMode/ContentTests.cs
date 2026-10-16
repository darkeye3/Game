using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Content;
using Darkest.Domain.Save;
using NUnit.Framework;

namespace Darkest.Tests
{
    public class ContentTests
    {
        [Test]
        public void DefaultContent_IsValid()
        {
            var catalog = DefaultContent.Create();
            Assert.That(catalog.HeroClasses.Count, Is.GreaterThanOrEqualTo(4));
            foreach (var heroClass in catalog.HeroClasses)
                Assert.That(heroClass.Skills.Count, Is.GreaterThan(0), heroClass.Id);
        }

        [Test]
        public void SkillEffects_SpecRoundTrip()
        {
            // 에셋 내보내기(ContentAssetGenerator)가 기대하는 성질: Create(spec).ToSpec() == spec
            foreach (var heroClass in DefaultContent.Create().HeroClasses)
            foreach (var skill in heroClass.Skills)
            foreach (var effect in skill.Effects)
            {
                var spec = effect.ToSpec();
                var again = SkillEffectFactory.Create(spec).ToSpec();
                Assert.That(again.Kind, Is.EqualTo(spec.Kind), skill.Id);
                Assert.That(again.Amount, Is.EqualTo(spec.Amount), skill.Id);
                Assert.That(again.AmountMax, Is.EqualTo(spec.AmountMax), skill.Id);
                Assert.That(again.Duration, Is.EqualTo(spec.Duration), skill.Id);
                Assert.That(again.Chance, Is.EqualTo(spec.Chance), skill.Id);
                Assert.That(effect.Describe(), Is.Not.Empty);
            }
        }

        [Test]
        public void RankMask_StringRoundTrip()
        {
            foreach (var text in new[] { "1", "1,2", "2,3,4", "1,2,3,4" })
                Assert.That(RankMaskExtensions.Parse(text).ToRankString(), Is.EqualTo(text));
        }

        [Test]
        public void Catalog_RejectsDuplicateIdsAndMissingMonsters()
        {
            var good = DefaultContent.Create();
            var heroes = new System.Collections.Generic.List<HeroClassDefinition>(good.HeroClasses);
            heroes.Add(good.HeroClasses[0]);
            Assert.Throws<System.ArgumentException>(() => new ContentCatalog(heroes, new MonsterDefinition[0],
                new MonsterGroupDefinition[0], good.Curios, new Domain.Stress.ResolveDefinition[0], good.HeroNames));

            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => new ContentCatalog(good.HeroClasses, new MonsterDefinition[0],
                new[] { new MonsterGroupDefinition("g", new[] { "missing" }, false, 0) }, good.Curios, new Domain.Stress.ResolveDefinition[0], good.HeroNames));
        }

        private sealed class MemoryRepository : ISaveRepository
        {
            public string Stored { get; set; }
            public bool TryLoad(out string text) { text = Stored; return Stored != null; }
            public void Save(string text) => Stored = text;
            public void Delete() => Stored = null;
        }

        [Test]
        public void SaveGateway_CorruptedSave_ReturnsFalseInsteadOfThrowing()
        {
            var repo = new MemoryRepository { Stored = "garbage" };
            var gateway = new SaveGateway(repo, new LineSaveSerializer(), Domain.Core.NullLogger.Instance);
            Assert.That(gateway.TryLoad(out _), Is.False);

            Assert.That(gateway.TrySave(new GameSaveData { Gold = 3, Week = 1, NextId = 1 }), Is.Null);
            Assert.That(gateway.TryLoad(out var data), Is.True);
            Assert.That(data.Gold, Is.EqualTo(3));
        }
    }
}
