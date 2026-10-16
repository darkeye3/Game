using System.Collections.Generic;
using Darkest.Domain.Combat.Events;
using Darkest.Domain.Core;

namespace Darkest.Domain.Combat.Flow
{
    public enum BattlePhase
    {
        NotStarted,
        RoundStart,
        TurnStart,
        AwaitingHeroCommand,
        EnemyTurn,
        CheckEnd,
        Victory,
        Defeat
    }

    /// <summary>
    /// 전투 흐름 State 패턴. 상태 객체는 데이터 없이 BattleContext만 읽고 쓴다(무상태 → 1회 생성 후 재사용).
    /// Next()가 다음 상태를 돌려주며, 입력 대기/종료 상태에서 루프가 멈춘다.
    /// </summary>
    internal abstract class BattleState
    {
        public abstract BattlePhase Phase { get; }
        public virtual bool IsWaiting => false;
        public abstract BattleState Next(BattleContext context, BattleStateSet states);
    }

    /// <summary>상태 인스턴스 보관함. new를 반복하지 않기 위함.</summary>
    internal sealed class BattleStateSet
    {
        public readonly BattleState RoundStart = new RoundStartState();
        public readonly BattleState TurnStart = new TurnStartState();
        public readonly BattleState AwaitingHero = new AwaitingHeroState();
        public readonly BattleState EnemyTurn = new EnemyTurnState();
        public readonly BattleState CheckEnd = new CheckEndState();
        public readonly BattleState Victory = new EndState(BattlePhase.Victory);
        public readonly BattleState Defeat = new EndState(BattlePhase.Defeat);
    }

    internal sealed class RoundStartState : BattleState
    {
        public override BattlePhase Phase => BattlePhase.RoundStart;

        public override BattleState Next(BattleContext context, BattleStateSet states)
        {
            if (context.Round >= context.Rules.MaxRounds)
            {
                context.Logger.Error("Battle", "최대 라운드 초과 — 무한 루프 방지를 위해 패배 처리합니다.");
                context.Events.Publish(new BattleEnded(BattleResult.Defeat, context.Round));
                return states.Defeat;
            }

            context.Round++;
            context.TurnOrder.BuildRound(context, context.TurnQueue);
            context.Events.Publish(new RoundStarted(context.Round));
            return states.TurnStart;
        }
    }

    internal sealed class TurnStartState : BattleState
    {
        public override BattlePhase Phase => BattlePhase.TurnStart;

        public override BattleState Next(BattleContext context, BattleStateSet states)
        {
            var actor = DequeueLiving(context.TurnQueue);
            if (actor == null) return states.RoundStart;

            context.CurrentActor = actor;
            context.Events.Publish(new TurnStarted(actor));

            if (TickDamageOverTime(context, actor)) return states.CheckEnd;

            if (actor.Statuses.TryConsumeStun())
            {
                context.Events.Publish(new TurnSkipped(actor, TurnSkipReason.Stunned));
                return states.CheckEnd;
            }

            if (actor is Hero hero)
            {
                context.Stress.OnTurnStart(hero, context.Heroes.Members);
                return hero.IsDead ? states.CheckEnd : states.AwaitingHero;
            }
            return states.EnemyTurn;
        }

        private static Combatant DequeueLiving(Queue<Combatant> queue)
        {
            while (queue.Count > 0)
            {
                var next = queue.Dequeue();
                if (next.IsAlive) return next;
            }
            return null;
        }

        /// <returns>DoT로 사망했으면 true</returns>
        private static bool TickDamageOverTime(BattleContext context, Combatant actor)
        {
            int damage = actor.Statuses.TickDamageOverTime();
            if (damage <= 0) return false;

            var outcome = actor.ApplyDamage(damage, context.Random);
            context.Events.Publish(new DamageDealt(null, actor, damage, false, outcome));
            DamageReactions.Apply(context, actor, outcome, false);
            return actor.IsDead;
        }
    }

    internal sealed class AwaitingHeroState : BattleState
    {
        public override BattlePhase Phase => BattlePhase.AwaitingHeroCommand;
        public override bool IsWaiting => true;

        public override BattleState Next(BattleContext context, BattleStateSet states)
        {
            throw new System.InvalidOperationException("영웅 명령 대기 중에는 BattleSession.Submit()으로만 진행합니다.");
        }
    }

    internal sealed class EnemyTurnState : BattleState
    {
        public override BattlePhase Phase => BattlePhase.EnemyTurn;

        public override BattleState Next(BattleContext context, BattleStateSet states)
        {
            var actor = context.CurrentActor;
            var command = context.EnemyAI.Decide(context, actor);
            string error = command == null ? "AI가 명령을 만들지 못했습니다." : command.Validate(context);
            if (error != null)
            {
                // 몬스터 AI 오류로 게임이 멈추지 않도록: 에러 로그 + 대기(fallback).
                context.Logger.Error("Battle", actor.DisplayName + " AI 명령 무효: " + error);
                command = new Commands.PassCommand(actor);
            }

            command.Execute(context);
            context.Record(command);
            return states.CheckEnd;
        }
    }

    internal sealed class CheckEndState : BattleState
    {
        private readonly List<Combatant> _removed = new List<Combatant>(Formation.MaxRanks);

        public override BattlePhase Phase => BattlePhase.CheckEnd;

        public override BattleState Next(BattleContext context, BattleStateSet states)
        {
            RemoveDead(context, context.Monsters);
            RemoveDead(context, context.Heroes);

            if (context.Heroes.IsEmpty)
            {
                context.Events.Publish(new BattleEnded(BattleResult.Defeat, context.Round));
                return states.Defeat;
            }
            if (context.Monsters.IsEmpty)
            {
                context.Events.Publish(new BattleEnded(BattleResult.Victory, context.Round));
                return states.Victory;
            }
            return states.TurnStart;
        }

        private void RemoveDead(BattleContext context, Formation formation)
        {
            _removed.Clear();
            formation.RemoveDead(_removed);
            for (int i = 0; i < _removed.Count; i++)
            {
                context.Events.Publish(new CombatantDied(_removed[i]));
                if (_removed[i] is Hero) context.Stress.ApplyToAll(context.Heroes.Members, context.Rules.StressOnAllyDeath);
            }
        }
    }

    internal sealed class EndState : BattleState
    {
        private readonly BattlePhase _phase;

        public EndState(BattlePhase phase)
        {
            _phase = phase;
        }

        public override BattlePhase Phase => _phase;
        public override bool IsWaiting => true;

        public override BattleState Next(BattleContext context, BattleStateSet states) => this;
    }
}
