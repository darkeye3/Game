using Darkest.Domain.Combat;
using Darkest.Domain.Core;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Dungeon
{
    /// <summary>골동품 결과를 파티/인벤토리에 적용한다. 결과 종류가 늘면 여기만 확장한다.</summary>
    public sealed class CurioResolver
    {
        private readonly StressService _stress;
        private readonly PartyHazard _hazard;

        public CurioResolver(StressService stress, PartyHazard hazard)
        {
            _stress = Guard.NotNull(stress, nameof(stress));
            _hazard = Guard.NotNull(hazard, nameof(hazard));
        }

        public void Apply(CurioOutcome outcome, Formation party, Inventory inventory, TorchLight light)
        {
            switch (outcome.Kind)
            {
                case CurioEffectKind.Nothing:
                    break;
                case CurioEffectKind.Gold:
                    inventory.AddGold(outcome.Amount);
                    break;
                case CurioEffectKind.PartyStress:
                    _stress.ApplyToAll(party.Members, outcome.Amount);
                    break;
                case CurioEffectKind.PartyDamage:
                    _hazard.DamageAll(party, outcome.Amount);
                    break;
                case CurioEffectKind.PartyHeal:
                    for (int i = 0; i < party.Members.Count; i++) party.Members[i].Heal(outcome.Amount);
                    break;
                case CurioEffectKind.Light:
                    light.Change(outcome.Amount);
                    break;
                case CurioEffectKind.Food:
                    inventory.AddFood(outcome.Amount);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(outcome), outcome.Kind, "처리하지 않은 골동품 결과");
            }
        }
    }
}
