namespace Darkest.Domain.Combat.Flow
{
    /// <summary>피해 이후의 2차 반응(스트레스). 스킬 피해와 DoT 피해가 같은 규칙을 공유한다.</summary>
    internal static class DamageReactions
    {
        public static void Apply(BattleContext context, Combatant target, DamageOutcome outcome, bool isCrit)
        {
            if (!(target is Hero hero) || hero.IsDead) return;
            if (isCrit) context.Stress.Apply(hero, context.Rules.StressOnCritReceived);
            if (outcome == DamageOutcome.EnteredDeathsDoor) context.Stress.Apply(hero, context.Rules.StressOnDeathsDoor);
        }
    }
}
