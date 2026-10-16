using System.Collections.Generic;
using Darkest.Domain.Core;

namespace Darkest.Domain.Dungeon
{
    public enum CurioEffectKind
    {
        Nothing,
        Gold,
        PartyStress,
        PartyDamage,
        PartyHeal,
        Light,
        Food
    }

    /// <summary>골동품 결과 1개. Weight 비율로 뽑힌다.</summary>
    public sealed class CurioOutcome
    {
        public CurioOutcome(int weight, CurioEffectKind kind, int amount, string description)
        {
            Guard.That(weight > 0, "가중치는 1 이상");
            Weight = weight;
            Kind = kind;
            Amount = amount;
            Description = description ?? string.Empty;
        }

        public int Weight { get; }
        public CurioEffectKind Kind { get; }
        public int Amount { get; }
        public string Description { get; }
    }

    public sealed class CurioDefinition
    {
        public CurioDefinition(string id, string displayName, IReadOnlyList<CurioOutcome> outcomes)
        {
            Id = Guard.NotBlank(id, nameof(id));
            DisplayName = Guard.NotBlank(displayName, nameof(displayName));
            Guard.NotEmpty(outcomes, nameof(outcomes));
            Outcomes = outcomes;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<CurioOutcome> Outcomes { get; }

        public CurioOutcome Roll(IRandom random)
        {
            int total = 0;
            for (int i = 0; i < Outcomes.Count; i++) total += Outcomes[i].Weight;

            int roll = random.Range(0, total);
            for (int i = 0; i < Outcomes.Count; i++)
            {
                roll -= Outcomes[i].Weight;
                if (roll < 0) return Outcomes[i];
            }
            return Outcomes[Outcomes.Count - 1];
        }
    }
}
