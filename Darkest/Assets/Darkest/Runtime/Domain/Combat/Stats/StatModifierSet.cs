using System.Collections.Generic;

namespace Darkest.Domain.Combat.Stats
{
    public readonly struct StatModifier
    {
        public StatModifier(StatType stat, int amount, string sourceId)
        {
            Stat = stat;
            Amount = amount;
            SourceId = sourceId;
        }

        public StatType Stat { get; }
        public int Amount { get; }

        /// <summary>어디서 왔는지(예: "affliction:paranoid"). 같은 출처를 한 번에 제거하기 위해 쓴다.</summary>
        public string SourceId { get; }
    }

    /// <summary>버프/디버프/고통 등 런타임 가감치. 출처 단위로 붙이고 뗀다.</summary>
    public sealed class StatModifierSet
    {
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();

        public IReadOnlyList<StatModifier> All => _modifiers;

        public void Add(StatModifier modifier) => _modifiers.Add(modifier);

        public void AddRange(IReadOnlyList<StatModifier> modifiers)
        {
            for (int i = 0; i < modifiers.Count; i++) _modifiers.Add(modifiers[i]);
        }

        public int RemoveBySource(string sourceId)
        {
            int removed = 0;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (_modifiers[i].SourceId != sourceId) continue;
                _modifiers.RemoveAt(i);
                removed++;
            }
            return removed;
        }

        public int Sum(StatType stat)
        {
            int total = 0;
            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i].Stat == stat) total += _modifiers[i].Amount;
            }
            return total;
        }
    }
}
