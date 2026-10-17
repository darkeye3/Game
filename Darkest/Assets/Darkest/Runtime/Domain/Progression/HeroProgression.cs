using System;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;

namespace Darkest.Domain.Progression
{
    public sealed class HeroLeveledUp : IDomainEvent
    {
        public HeroLeveledUp(Hero hero, int level) { Hero = hero; Level = level; }
        public Hero Hero { get; }
        public int Level { get; }
        public override string ToString() => "LevelUp " + Hero.DisplayName + " → " + Level;
    }

    /// <summary>
    /// 경험치 → 레벨 → 스탯 보정. 레벨 보너스는 "level" 출처 StatModifier 하나로 붙여서,
    /// 레벨이 바뀔 때마다 떼고 다시 붙이면 되게 한다(중복 적용 방지, 세이브 복원도 같은 경로).
    /// </summary>
    public sealed class HeroProgression
    {
        public const string ModifierSource = "level";

        private readonly ProgressionRules _rules;
        private readonly IEventBus _events;

        public HeroProgression(ProgressionRules rules, IEventBus events)
        {
            _rules = Guard.NotNull(rules, nameof(rules));
            _events = Guard.NotNull(events, nameof(events));
            Guard.That(rules.XpThresholds != null && rules.XpThresholds.Length >= 1 && rules.XpThresholds[0] == 0,
                "XpThresholds는 0으로 시작해야 합니다.");
            for (int i = 1; i < rules.XpThresholds.Length; i++)
                Guard.That(rules.XpThresholds[i] > rules.XpThresholds[i - 1], "XpThresholds는 증가해야 합니다.");
        }

        public ProgressionRules Rules => _rules;

        /// <summary>원정 결과에 따른 경험치(생존자 1인당).</summary>
        public int ExpeditionXp(Expedition expedition)
        {
            Guard.NotNull(expedition, nameof(expedition));
            int xp = expedition.BattlesWon * _rules.XpPerBattleWon;
            if (expedition.Phase == ExpeditionPhase.Completed) xp += _rules.XpOnExpeditionCompleted;
            else if (expedition.Phase == ExpeditionPhase.Retreated) xp += _rules.XpOnExpeditionRetreated;
            return xp;
        }

        public void Grant(Hero hero, int xp)
        {
            Guard.NotNull(hero, nameof(hero));
            Guard.NonNegative(xp, nameof(xp));
            if (hero.IsDead || xp == 0) return;

            int before = hero.Level;
            Apply(hero, hero.Experience + xp);
            for (int level = before + 1; level <= hero.Level; level++) _events.Publish(new HeroLeveledUp(hero, level));
        }

        /// <summary>세이브 복원: 이벤트 없이 경험치만 맞춘다.</summary>
        public void Restore(Hero hero, int experience) => Apply(Guard.NotNull(hero, nameof(hero)), Math.Max(0, experience));

        /// <summary>다음 레벨까지 남은 경험치. 최대 레벨이면 0.</summary>
        public int XpToNextLevel(Hero hero) =>
            hero.Level >= _rules.MaxLevel ? 0 : _rules.XpThresholds[hero.Level + 1] - hero.Experience;

        public int LevelFor(int experience)
        {
            int level = 0;
            while (level < _rules.MaxLevel && experience >= _rules.XpThresholds[level + 1]) level++;
            return level;
        }

        private void Apply(Hero hero, int experience)
        {
            int level = LevelFor(experience);
            hero.SetProgress(experience, level);

            hero.Modifiers.RemoveBySource(ModifierSource);
            if (level == 0) return;
            AddBonus(hero, StatType.MaxHp, _rules.MaxHpPerLevel * level);
            AddBonus(hero, StatType.Accuracy, _rules.AccuracyPerLevel * level);
            AddBonus(hero, StatType.DamageMin, _rules.DamagePerLevel * level);
            AddBonus(hero, StatType.DamageMax, _rules.DamagePerLevel * level);
            AddBonus(hero, StatType.Dodge, _rules.DodgePerLevel * level);
        }

        private static void AddBonus(Hero hero, StatType stat, int amount)
        {
            if (amount != 0) hero.Modifiers.Add(new StatModifier(stat, amount, ModifierSource));
        }
    }
}
