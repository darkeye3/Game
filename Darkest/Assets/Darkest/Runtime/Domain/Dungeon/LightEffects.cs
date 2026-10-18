using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Core;

namespace Darkest.Domain.Dungeon
{
    /// <summary>
    /// 빛 단계에 따른 보정: 어두울수록 몬스터가 강해지고 전리품이 늘어난다(위험 ↔ 보상 트레이드오프).
    /// 몬스터 보정은 StatModifier(출처 "light")로 붙여 디버그 시 어디서 온 수치인지 추적할 수 있다.
    /// </summary>
    public sealed class LightEffects
    {
        public const string ModifierSource = "light";

        private readonly ExpeditionRules _rules;

        public LightEffects(ExpeditionRules rules)
        {
            _rules = Guard.NotNull(rules, nameof(rules));
            Guard.That(rules.MonsterAccuracyBonusByLight.Length == LightLevelCount
                       && rules.MonsterCritBonusByLight.Length == LightLevelCount
                       && rules.LootBonusPercentByLight.Length == LightLevelCount, "빛 보정 배열은 4단계 값이 필요합니다.");
        }

        private static int LightLevelCount => (int)LightLevel.Dark + 1;

        public void ApplyToMonsters(Formation monsters, LightLevel level)
        {
            int accuracy = _rules.MonsterAccuracyBonusByLight[(int)level];
            int crit = _rules.MonsterCritBonusByLight[(int)level];
            for (int i = 0; i < monsters.Members.Count; i++)
            {
                var monster = monsters.Members[i];
                monster.Modifiers.RemoveBySource(ModifierSource);
                if (accuracy != 0) monster.Modifiers.Add(new StatModifier(StatType.Accuracy, accuracy, ModifierSource));
                if (crit != 0) monster.Modifiers.Add(new StatModifier(StatType.CritChance, crit, ModifierSource));
            }
        }

        public int ScaleLoot(int gold, LightLevel level) =>
            gold + gold * _rules.LootBonusPercentByLight[(int)level] / RandomExtensions.PercentScale;
    }
}
