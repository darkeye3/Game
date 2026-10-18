using System.Collections.Generic;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Core;
using Darkest.Domain.Items;

namespace Darkest.Domain.Dungeon
{
    public enum CampSkillTarget
    {
        Self,
        Ally,
        Party
    }

    public enum CampEffectKind
    {
        /// <summary>최대 HP 대비 %만큼 회복.</summary>
        HealPercent,
        /// <summary>스트레스 증감(음수 = 완화).</summary>
        Stress,
        /// <summary>다음 전투가 끝날 때까지 능력치 보정.</summary>
        Buff
    }

    /// <summary>야영 스킬 효과 1개. Buff일 때만 Stat을 쓴다.</summary>
    public readonly struct CampEffect
    {
        public CampEffect(CampEffectKind kind, int amount, StatType stat = StatType.MaxHp)
        {
            Kind = kind;
            Amount = amount;
            Stat = stat;
        }

        public CampEffectKind Kind { get; }
        public int Amount { get; }
        public StatType Stat { get; }

        public StatBonus ToBonus() => new StatBonus(Stat, Amount);
    }

    /// <summary>직업별 야영 스킬 정의(불변). 야영 1회에서 영웅마다 같은 스킬은 한 번만 쓸 수 있다.</summary>
    public sealed class CampSkillDefinition
    {
        /// <summary>야영 버프 보정치의 출처 ID. 다음 전투가 끝나면 이 출처를 한 번에 떼어낸다.</summary>
        public const string BuffSourceId = "camp_buff";

        public CampSkillDefinition(string id, string displayName, string classId, int cost, CampSkillTarget target,
            IReadOnlyList<CampEffect> effects)
        {
            Id = Guard.NotBlank(id, nameof(id));
            DisplayName = Guard.NotBlank(displayName, nameof(displayName));
            ClassId = Guard.NotBlank(classId, nameof(classId));
            Guard.That(cost > 0, id + ": 야영 스킬 비용은 1 이상이어야 합니다.");
            Cost = cost;
            Target = target;
            Guard.NotEmpty(effects, nameof(effects));
            Effects = effects;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string ClassId { get; }
        public int Cost { get; }
        public CampSkillTarget Target { get; }
        public IReadOnlyList<CampEffect> Effects { get; }

        public override string ToString() => DisplayName;
    }
}
