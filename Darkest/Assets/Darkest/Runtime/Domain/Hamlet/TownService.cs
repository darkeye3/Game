using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Progression;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Hamlet
{
    public sealed class TownChanged : IDomainEvent
    {
        public TownChanged(string reason) { Reason = reason; }
        public string Reason { get; }
        public override string ToString() => "TownChanged " + Reason;
    }

    /// <summary>
    /// 마을 행동(모집/선술집/보급/귀환 정산). 성공/실패를 문자열 사유로 돌려주어 UI가 그대로 표시할 수 있게 한다.
    /// </summary>
    public sealed class TownService
    {
        private readonly Town _town;
        private readonly TownRules _rules;
        private readonly StressService _stress;
        private readonly HeroProgression _progression;
        private readonly CombatantFactory _factory;
        private readonly IRandom _random;
        private readonly IEventBus _events;

        public TownService(Town town, TownRules rules, StressService stress, CombatantFactory factory,
            HeroProgression progression, IRandom random, IEventBus events)
        {
            _progression = Guard.NotNull(progression, nameof(progression));
            _town = Guard.NotNull(town, nameof(town));
            _rules = Guard.NotNull(rules, nameof(rules));
            _stress = Guard.NotNull(stress, nameof(stress));
            _factory = Guard.NotNull(factory, nameof(factory));
            _random = Guard.NotNull(random, nameof(random));
            _events = Guard.NotNull(events, nameof(events));
            Buildings = new TownBuildings(town, rules, events);
            Trinkets = new TrinketService(town, events);
        }

        public Town Town => _town;
        public TownRules Rules => _rules;

        /// <summary>건물 단계와 효과(모집 수, 선술집, 보급 할인, 훈련소 경험치).</summary>
        public TownBuildings Buildings { get; }
        public TrinketService Trinkets { get; }

        public int RecruitCost => _rules.RecruitCost;
        public int TavernCost => Buildings.TavernCost;

        /// <summary>새 게임: 시작 영웅 + 모집 후보 생성.</summary>
        public void SetupNewGame()
        {
            for (int i = 0; i < _rules.StartingHeroes; i++) _town.AddToRoster(_factory.CreateRandomHero(_random));
            RefreshRecruits();
        }

        public void RefreshRecruits()
        {
            _town.ClearRecruits();
            for (int i = 0; i < Buildings.RecruitPoolSize; i++) _town.AddRecruit(_factory.CreateRandomHero(_random));
            _events.Publish(new TownChanged("모집 후보 갱신"));
        }

        public string TryRecruit(Hero candidate)
        {
            if (candidate == null || !Contains(_town.Recruits, candidate)) return "모집 후보가 아닙니다.";
            if (_town.Roster.Count >= Buildings.RosterCapacity) return "명부가 가득 찼습니다.";
            if (!_town.TrySpend(_rules.RecruitCost)) return "골드가 부족합니다.";

            _town.RemoveRecruit(candidate);
            _town.AddToRoster(candidate);
            _events.Publish(new TownChanged(candidate.DisplayName + " 모집"));
            return null;
        }

        public string TryTreatStress(Hero hero)
        {
            if (hero == null || _town.FindHero(hero.Id) == null) return "명부에 없는 영웅입니다.";
            if (hero.Stress.Current == 0) return "스트레스가 없습니다.";
            if (!_town.TrySpend(Buildings.TavernCost)) return "골드가 부족합니다.";

            _stress.Apply(hero, -Buildings.TavernStressRelief);
            _events.Publish(new TownChanged(hero.DisplayName + " 선술집 치료"));
            return null;
        }

        public string TryDismiss(Hero hero)
        {
            if (hero == null || !_town.RemoveFromRoster(hero)) return "명부에 없는 영웅입니다.";
            _town.StashTrinketsOf(hero); // 해고해도 장신구는 영지에 남는다.
            _events.Publish(new TownChanged(hero.DisplayName + " 해고"));
            return null;
        }

        public int ProvisionCost(Provisions provisions) =>
            provisions.Food * FoodPrice + provisions.Torches * TorchPrice + provisions.Firewood * FirewoodPrice;

        public int FoodPrice => Buildings.ProvisionPrice(_rules.FoodPrice);
        public int TorchPrice => Buildings.ProvisionPrice(_rules.TorchPrice);
        public int FirewoodPrice => Buildings.ProvisionPrice(_rules.FirewoodPrice);

        /// <summary>보급품 구매 → 탐험용 인벤토리 생성.</summary>
        public string TryBuyProvisions(Provisions provisions, out Inventory inventory)
        {
            inventory = null;
            if (!provisions.IsValid) return "수량 오류";
            if (!_town.TrySpend(ProvisionCost(provisions))) return "골드가 부족합니다.";
            inventory = new Inventory(provisions);
            return null;
        }

        /// <summary>
        /// 탐험 귀환 정산: 사망자 제거(장신구도 잃음), 경험치(훈련소 보너스 포함), 생존자 회복·야영 버프 제거,
        /// 전리품 골드·장신구 입고(전멸 제외), 주차 진행, 모집 후보 갱신.
        /// </summary>
        public void SettleExpedition(Expedition expedition)
        {
            Guard.NotNull(expedition, nameof(expedition));
            for (int i = 0; i < expedition.Fallen.Count; i++) _town.RemoveFromRoster(expedition.Fallen[i]);
            int xp = SettlementXp(expedition);
            for (int i = 0; i < expedition.Embarked.Count; i++)
            {
                var hero = expedition.Embarked[i];
                hero.Modifiers.RemoveBySource(CampSkillDefinition.BuffSourceId);
                _progression.Grant(hero, xp); // 레벨업으로 MaxHp가 오른 뒤에 완전 회복
                if (hero.IsAlive) hero.Heal(hero.MaxHp);
                hero.Statuses.Clear();
            }

            // 전멸하면 전리품은 던전에 남는다.
            if (expedition.Phase != ExpeditionPhase.Failed)
            {
                _town.Earn(expedition.Inventory.Gold);
                for (int i = 0; i < expedition.Inventory.Trinkets.Count; i++) _town.AddTrinket(expedition.Inventory.Trinkets[i]);
            }
            _town.AdvanceWeek();
            RefreshRecruits();
            _events.Publish(new TownChanged("탐험 정산"));
        }

        /// <summary>귀환 정산 때 생존자 1인당 받을 경험치(원정 결과 + 훈련소 보너스).</summary>
        public int SettlementXp(Expedition expedition) => _progression.ExpeditionXp(expedition) + Buildings.ExpeditionXpBonus;

        private static bool Contains(IReadOnlyList<Hero> heroes, Hero hero)
        {
            for (int i = 0; i < heroes.Count; i++)
            {
                if (heroes[i] == hero) return true;
            }
            return false;
        }
    }
}
