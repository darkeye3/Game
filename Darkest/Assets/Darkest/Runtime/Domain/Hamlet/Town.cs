using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Core;

namespace Darkest.Domain.Hamlet
{
    /// <summary>마을(영지) 상태: 골드, 주차, 영웅 명부, 모집 후보. 규칙은 TownService에 있다.</summary>
    public sealed class Town
    {
        private readonly List<Hero> _roster = new List<Hero>();
        private readonly List<Hero> _recruits = new List<Hero>();

        public Town(int gold, int week)
        {
            Gold = Guard.NonNegative(gold, nameof(gold));
            Week = week;
        }

        public int Gold { get; private set; }
        public int Week { get; private set; }
        public IReadOnlyList<Hero> Roster => _roster;
        public IReadOnlyList<Hero> Recruits => _recruits;

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
