using System.Collections.Generic;
using Darkest.Domain.Combat.Events;
using Darkest.Domain.Combat.Flow;
using Darkest.Domain.Combat.Resolution;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Core;

namespace Darkest.Domain.Combat.Commands
{
    /// <summary>
    /// 스킬 사용: 명중 판정 → 피해 → (명중 시) 부가효과 → 시전자 이동.
    /// 효과 자체는 ISkillEffect에 위임하므로 이 클래스는 "순서"만 책임진다.
    /// </summary>
    public sealed class UseSkillCommand : IBattleCommand
    {
        private readonly List<Combatant> _targets = new List<Combatant>(Formation.MaxRanks);

        public UseSkillCommand(Combatant actor, SkillDefinition skill, Combatant target)
        {
            Actor = Guard.NotNull(actor, nameof(actor));
            Skill = Guard.NotNull(skill, nameof(skill));
            Target = target;
        }

        public Combatant Actor { get; }
        public SkillDefinition Skill { get; }
        public Combatant Target { get; }

        public string Validate(BattleContext context)
        {
            if (Actor.IsDead) return "사망한 대상";
            if (!Contains(Actor.Skills, Skill)) return Actor.DisplayName + "의 스킬이 아닙니다: " + Skill.DisplayName;
            if (!SkillTargeting.CanUseFromCurrentRank(context, Actor, Skill)) return "현재 위치에서 쓸 수 없는 스킬입니다.";
            if (!SkillTargeting.IsValidTarget(context, Actor, Skill, Target)) return "유효하지 않은 대상입니다.";
            return null;
        }

        public void Execute(BattleContext context)
        {
            context.Events.Publish(new SkillUsed(Actor, Skill, Target));

            if (Skill.HitsAllTargets) SkillTargeting.CollectValidTargets(context, Actor, Skill, _targets);
            else
            {
                _targets.Clear();
                _targets.Add(Target);
            }

            bool anyCrit = false;
            for (int i = 0; i < _targets.Count; i++) anyCrit |= ResolveOn(context, _targets[i]);

            // 아군 사기 회복은 공격 치명타에만. 회복 스킬 치명타는 회복량 증가(1.5배)로 끝난다.
            if (anyCrit && Actor is Hero && Skill.IsAttack)
                context.Stress.ApplyToAll(context.Heroes.Members, -context.Rules.StressReliefOnHeroCrit);

            if (Skill.SelfMove != 0 && Actor.IsAlive) MoveSelf(context);
        }

        public string Describe() => Actor.DisplayName + " → " + Skill.DisplayName + " → " + (Target?.DisplayName ?? "-");

        /// <returns>치명타 여부</returns>
        private bool ResolveOn(BattleContext context, Combatant target)
        {
            if (target.IsDead) return false;

            var hit = context.HitResolver.Resolve(Actor, target, Skill, context.Random);
            if (hit == HitKind.Miss)
            {
                context.Events.Publish(new AttackMissed(Actor, target));
                return false;
            }

            bool isCrit = hit == HitKind.Crit;
            if (Skill.DealsDamage && Skill.IsAttack)
            {
                int damage = context.DamageCalculator.Calculate(Actor, target, Skill, isCrit, context.Random);
                var outcome = target.ApplyDamage(damage, context.Random);
                context.Events.Publish(new DamageDealt(Actor, target, damage, isCrit, outcome));
                DamageReactions.Apply(context, target, outcome, isCrit);
            }

            if (target.IsDead) return isCrit;

            var effectContext = new SkillEffectContext(context, Actor, target, isCrit);
            for (int i = 0; i < Skill.Effects.Count; i++) Skill.Effects[i].Apply(effectContext);
            return isCrit;
        }

        private void MoveSelf(BattleContext context)
        {
            var formation = context.FormationOf(Actor);
            int from = formation.RankOf(Actor);
            int moved = formation.Move(Actor, Skill.SelfMove);
            if (moved != 0) context.Events.Publish(new CombatantMoved(Actor, from, from + moved));
        }

        private static bool Contains(IReadOnlyList<SkillDefinition> skills, SkillDefinition skill)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] == skill) return true;
            }
            return false;
        }
    }
}
