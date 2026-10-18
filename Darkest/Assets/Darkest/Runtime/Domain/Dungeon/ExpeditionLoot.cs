using Darkest.Domain.Core;
using Darkest.Domain.Items;

namespace Darkest.Domain.Dungeon
{
    /// <summary>원정 전리품 지급(골드 빛 보정, 장신구 드롭). Expedition은 "언제" 주는지만 정하고 "얼마나"는 여기서 정한다.</summary>
    internal sealed class ExpeditionLoot
    {
        private readonly ExpeditionRules _rules;
        private readonly LightEffects _lightEffects;
        private readonly TrinketDropTable _trinkets;
        private readonly Inventory _inventory;
        private readonly IRandom _random;
        private readonly IEventBus _events;

        public ExpeditionLoot(ExpeditionRules rules, LightEffects lightEffects, TrinketDropTable trinkets, Inventory inventory,
            IRandom random, IEventBus events)
        {
            _rules = Guard.NotNull(rules, nameof(rules));
            _lightEffects = Guard.NotNull(lightEffects, nameof(lightEffects));
            _trinkets = Guard.NotNull(trinkets, nameof(trinkets));
            _inventory = Guard.NotNull(inventory, nameof(inventory));
            _random = Guard.NotNull(random, nameof(random));
            _events = Guard.NotNull(events, nameof(events));
        }

        /// <summary>빛 단계 보너스를 적용한 골드 지급.</summary>
        public void GainScaledGold(int baseGold, LightLevel light, string source)
        {
            int gold = _lightEffects.ScaleLoot(baseGold, light);
            if (gold <= 0) return;
            _inventory.AddGold(gold);
            _events.Publish(new LootGained(gold, source));
        }

        public void RollTreasureTrinket()
        {
            if (_random.RollPercent(_rules.TreasureTrinketChancePercent)) GainTrinket("보물");
        }

        public void DropBossTrinkets()
        {
            for (int i = 0; i < _rules.BossTrinketDrops; i++) GainTrinket("보스");
        }

        private void GainTrinket(string source)
        {
            var trinket = _trinkets.Roll(_random);
            if (trinket == null) return;
            _inventory.AddTrinket(trinket);
            _events.Publish(new TrinketFound(trinket, source));
        }
    }
}
