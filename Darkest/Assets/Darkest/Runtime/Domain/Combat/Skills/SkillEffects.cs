using Darkest.Domain.Combat.Events;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Combat.Status;
using Darkest.Domain.Core;

namespace Darkest.Domain.Combat.Skills
{
    /// <summary>출혈/역병 부여. 적용 확률 = 기본 확률 - 대상 저항.</summary>
    public sealed class DamageOverTimeEffect : ISkillEffect
    {
        private readonly StatusKind _kind;
        private readonly int _damagePerTurn;
        private readonly int _durationTurns;
        private readonly int _baseChance;

        public DamageOverTimeEffect(StatusKind kind, int damagePerTurn, int durationTurns, int baseChance)
        {
            Guard.That(kind != StatusKind.Stun, "기절은 StunEffect를 사용하세요.");
            _kind = kind;
            _damagePerTurn = Guard.NonNegative(damagePerTurn, nameof(damagePerTurn));
            _durationTurns = Guard.InRange(durationTurns, 1, 99, nameof(durationTurns));
            _baseChance = baseChance;
        }

        public void Apply(in SkillEffectContext context)
        {
            var resistStat = _kind == StatusKind.Bleed ? StatType.BleedResist : StatType.BlightResist;
            int chance = _baseChance - context.Target.GetStat(resistStat);
            if (!context.Battle.Random.RollPercent(chance))
            {
                context.Battle.Events.Publish(new StatusResisted(context.Target, _kind));
                return;
            }
            context.Target.Statuses.Add(new StatusEffect(_kind, _damagePerTurn, _durationTurns));
            context.Battle.Events.Publish(new StatusApplied(context.Target, _kind));
        }

        public string Describe() => _kind + " " + _damagePerTurn + "x" + _durationTurns + " (" + _baseChance + "%)";

        public SkillEffectSpec ToSpec() => new SkillEffectSpec(_kind == StatusKind.Bleed ? SkillEffectKind.Bleed : SkillEffectKind.Blight, _damagePerTurn, 0, _durationTurns, _baseChance);
    }

    public sealed class StunEffect : ISkillEffect
    {
        private readonly int _baseChance;

        public StunEffect(int baseChance)
        {
            _baseChance = baseChance;
        }

        public void Apply(in SkillEffectContext context)
        {
            int chance = _baseChance - context.Target.GetStat(StatType.StunResist);
            if (!context.Battle.Random.RollPercent(chance))
            {
                context.Battle.Events.Publish(new StatusResisted(context.Target, StatusKind.Stun));
                return;
            }
            context.Target.Statuses.Add(new StatusEffect(StatusKind.Stun, 0, 1));
            context.Battle.Events.Publish(new StatusApplied(context.Target, StatusKind.Stun));
        }

        public string Describe() => "기절 (" + _baseChance + "%)";

        public SkillEffectSpec ToSpec() => new SkillEffectSpec(SkillEffectKind.Stun, 0, chance: _baseChance);
    }

    public sealed class HealEffect : ISkillEffect
    {
        private readonly int _min;
        private readonly int _max;

        public HealEffect(int min, int max)
        {
            Guard.That(max >= min && min >= 0, "회복량 범위 오류");
            _min = min;
            _max = max;
        }

        public void Apply(in SkillEffectContext context)
        {
            int amount = context.Battle.Random.RangeInclusive(_min, _max);
            if (context.IsCrit) amount = amount * context.Battle.Rules.CritMultiplierPercent / RandomExtensions.PercentScale;
            int healed = context.Target.Heal(amount);
            context.Battle.Events.Publish(new Healed(context.Target, healed));
        }

        public string Describe() => "회복 " + _min + "~" + _max;

        public SkillEffectSpec ToSpec() => new SkillEffectSpec(SkillEffectKind.Heal, _min, _max);
    }

    /// <summary>스트레스 가감. amount 양수 = 스트레스 피해, 음수 = 스트레스 회복. 영웅에게만 의미가 있다.</summary>
    public sealed class StressEffect : ISkillEffect
    {
        private readonly int _amount;

        public StressEffect(int amount)
        {
            _amount = amount;
        }

        public void Apply(in SkillEffectContext context)
        {
            if (!(context.Target is Hero hero)) return;
            context.Battle.Stress.Apply(hero, _amount);
        }

        public string Describe() => _amount >= 0 ? "스트레스 +" + _amount : "스트레스 " + _amount;

        public SkillEffectSpec ToSpec() => new SkillEffectSpec(SkillEffectKind.Stress, _amount);
    }

    /// <summary>대상 밀치기(양수)/당기기(음수).</summary>
    public sealed class MoveTargetEffect : ISkillEffect
    {
        private readonly int _delta;

        public MoveTargetEffect(int delta)
        {
            _delta = delta;
        }

        public void Apply(in SkillEffectContext context)
        {
            var formation = context.Battle.FormationOf(context.Target);
            int from = formation.RankOf(context.Target);
            int moved = formation.Move(context.Target, _delta);
            if (moved != 0) context.Battle.Events.Publish(new CombatantMoved(context.Target, from, from + moved));
        }

        public string Describe() => _delta > 0 ? "밀치기 " + _delta : "당기기 " + (-_delta);

        public SkillEffectSpec ToSpec() => new SkillEffectSpec(SkillEffectKind.MoveTarget, _delta);
    }
}
