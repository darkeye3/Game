using Darkest.Domain.Core;

namespace Darkest.Domain.Hamlet
{
    /// <summary>
    /// 건물 업그레이드와 그 효과 계산. 효과는 "기본 규칙값 + 단계 × 단계당 보정"으로 한 곳에서만 계산해
    /// 모집/선술집/보급 코드가 건물 단계를 직접 알 필요가 없게 한다.
    /// </summary>
    public sealed class TownBuildings
    {
        private readonly Town _town;
        private readonly TownRules _townRules;
        private readonly BuildingRules _rules;
        private readonly IEventBus _events;

        public TownBuildings(Town town, TownRules townRules, IEventBus events)
        {
            _town = Guard.NotNull(town, nameof(town));
            _townRules = Guard.NotNull(townRules, nameof(townRules));
            _rules = Guard.NotNull(townRules.Buildings, nameof(townRules.Buildings));
            _events = Guard.NotNull(events, nameof(events));
            Guard.That(_rules.UpgradeCosts.Length >= _rules.MaxLevel, "건물 업그레이드 비용이 최대 단계보다 적습니다.");
        }

        public BuildingRules Rules => _rules;

        public int Level(BuildingType type) => _town.BuildingLevel(type);

        public bool IsMaxed(BuildingType type) => Level(type) >= _rules.MaxLevel;

        /// <returns>다음 단계 비용. 최대 단계면 -1.</returns>
        public int UpgradeCost(BuildingType type) => IsMaxed(type) ? -1 : _rules.UpgradeCosts[Level(type)];

        public string TryUpgrade(BuildingType type)
        {
            if (IsMaxed(type)) return "이미 최대 단계입니다.";
            if (!_town.TrySpend(UpgradeCost(type))) return "골드가 부족합니다.";
            _town.SetBuildingLevel(type, Level(type) + 1);
            _events.Publish(new TownChanged(type + " " + Level(type) + "단계"));
            return null;
        }

        public int RosterCapacity => _townRules.MaxRosterSize + Level(BuildingType.Stagecoach) * _rules.StagecoachRosterPerLevel;
        public int RecruitPoolSize => _townRules.RecruitPoolSize + Level(BuildingType.Stagecoach) * _rules.StagecoachRecruitsPerLevel;
        public int TavernStressRelief => _townRules.TavernStressRelief + Level(BuildingType.Tavern) * _rules.TavernReliefPerLevel;
        public int TavernCost => System.Math.Max(0, _townRules.TavernCost - Level(BuildingType.Tavern) * _rules.TavernDiscountPerLevel);
        public int ExpeditionXpBonus => Level(BuildingType.Guild) * _rules.GuildXpPerLevel;

        /// <summary>보급상 할인 적용 가격(내림). 할인은 100%를 넘지 않는다.</summary>
        public int ProvisionPrice(int basePrice)
        {
            int discount = System.Math.Min(RandomExtensions.PercentScale, Level(BuildingType.Provisioner) * _rules.ProvisionerDiscountPercentPerLevel);
            return basePrice * (RandomExtensions.PercentScale - discount) / RandomExtensions.PercentScale;
        }
    }
}
