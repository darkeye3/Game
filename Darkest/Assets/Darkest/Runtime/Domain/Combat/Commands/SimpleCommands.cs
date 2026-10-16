using Darkest.Domain.Combat.Events;
using Darkest.Domain.Combat.Flow;
using Darkest.Domain.Core;

namespace Darkest.Domain.Combat.Commands
{
    /// <summary>아무것도 하지 않고 턴을 넘긴다. AI가 쓸 스킬이 없을 때의 안전한 fallback이기도 하다.</summary>
    public sealed class PassCommand : IBattleCommand
    {
        public PassCommand(Combatant actor)
        {
            Actor = Guard.NotNull(actor, nameof(actor));
        }

        public Combatant Actor { get; }

        public string Validate(BattleContext context) => Actor.IsDead ? "사망한 대상" : null;

        public void Execute(BattleContext context) => context.Events.Publish(new TurnSkipped(Actor, TurnSkipReason.Passed));

        public string Describe() => Actor.DisplayName + " 대기";
    }

    /// <summary>턴을 소모해 한 칸 이동(음수=전진, 양수=후퇴).</summary>
    public sealed class MoveCommand : IBattleCommand
    {
        public MoveCommand(Combatant actor, int delta)
        {
            Actor = Guard.NotNull(actor, nameof(actor));
            Guard.That(delta != 0, "이동량 0");
            Delta = delta;
        }

        public Combatant Actor { get; }
        public int Delta { get; }

        public string Validate(BattleContext context)
        {
            if (Actor.IsDead) return "사망한 대상";
            var formation = context.FormationOf(Actor);
            int target = formation.RankOf(Actor) + Delta;
            if (target < 1 || target > formation.Count) return "그 방향으로 이동할 수 없습니다.";
            return null;
        }

        public void Execute(BattleContext context)
        {
            var formation = context.FormationOf(Actor);
            int from = formation.RankOf(Actor);
            int moved = formation.Move(Actor, Delta);
            if (moved != 0) context.Events.Publish(new CombatantMoved(Actor, from, from + moved));
        }

        public string Describe() => Actor.DisplayName + (Delta < 0 ? " 전진" : " 후퇴");
    }
}
