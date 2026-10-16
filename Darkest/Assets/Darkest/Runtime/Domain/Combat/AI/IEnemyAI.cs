using System.Collections.Generic;
using Darkest.Domain.Combat.Commands;
using Darkest.Domain.Combat.Flow;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Core;

namespace Darkest.Domain.Combat.AI
{
    /// <summary>몬스터 행동 결정 Strategy. 보스 전용 AI 등은 이 인터페이스를 새로 구현한다.</summary>
    public interface IEnemyAI
    {
        IBattleCommand Decide(BattleContext context, Combatant actor);
    }

    /// <summary>쓸 수 있는 스킬 중 무작위, 대상도 무작위. 쓸 게 없으면 앞으로 이동, 그것도 안 되면 대기.</summary>
    public sealed class RandomEnemyAI : IEnemyAI
    {
        private readonly List<SkillDefinition> _usable = new List<SkillDefinition>();
        private readonly List<Combatant> _targets = new List<Combatant>(Formation.MaxRanks);

        public IBattleCommand Decide(BattleContext context, Combatant actor)
        {
            _usable.Clear();
            for (int i = 0; i < actor.Skills.Count; i++)
            {
                var skill = actor.Skills[i];
                if (!SkillTargeting.CanUseFromCurrentRank(context, actor, skill)) continue;
                SkillTargeting.CollectValidTargets(context, actor, skill, _targets);
                if (_targets.Count > 0) _usable.Add(skill);
            }

            if (_usable.Count == 0)
            {
                int rank = context.FormationOf(actor).RankOf(actor);
                return rank > 1 ? (IBattleCommand)new MoveCommand(actor, -1) : new PassCommand(actor);
            }

            var chosen = context.Random.Pick(_usable);
            SkillTargeting.CollectValidTargets(context, actor, chosen, _targets);
            return new UseSkillCommand(actor, chosen, context.Random.Pick(_targets));
        }
    }
}
