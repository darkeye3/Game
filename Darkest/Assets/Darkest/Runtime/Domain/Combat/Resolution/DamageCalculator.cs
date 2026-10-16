using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Core;

namespace Darkest.Domain.Combat.Resolution
{
    /// <summary>피해량 계산 Strategy.</summary>
    public interface IDamageCalculator
    {
        int Calculate(Combatant attacker, Combatant target, SkillDefinition skill, bool isCrit, IRandom random);
    }

    /// <summary>
    /// 무기 피해(min~max) × 스킬 배율 → 치명타면 최대치 기준 × 치명 배율 → 방어(Protection)% 감소. 최소 1.
    /// </summary>
    public sealed class DefaultDamageCalculator : IDamageCalculator
    {
        private const int MinimumDamage = 1;
        private readonly CombatRules _rules;

        public DefaultDamageCalculator(CombatRules rules)
        {
            _rules = Guard.NotNull(rules, nameof(rules));
        }

        public int Calculate(Combatant attacker, Combatant target, SkillDefinition skill, bool isCrit, IRandom random)
        {
            if (!skill.DealsDamage) return 0;

            int min = attacker.GetStat(StatType.DamageMin);
            int max = attacker.GetStat(StatType.DamageMax);
            int weaponRoll = isCrit ? max : random.RangeInclusive(min, max);

            int damage = weaponRoll * skill.DamagePercent / RandomExtensions.PercentScale;
            if (isCrit) damage = damage * _rules.CritMultiplierPercent / RandomExtensions.PercentScale;

            int protection = target.GetStat(StatType.Protection);
            if (protection > _rules.MaxProtection) protection = _rules.MaxProtection;
            if (protection < 0) protection = 0;
            damage = damage * (RandomExtensions.PercentScale - protection) / RandomExtensions.PercentScale;

            return damage < MinimumDamage ? MinimumDamage : damage;
        }
    }
}
