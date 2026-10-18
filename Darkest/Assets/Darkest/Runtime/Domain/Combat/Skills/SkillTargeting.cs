using System.Collections.Generic;
using Darkest.Domain.Combat.Flow;

namespace Darkest.Domain.Combat.Skills
{
    /// <summary>위치 규칙 판정(사용 가능 열 / 유효 대상). UI와 AI가 같은 규칙을 쓰도록 한 곳에 둔다.</summary>
    public static class SkillTargeting
    {
        public static bool CanUseFromCurrentRank(BattleContext context, Combatant actor, SkillDefinition skill)
        {
            int rank = context.FormationOf(actor).RankOf(actor);
            return skill.UsableFrom.Contains(rank);
        }

        public static void CollectValidTargets(BattleContext context, Combatant actor, SkillDefinition skill, List<Combatant> output)
        {
            output.Clear();
            if (skill.Side == TargetSide.Self)
            {
                output.Add(actor);
                return;
            }

            var formation = skill.Side == TargetSide.Enemy ? context.OpponentsOf(actor) : context.FormationOf(actor);
            var members = formation.Members;
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].IsDead) continue;
                if (skill.TargetRanks.Contains(i + 1)) output.Add(members[i]);
            }
        }

        public static bool IsValidTarget(BattleContext context, Combatant actor, SkillDefinition skill, Combatant target)
        {
            if (target == null || target.IsDead) return false;
            if (skill.Side == TargetSide.Self) return target == actor;
            var formation = skill.Side == TargetSide.Enemy ? context.OpponentsOf(actor) : context.FormationOf(actor);
            return skill.TargetRanks.Contains(formation.RankOf(target));
        }

        public static bool HasAnyUsableSkill(BattleContext context, Combatant actor, List<Combatant> scratch)
        {
            for (int i = 0; i < actor.Skills.Count; i++)
            {
                var skill = actor.Skills[i];
                if (!CanUseFromCurrentRank(context, actor, skill)) continue;
                CollectValidTargets(context, actor, skill, scratch);
                if (scratch.Count > 0) return true;
            }
            return false;
        }
    }
}
