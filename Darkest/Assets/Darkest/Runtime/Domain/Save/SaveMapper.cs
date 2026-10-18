using System;
using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Combat.Status;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Hamlet;
using Darkest.Domain.Progression;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Save
{
    /// <summary>
    /// 런타임 상태 ↔ 세이브 DTO 변환(마을 골드/주차/건물/보관함, 영웅 명부와 장신구·상태이상·야영 버프).
    /// 원정 진행도는 ExpeditionSaveMapper가 맡는다. 삭제된 콘텐츠를 가리키는 항목은 에러 로그 후 건너뛴다.
    /// </summary>
    public sealed class SaveMapper
    {
        private const string LogCategory = "Save";

        private readonly IContentCatalog _catalog;
        private readonly CombatantFactory _factory;
        private readonly StressService _stress;
        private readonly HeroProgression _progression;
        private readonly IGameLogger _logger;

        public SaveMapper(IContentCatalog catalog, CombatantFactory factory, StressService stress, HeroProgression progression, IGameLogger logger)
        {
            _progression = Guard.NotNull(progression, nameof(progression));
            _catalog = Guard.NotNull(catalog, nameof(catalog));
            _factory = Guard.NotNull(factory, nameof(factory));
            _stress = Guard.NotNull(stress, nameof(stress));
            _logger = logger ?? NullLogger.Instance;
        }

        public GameSaveData Capture(Town town)
        {
            Guard.NotNull(town, nameof(town));
            var data = new GameSaveData { Gold = town.Gold, Week = town.Week, NextId = _factory.Ids.PeekNext };
            foreach (BuildingType type in Enum.GetValues(typeof(BuildingType)))
            {
                if (town.BuildingLevel(type) > 0) data.Buildings[type.ToString()] = town.BuildingLevel(type);
            }
            for (int i = 0; i < town.Trinkets.Count; i++) data.TrinketStash.Add(town.Trinkets[i].Id);
            for (int i = 0; i < town.Roster.Count; i++) data.Roster.Add(CaptureHero(town.Roster[i]));
            return data;
        }

        /// <summary>마을 + 명부 복원. 알 수 없는 직업은 에러 로그 후 건너뛴다(콘텐츠 삭제된 구버전 세이브 대비).</summary>
        public Town Restore(GameSaveData data)
        {
            Guard.NotNull(data, nameof(data));
            _factory.Ids.EnsureAbove(data.NextId - 1);

            var town = new Town(data.Gold, data.Week);
            foreach (var building in data.Buildings)
            {
                if (Enum.TryParse(building.Key, out BuildingType type)) town.SetBuildingLevel(type, building.Value);
                else _logger.Error(LogCategory, "알 수 없는 건물 — 건너뜀: " + building.Key);
            }
            foreach (var id in data.TrinketStash)
            {
                var trinket = FindTrinket(id);
                if (trinket != null) town.AddTrinket(trinket);
            }
            foreach (var saved in data.Roster)
            {
                var hero = RestoreHero(saved);
                if (hero != null) town.AddToRoster(hero);
            }
            return town;
        }

        private static HeroSaveData CaptureHero(Hero hero)
        {
            var data = NewHeroData(hero);
            for (int slot = 0; slot < Hero.TrinketSlots; slot++) data.Trinkets.Add(hero.GetTrinket(slot)?.Id ?? string.Empty);
            foreach (var status in hero.Statuses.All)
                data.Statuses.Add(new StatusSaveData(status.Kind.ToString(), status.DamagePerTurn, status.RemainingTurns));
            foreach (var modifier in hero.Modifiers.All)
            {
                if (modifier.SourceId == CampSkillDefinition.BuffSourceId)
                    data.CampBuffs.Add(new StatBonusSaveData(modifier.Stat.ToString(), modifier.Amount));
            }
            return data;
        }

        private static HeroSaveData NewHeroData(Hero hero) => new HeroSaveData
        {
            Id = hero.Id,
            ClassId = hero.ClassId,
            Name = hero.DisplayName,
            Hp = hero.CurrentHp,
            Stress = hero.Stress.Current,
            ResolveId = hero.Stress.Resolve?.Id ?? string.Empty,
            IsDead = hero.IsDead,
            IsAtDeathsDoor = hero.IsAtDeathsDoor,
            Experience = hero.Experience
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
                _logger.Error(LogCategory, "알 수 없는 직업 ID — 영웅 복원 생략: " + saved.ClassId + " (" + saved.Name + ")");
                return null;
            }

            var hero = _factory.CreateHero(heroClass, saved.Name, saved.Id);
            _progression.Restore(hero, saved.Experience); // 레벨 보너스로 MaxHp가 바뀌므로 HP보다 먼저
            RestoreEquipment(hero, saved); // 장신구 HP 보정도 HP보다 먼저
            hero.RestoreHp(saved.Hp);
            _stress.Restore(hero, saved.Stress, _catalog.FindResolve(saved.ResolveId));
            if (saved.IsDead) hero.RestoreDead();
            else if (saved.IsAtDeathsDoor) hero.RestoreDeathsDoor();
            RestoreStatuses(hero, saved);
            return hero;
        }

        private void RestoreEquipment(Hero hero, HeroSaveData saved)
        {
            for (int slot = 0; slot < saved.Trinkets.Count && slot < Hero.TrinketSlots; slot++)
            {
                var trinket = FindTrinket(saved.Trinkets[slot]);
                if (trinket != null) hero.SetTrinket(slot, trinket);
            }
            foreach (var buff in saved.CampBuffs)
            {
                if (Enum.TryParse(buff.Stat, out StatType stat))
                    hero.Modifiers.Add(new StatModifier(stat, buff.Amount, CampSkillDefinition.BuffSourceId));
                else _logger.Error(LogCategory, "알 수 없는 능력치 — 야영 버프 생략: " + buff.Stat);
            }
        }

        private void RestoreStatuses(Hero hero, HeroSaveData saved)
        {
            if (hero.IsDead) return;
            foreach (var status in saved.Statuses)
            {
                if (Enum.TryParse(status.Kind, out StatusKind kind))
                    hero.Statuses.Add(new StatusEffect(kind, status.DamagePerTurn, status.RemainingTurns));
                else _logger.Error(LogCategory, "알 수 없는 상태이상 — 생략: " + status.Kind);
            }
        }

        private Items.TrinketDefinition FindTrinket(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var trinket = _catalog.FindTrinket(id);
            if (trinket == null) _logger.Error(LogCategory, "알 수 없는 장신구 — 건너뜀: " + id);
            return trinket;
        }
    }
}
