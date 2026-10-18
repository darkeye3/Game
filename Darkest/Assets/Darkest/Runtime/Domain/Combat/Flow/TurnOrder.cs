using System.Collections.Generic;
using Darkest.Domain.Combat.Resolution;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Core;

namespace Darkest.Domain.Combat.Flow
{
    /// <summary>라운드 턴 순서 결정 Strategy.</summary>
    public interface ITurnOrderStrategy
    {
        void BuildRound(BattleContext context, Queue<Combatant> output);
    }

    /// <summary>속도 + 1dN 내림차순. 동점이면 영웅 우선, 그다음 열 순서(결정적 정렬 → 재현성 보장).</summary>
    public sealed class SpeedInitiativeTurnOrder : ITurnOrderStrategy
    {
        private readonly CombatRules _rules;
        private readonly List<Entry> _buffer = new List<Entry>(Formation.MaxRanks * 2);

        public SpeedInitiativeTurnOrder(CombatRules rules)
        {
            _rules = Guard.NotNull(rules, nameof(rules));
        }

        public void BuildRound(BattleContext context, Queue<Combatant> output)
        {
            _buffer.Clear();
            Collect(context.Heroes, context.Random, 0);
            Collect(context.Monsters, context.Random, Formation.MaxRanks);
            _buffer.Sort(Compare);

            output.Clear();
            for (int i = 0; i < _buffer.Count; i++) output.Enqueue(_buffer[i].Combatant);
        }

        private void Collect(Formation formation, IRandom random, int orderOffset)
        {
            var members = formation.Members;
            for (int i = 0; i < members.Count; i++)
            {
                var combatant = members[i];
                if (combatant.IsDead) continue;
                int initiative = combatant.GetStat(StatType.Speed) + random.RangeInclusive(1, _rules.InitiativeDieSides);
                _buffer.Add(new Entry(combatant, initiative, orderOffset + i));
            }
        }

        private static int Compare(Entry a, Entry b)
        {
            int byInitiative = b.Initiative.CompareTo(a.Initiative);
            return byInitiative != 0 ? byInitiative : a.TieBreaker.CompareTo(b.TieBreaker);
        }

        private readonly struct Entry
        {
            public Entry(Combatant combatant, int initiative, int tieBreaker)
            {
                Combatant = combatant;
                Initiative = initiative;
                TieBreaker = tieBreaker;
            }

            public Combatant Combatant { get; }
            public int Initiative { get; }
            public int TieBreaker { get; }
        }
    }
}
