using System;
using Darkest.Domain.Combat.Status;

namespace Darkest.Domain.Combat.Skills
{
    public enum SkillEffectKind
    {
        Bleed,
        Blight,
        Stun,
        Heal,
        Stress,
        MoveTarget
    }

    /// <summary>
    /// 직렬화 가능한 효과 명세. ScriptableObject/세이브는 인터페이스를 직렬화할 수 없으므로
    /// "종류 + 수치"만 저장하고 런타임 객체는 팩토리가 만든다.
    /// </summary>
    public readonly struct SkillEffectSpec
    {
        public SkillEffectSpec(SkillEffectKind kind, int amount, int amountMax = 0, int duration = 0, int chance = 100)
        {
            Kind = kind;
            Amount = amount;
            AmountMax = amountMax;
            Duration = duration;
            Chance = chance;
        }

        public SkillEffectKind Kind { get; }
        public int Amount { get; }
        public int AmountMax { get; }
        public int Duration { get; }
        public int Chance { get; }
    }

    public static class SkillEffectFactory
    {
        public static ISkillEffect Create(SkillEffectSpec spec)
        {
            switch (spec.Kind)
            {
                case SkillEffectKind.Bleed: return new DamageOverTimeEffect(StatusKind.Bleed, spec.Amount, spec.Duration, spec.Chance);
                case SkillEffectKind.Blight: return new DamageOverTimeEffect(StatusKind.Blight, spec.Amount, spec.Duration, spec.Chance);
                case SkillEffectKind.Stun: return new StunEffect(spec.Chance);
                case SkillEffectKind.Heal: return new HealEffect(spec.Amount, Math.Max(spec.Amount, spec.AmountMax));
                case SkillEffectKind.Stress: return new StressEffect(spec.Amount);
                case SkillEffectKind.MoveTarget: return new MoveTargetEffect(spec.Amount);
                default: throw new ArgumentOutOfRangeException(nameof(spec), spec.Kind, "알 수 없는 효과 종류");
            }
        }

        public static ISkillEffect[] CreateAll(params SkillEffectSpec[] specs)
        {
            var effects = new ISkillEffect[specs.Length];
            for (int i = 0; i < specs.Length; i++) effects[i] = Create(specs[i]);
            return effects;
        }
    }
}
