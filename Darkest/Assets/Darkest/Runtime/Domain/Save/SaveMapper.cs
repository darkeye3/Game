using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Hamlet;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Save
{
    /// <summary>런타임 상태 ↔ 세이브 DTO 변환(마을 골드/주차/영웅 명부). 원정 진행도는 ExpeditionSaveMapper가 맡는다.</summary>
    public sealed class SaveMapper
    {
        private readonly IContentCatalog _catalog;
        private readonly CombatantFactory _factory;
        private readonly StressService _stress;
        private readonly IGameLogger _logger;

        public SaveMapper(IContentCatalog catalog, CombatantFactory factory, StressService stress, IGameLogger logger)
        {
            _catalog = Guard.NotNull(catalog, nameof(catalog));
            _factory = Guard.NotNull(factory, nameof(factory));
            _stress = Guard.NotNull(stress, nameof(stress));
            _logger = logger ?? NullLogger.Instance;
        }

        public GameSaveData Capture(Town town)
        {
            Guard.NotNull(town, nameof(town));
            var data = new GameSaveData { Gold = town.Gold, Week = town.Week, NextId = _factory.Ids.PeekNext };
            for (int i = 0; i < town.Roster.Count; i++) data.Roster.Add(CaptureHero(town.Roster[i]));
            return data;
        }

        /// <summary>마을 + 명부 복원. 알 수 없는 직업은 에러 로그 후 건너뛴다(콘텐츠 삭제된 구버전 세이브 대비).</summary>
        public Town Restore(GameSaveData data)
        {
            Guard.NotNull(data, nameof(data));
            _factory.Ids.EnsureAbove(data.NextId - 1);

            var town = new Town(data.Gold, data.Week);
            foreach (var saved in data.Roster)
            {
                var hero = RestoreHero(saved);
                if (hero != null) town.AddToRoster(hero);
            }
            return town;
        }

        private static HeroSaveData CaptureHero(Hero hero) => new HeroSaveData
        {
            Id = hero.Id,
            ClassId = hero.ClassId,
            Name = hero.DisplayName,
            Hp = hero.CurrentHp,
            Stress = hero.Stress.Current,
            ResolveId = hero.Stress.Resolve?.Id ?? string.Empty,
            IsDead = hero.IsDead,
            IsAtDeathsDoor = hero.IsAtDeathsDoor
        };

        private Hero RestoreHero(HeroSaveData saved)
        {
            HeroClassDefinition heroClass;
            try
            {
                heroClass = _catalog.GetHeroClass(saved.ClassId);
            }
            catch (KeyNotFoundException)
            {
                _logger.Error("Save", "알 수 없는 직업 ID — 영웅 복원 생략: " + saved.ClassId + " (" + saved.Name + ")");
                return null;
            }

            var hero = _factory.CreateHero(heroClass, saved.Name, saved.Id);
            hero.RestoreHp(saved.Hp);
            _stress.Restore(hero, saved.Stress, _catalog.FindResolve(saved.ResolveId));
            if (saved.IsDead) hero.RestoreDead();
            else if (saved.IsAtDeathsDoor) hero.RestoreDeathsDoor();
            return hero;
        }
    }
}
