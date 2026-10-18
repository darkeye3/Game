using Darkest.Domain.Combat;
using Darkest.Domain.Core;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Dungeon
{
    public sealed class CampMade : IDomainEvent
    {
        public CampMade(int healedTotal, int stressRelief) { HealedTotal = healedTotal; StressRelief = stressRelief; }
        public int HealedTotal { get; }
        public int StressRelief { get; }
        public override string ToString() => "Camp healed=" + HealedTotal + " stress=-" + StressRelief;
    }

    /// <summary>
    /// 야영 규칙: 장작 1개 + 인원수만큼 식량 → HP 회복, 스트레스 완화, 지속 피해 제거.
    /// 빛은 LightAfterCamp까지만 낮아지고, 원래 더 어두웠다면 그대로 둔다(야영이 횃불 대용이 되지 않게).
    /// </summary>
    public sealed class Camp
    {
        private readonly ExpeditionRules _rules;
        private readonly StressService _stress;
        private readonly IEventBus _events;

        public Camp(ExpeditionRules rules, StressService stress, IEventBus events)
        {
            _rules = Guard.NotNull(rules, nameof(rules));
            _stress = Guard.NotNull(stress, nameof(stress));
            _events = Guard.NotNull(events, nameof(events));
        }

        public int FoodCost(Formation party) => party.Count * _rules.CampFoodPerHero;

        /// <returns>실패 사유, 성공 시 null. 실패하면 아무것도 소모하지 않는다.</returns>
        public string TryMake(Formation party, Inventory inventory, TorchLight light)
        {
            if (inventory.Firewood <= 0) return "장작이 없습니다.";
            if (inventory.Food < FoodCost(party)) return "식량이 부족합니다. (필요 " + FoodCost(party) + ")";

            inventory.TryConsumeFirewood();
            inventory.TryConsumeFood(FoodCost(party));

            int healedTotal = 0;
            for (int i = 0; i < party.Members.Count; i++)
            {
                var member = party.Members[i];
                int amount = System.Math.Max(1, member.MaxHp * _rules.CampHealPercent / RandomExtensions.PercentScale);
                healedTotal += member.Heal(amount);
                member.Statuses.Clear();
            }
            _stress.ApplyToAll(party.Members, -_rules.CampStressRelief);
            if (light.Value > _rules.LightAfterCamp) light.Change(_rules.LightAfterCamp - light.Value);

            _events.Publish(new CampMade(healedTotal, _rules.CampStressRelief));
            return null;
        }
    }
}
