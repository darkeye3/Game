using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Core;
using Darkest.Domain.Items;

namespace Darkest.Domain.Hamlet
{
    /// <summary>마을(영지) 상태: 골드, 주차, 영웅 명부, 모집 후보, 건물 단계, 장신구 보관함. 규칙은 서비스 클래스에 있다.</summary>
    public sealed class Town
    {
        private readonly List<Hero> _roster = new List<Hero>();
        private readonly List<Hero> _recruits = new List<Hero>();
        private readonly List<TrinketDefinition> _trinkets = new List<TrinketDefinition>();
        private readonly int[] _buildingLevels = new int[BuildingRules.BuildingCount];

        public Town(int gold, int week)
        {
            Gold = Guard.NonNegative(gold, nameof(gold));
            Week = week;
        }

        public int Gold { get; private set; }
        public int Week { get; private set; }
        public IReadOnlyList<Hero> Roster => _roster;
        public IReadOnlyList<Hero> Recruits => _recruits;

        /// <summary>장착하지 않은 장신구 보관함.</summary>
        public IReadOnlyList<TrinketDefinition> Trinkets => _trinkets;

        public int BuildingLevel(BuildingType type) => _buildingLevels[(int)type];

        internal bool TrySpend(int amount)
        {
            Guard.NonNegative(amount, nameof(amount));
            if (Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        internal void Earn(int amount) => Gold += Guard.NonNegative(amount, nameof(amount));
        internal void AdvanceWeek() => Week++;
        internal void AddToRoster(Hero hero) => _roster.Add(Guard.NotNull(hero, nameof(hero)));
        internal bool RemoveFromRoster(Hero hero) => _roster.Remove(hero);
        internal void ClearRecruits() => _recruits.Clear();
        internal void AddRecruit(Hero hero) => _recruits.Add(hero);
        internal bool RemoveRecruit(Hero hero) => _recruits.Remove(hero);
        internal void SetBuildingLevel(BuildingType type, int level) => _buildingLevels[(int)type] = Guard.NonNegative(level, nameof(level));
        internal void AddTrinket(TrinketDefinition trinket) => _trinkets.Add(Guard.NotNull(trinket, nameof(trinket)));
        internal bool RemoveTrinket(TrinketDefinition trinket) => _trinkets.Remove(trinket);

        /// <summary>영웅이 낀 장신구를 모두 보관함으로 되돌린다(해고 시).</summary>
        internal void StashTrinketsOf(Hero hero)
        {
            for (int slot = 0; slot < Hero.TrinketSlots; slot++)
            {
                var trinket = hero.GetTrinket(slot);
                if (trinket == null) continue;
                hero.SetTrinket(slot, null);
                _trinkets.Add(trinket);
            }
        }

        public Hero FindHero(string heroId)
        {
            for (int i = 0; i < _roster.Count; i++)
            {
                if (_roster[i].Id == heroId) return _roster[i];
            }
            return null;
        }
    }
}
