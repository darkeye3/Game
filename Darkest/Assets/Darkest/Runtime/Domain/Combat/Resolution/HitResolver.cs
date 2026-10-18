using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Core;

namespace Darkest.Domain.Combat.Resolution
{
    public enum HitKind
    {
        Miss,
        Hit,
        Crit
    }

    /// <summary>명중 판정 Strategy. 규칙 변경(예: 빛 수치에 따른 보정)은 새 구현으로 교체한다.</summary>
    public interface IHitResolver
    {
        int GetHitChance(Combatant attacker, Combatant target, SkillDefinition skill);
        HitKind Resolve(Combatant attacker, Combatant target, SkillDefinition skill, IRandom random);
    }

    public sealed class DefaultHitResolver : IHitResolver
    {
        private readonly CombatRules _rules;

        public DefaultHitResolver(CombatRules rules)
        {
            _rules = Guard.NotNull(rules, nameof(rules));
        }

        public int GetHitChance(Combatant attacker, Combatant target, SkillDefinition skill)
        {
            // 아군 대상 스킬(힐/버프)은 항상 명중.
            if (!skill.IsAttack) return RandomExtensions.PercentScale;
            int raw = skill.Accuracy + attacker.GetStat(StatType.Accuracy) - target.GetStat(StatType.Dodge);
            return Clamp(raw, _rules.MinHitChance, _rules.MaxHitChance);
        }

        public HitKind Resolve(Combatant attacker, Combatant target, SkillDefinition skill, IRandom random)
        {
            if (!random.RollPercent(GetHitChance(attacker, target, skill))) return HitKind.Miss;
            int critChance = attacker.GetStat(StatType.CritChance) + skill.CritBonus;
            return random.RollPercent(critChance) ? HitKind.Crit : HitKind.Hit;
        }

        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
    }
}
