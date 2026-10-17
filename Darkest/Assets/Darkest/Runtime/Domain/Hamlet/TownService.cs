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
        }

        public Town Town => _town;
        public TownRules Rules => _rules;

        /// <summary>새 게임: 시작 영웅 + 모집 후보 생성.</summary>
        public void SetupNewGame()
        {
            for (int i = 0; i < _rules.StartingHeroes; i++) _town.AddToRoster(_factory.CreateRandomHero(_random));
            RefreshRecruits();
        }

        public void RefreshRecruits()
        {
            _town.ClearRecruits();
            for (int i = 0; i < _rules.RecruitPoolSize; i++) _town.AddRecruit(_factory.CreateRandomHero(_random));
            _events.Publish(new TownChanged("모집 후보 갱신"));
        }

        public string TryRecruit(Hero candidate)
        {
            if (candidate == null || !Contains(_town.Recruits, candidate)) return "모집 후보가 아닙니다.";
            if (_town.Roster.Count >= _rules.MaxRosterSize) return "명부가 가득 찼습니다.";
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
            if (!_town.TrySpend(_rules.TavernCost)) return "골드가 부족합니다.";

            _stress.Apply(hero, -_rules.TavernStressRelief);
            _events.Publish(new TownChanged(hero.DisplayName + " 선술집 치료"));
            return null;
        }

        public string TryDismiss(Hero hero)
        {
            if (hero == null || !_town.RemoveFromRoster(hero)) return "명부에 없는 영웅입니다.";
            _events.Publish(new TownChanged(hero.DisplayName + " 해고"));
            return null;
        }

        public int ProvisionCost(Provisions provisions) =>
            provisions.Food * _rules.FoodPrice + provisions.Torches * _rules.TorchPrice + provisions.Firewood * _rules.FirewoodPrice;

        /// <summary>보급품 구매 → 탐험용 인벤토리 생성.</summary>
        public string TryBuyProvisions(Provisions provisions, out Inventory inventory)
        {
            inventory = null;
            if (!provisions.IsValid) return "수량 오류";
            if (!_town.TrySpend(ProvisionCost(provisions))) return "골드가 부족합니다.";
            inventory = new Inventory(provisions);
            return null;
        }

        /// <summary>탐험 귀환 정산: 사망자 제거, 전리품 입금(전멸 제외), 주차 진행, 생존자 HP 회복, 모집 후보 갱신.</summary>
        public void SettleExpedition(Expedition expedition)
        {
            Guard.NotNull(expedition, nameof(expedition));
            for (int i = 0; i < expedition.Fallen.Count; i++) _town.RemoveFromRoster(expedition.Fallen[i]);
            int xp = _progression.ExpeditionXp(expedition);
            for (int i = 0; i < expedition.Embarked.Count; i++)
            {
                var hero = expedition.Embarked[i];
                _progression.Grant(hero, xp); // 레벨업으로 MaxHp가 오른 뒤에 완전 회복
                if (hero.IsAlive) hero.Heal(hero.MaxHp);
                hero.Statuses.Clear();
            }

            // 전멸하면 전리품은 던전에 남는다.
            if (expedition.Phase != ExpeditionPhase.Failed) _town.Earn(expedition.Inventory.Gold);
            _town.AdvanceWeek();
            RefreshRecruits();
            _events.Publish(new TownChanged("탐험 정산"));
        }

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
