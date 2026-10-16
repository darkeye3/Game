using Darkest.Domain.Combat;
using Darkest.Domain.Core;

namespace Darkest.Domain.Stress
{
    public sealed class StressChanged : IDomainEvent
    {
        public StressChanged(Hero hero, int delta) { Hero = hero; Delta = delta; }
        public Hero Hero { get; }
        public int Delta { get; }
        public override string ToString() => "Stress " + Hero.DisplayName + " " + (Delta >= 0 ? "+" : "") + Delta + " => " + Hero.Stress.Current;
    }

    public sealed class ResolveTriggered : IDomainEvent
    {
        public ResolveTriggered(Hero hero, ResolveDefinition resolve) { Hero = hero; Resolve = resolve; }
        public Hero Hero { get; }
        public ResolveDefinition Resolve { get; }
        public override string ToString() => "Resolve " + Hero.DisplayName + " => " + Resolve.DisplayName + (Resolve.IsVirtue ? " (미덕)" : " (고통)");
    }

    public sealed class ResolveActed : IDomainEvent
    {
        public ResolveActed(Hero hero, ResolveDefinition resolve) { Hero = hero; Resolve = resolve; }
        public Hero Hero { get; }
        public ResolveDefinition Resolve { get; }
        public override string ToString() => "ResolveActed " + Hero.DisplayName + " " + Resolve.DisplayName;
    }

    public sealed class HeartAttackSuffered : IDomainEvent
    {
        public HeartAttackSuffered(Hero hero, DamageOutcome outcome) { Hero = hero; Outcome = outcome; }
        public Hero Hero { get; }
        public DamageOutcome Outcome { get; }
        public override string ToString() => "HeartAttack " + Hero.DisplayName + " " + Outcome;
    }
}
