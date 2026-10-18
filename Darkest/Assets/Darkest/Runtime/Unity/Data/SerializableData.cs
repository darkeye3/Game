using System;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Items;
using UnityEngine;

namespace Darkest.Unity.Data
{
    /// <summary>인스펙터 편집용 스탯. 도메인 StatBlock(불변)으로 변환해서 쓴다.</summary>
    [Serializable]
    public sealed class StatBlockData
    {
        [SerializeField] private int maxHp = 20;
        [SerializeField] private int speed;
        [SerializeField] private int accuracy;
        [SerializeField] private int critChance;
        [SerializeField] private int dodge;
        [SerializeField] private int protection;
        [SerializeField] private int damageMin = 3;
        [SerializeField] private int damageMax = 6;
        [SerializeField] private int stunResist;
        [SerializeField] private int bleedResist;
        [SerializeField] private int blightResist;
        [SerializeField] private int deathblowResist;

        public StatBlock ToStatBlock() => StatBlock.Create()
            .Hp(maxHp).Speed(speed).Accuracy(accuracy).Crit(critChance).Dodge(dodge).Protection(protection)
            .Damage(damageMin, damageMax).Resist(stunResist, bleedResist, blightResist, deathblowResist).Build();

        internal static StatBlockData From(StatBlock stats) => new StatBlockData
        {
            maxHp = stats[StatType.MaxHp], speed = stats[StatType.Speed], accuracy = stats[StatType.Accuracy],
            critChance = stats[StatType.CritChance], dodge = stats[StatType.Dodge], protection = stats[StatType.Protection],
            damageMin = stats[StatType.DamageMin], damageMax = stats[StatType.DamageMax],
            stunResist = stats[StatType.StunResist], bleedResist = stats[StatType.BleedResist],
            blightResist = stats[StatType.BlightResist], deathblowResist = stats[StatType.DeathblowResist]
        };
    }

    [Serializable]
    public sealed class SkillEffectData
    {
        [SerializeField] private SkillEffectKind kind;
        [SerializeField] private int amount;
        [SerializeField] private int amountMax;
        [SerializeField] private int duration;
        [SerializeField, Range(0, 200)] private int chance = 100;

        public SkillEffectSpec ToSpec() => new SkillEffectSpec(kind, amount, amountMax, duration, chance);

        internal static SkillEffectData Create(SkillEffectKind kind, int amount, int amountMax, int duration, int chance) =>
            new SkillEffectData { kind = kind, amount = amount, amountMax = amountMax, duration = duration, chance = chance };
    }

    [Serializable]
    public sealed class StatModifierData
    {
        [SerializeField] private StatType stat;
        [SerializeField] private int amount;

        public StatModifier ToModifier(string sourceId) => new StatModifier(stat, amount, sourceId);
        public StatBonus ToBonus() => new StatBonus(stat, amount);

        internal static StatModifierData Create(StatType stat, int amount) => new StatModifierData { stat = stat, amount = amount };
    }

    [Serializable]
    public sealed class CurioOutcomeData
    {
        [SerializeField, Min(1)] private int weight = 1;
        [SerializeField] private CurioEffectKind kind;
        [SerializeField] private int amount;
        [SerializeField] private string description;

        public CurioOutcome ToOutcome() => new CurioOutcome(weight, kind, amount, description);

        internal static CurioOutcomeData From(CurioOutcome outcome) => new CurioOutcomeData
        {
            weight = outcome.Weight, kind = outcome.Kind, amount = outcome.Amount, description = outcome.Description
        };
    }
}
