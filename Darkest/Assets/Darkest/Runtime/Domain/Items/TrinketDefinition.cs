using System.Collections.Generic;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Core;

namespace Darkest.Domain.Items
{
    public enum TrinketRarity
    {
        Common,
        Rare
    }

    /// <summary>
    /// 장신구 정의(불변 데이터). 능력치 보정만 가진다. 장착하면 Hero가 슬롯별 출처 ID로 보정치를 붙인다.
    /// </summary>
    public sealed class TrinketDefinition
    {
        public TrinketDefinition(string id, string displayName, TrinketRarity rarity, IReadOnlyList<StatBonus> bonuses,
            int sellValue, int dropWeight)
        {
            Id = Guard.NotBlank(id, nameof(id));
            DisplayName = Guard.NotBlank(displayName, nameof(displayName));
            Rarity = rarity;
            Guard.NotEmpty(bonuses, nameof(bonuses));
            Bonuses = bonuses;
            SellValue = Guard.NonNegative(sellValue, nameof(sellValue));
            Guard.That(dropWeight > 0, id + ": 드롭 가중치는 1 이상이어야 합니다.");
            DropWeight = dropWeight;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public TrinketRarity Rarity { get; }
        public IReadOnlyList<StatBonus> Bonuses { get; }

        /// <summary>마을에서 팔 때 받는 골드.</summary>
        public int SellValue { get; }

        /// <summary>전리품으로 뽑힐 상대 가중치(희귀할수록 작게).</summary>
        public int DropWeight { get; }

        public override string ToString() => DisplayName;
    }

    /// <summary>출처가 정해지기 전의 능력치 보정(값 객체). 장착/버프 시점에 StatModifier로 바뀐다.</summary>
    public readonly struct StatBonus
    {
        public StatBonus(StatType stat, int amount)
        {
            Stat = stat;
            Amount = amount;
        }

        public StatType Stat { get; }
        public int Amount { get; }

        public StatModifier WithSource(string sourceId) => new StatModifier(Stat, Amount, sourceId);
    }
}
