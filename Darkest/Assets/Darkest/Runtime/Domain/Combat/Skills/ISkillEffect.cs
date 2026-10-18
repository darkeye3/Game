using Darkest.Domain.Combat.Flow;

namespace Darkest.Domain.Combat.Skills
{
    /// <summary>
    /// 스킬 부가효과 Strategy. "명중했을 때 대상에게 무엇을 하는가"만 책임진다.
    /// 명중/피해 판정은 UseSkillCommand가 먼저 처리하고, 명중 시에만 효과가 호출된다.
    /// </summary>
    public interface ISkillEffect
    {
        void Apply(in SkillEffectContext context);

        /// <summary>UI 툴팁/디버그용 한 줄 설명.</summary>
        string Describe();

        /// <summary>직렬화 가능한 명세로 되돌린다(에셋 내보내기/세이브용). SkillEffectFactory.Create의 역함수.</summary>
        SkillEffectSpec ToSpec();
    }

    public readonly struct SkillEffectContext
    {
        public SkillEffectContext(BattleContext battle, Combatant source, Combatant target, bool isCrit)
        {
            Battle = battle;
            Source = source;
            Target = target;
            IsCrit = isCrit;
        }

        public BattleContext Battle { get; }
        public Combatant Source { get; }
        public Combatant Target { get; }
        public bool IsCrit { get; }
    }
}
