using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Status;
using Darkest.Domain.Core;

namespace Darkest.Domain.Combat.Events
{
    // 전투 도메인 이벤트. "일어난 사실"만 담는다. View는 이것만 보고 연출/로그를 만든다.

    public enum BattleResult
    {
        Victory,
        Defeat
    }

    public sealed class BattleStarted : IDomainEvent
    {
        public BattleStarted(Formation heroes, Formation monsters) { Heroes = heroes; Monsters = monsters; }
        public Formation Heroes { get; }
        public Formation Monsters { get; }
        public override string ToString() => "BattleStarted";
    }

    public sealed class RoundStarted : IDomainEvent
    {
        public RoundStarted(int round) { Round = round; }
        public int Round { get; }
        public override string ToString() => "RoundStarted " + Round;
    }

    public sealed class TurnStarted : IDomainEvent
    {
        public TurnStarted(Combatant actor) { Actor = actor; }
        public Combatant Actor { get; }
        public override string ToString() => "TurnStarted " + Actor;
    }

    public enum TurnSkipReason
    {
        Stunned,
        Passed
    }

    public sealed class TurnSkipped : IDomainEvent
    {
        public TurnSkipped(Combatant actor, TurnSkipReason reason) { Actor = actor; Reason = reason; }
        public Combatant Actor { get; }
        public TurnSkipReason Reason { get; }
        public override string ToString() => "TurnSkipped " + Actor + " " + Reason;
    }

    public sealed class SkillUsed : IDomainEvent
    {
        public SkillUsed(Combatant actor, SkillDefinition skill, Combatant primaryTarget)
        {
            Actor = actor; Skill = skill; PrimaryTarget = primaryTarget;
        }
        public Combatant Actor { get; }
        public SkillDefinition Skill { get; }
        public Combatant PrimaryTarget { get; }
        public override string ToString() => "SkillUsed " + Actor + " -> " + Skill + " @ " + PrimaryTarget;
    }

    public sealed class AttackMissed : IDomainEvent
    {
        public AttackMissed(Combatant attacker, Combatant target) { Attacker = attacker; Target = target; }
        public Combatant Attacker { get; }
        public Combatant Target { get; }
        public override string ToString() => "Missed " + Attacker + " -> " + Target;
    }

    public sealed class DamageDealt : IDomainEvent
    {
        public DamageDealt(Combatant source, Combatant target, int amount, bool isCrit, DamageOutcome outcome)
        {
            Source = source; Target = target; Amount = amount; IsCrit = isCrit; Outcome = outcome;
        }
        /// <summary>DoT 피해면 null.</summary>
        public Combatant Source { get; }
        public Combatant Target { get; }
        public int Amount { get; }
        public bool IsCrit { get; }
        public DamageOutcome Outcome { get; }
        public override string ToString() => "Damage " + Target + " -" + Amount + (IsCrit ? " CRIT" : "") + " " + Outcome;
    }

    public sealed class Healed : IDomainEvent
    {
        public Healed(Combatant target, int amount) { Target = target; Amount = amount; }
        public Combatant Target { get; }
        public int Amount { get; }
        public override string ToString() => "Healed " + Target + " +" + Amount;
    }

    public sealed class StatusApplied : IDomainEvent
    {
        public StatusApplied(Combatant target, StatusKind kind) { Target = target; Kind = kind; }
        public Combatant Target { get; }
        public StatusKind Kind { get; }
        public override string ToString() => "StatusApplied " + Target + " " + Kind;
    }

    public sealed class StatusResisted : IDomainEvent
    {
        public StatusResisted(Combatant target, StatusKind kind) { Target = target; Kind = kind; }
        public Combatant Target { get; }
        public StatusKind Kind { get; }
        public override string ToString() => "StatusResisted " + Target + " " + Kind;
    }

    public sealed class CombatantMoved : IDomainEvent
    {
        public CombatantMoved(Combatant combatant, int fromRank, int toRank)
        {
            Combatant = combatant; FromRank = fromRank; ToRank = toRank;
        }
        public Combatant Combatant { get; }
        public int FromRank { get; }
        public int ToRank { get; }
        public override string ToString() => "Moved " + Combatant + " " + FromRank + "->" + ToRank;
    }

    public sealed class CombatantDied : IDomainEvent
    {
        public CombatantDied(Combatant combatant) { Combatant = combatant; }
        public Combatant Combatant { get; }
        public override string ToString() => "Died " + Combatant.DisplayName;
    }

    public sealed class BattleEnded : IDomainEvent
    {
        public BattleEnded(BattleResult result, int rounds) { Result = result; Rounds = rounds; }
        public BattleResult Result { get; }
        public int Rounds { get; }
        public override string ToString() => "BattleEnded " + Result + " after " + Rounds + " rounds";
    }
}
