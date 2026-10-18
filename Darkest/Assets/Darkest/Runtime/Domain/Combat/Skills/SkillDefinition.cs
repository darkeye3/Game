using System.Collections.Generic;
using Darkest.Domain.Core;

namespace Darkest.Domain.Combat.Skills
{
    public enum TargetSide
    {
        Enemy,
        Ally,
        Self
    }

    /// <summary>
    /// 스킬 "데이터"(불변). 실제 효과는 ISkillEffect 목록(Strategy)에 위임하므로
    /// 새 효과를 추가해도 이 클래스나 UseSkillCommand는 수정할 필요가 없다.
    /// </summary>
    public sealed class SkillDefinition
    {
        public SkillDefinition(
            string id,
            string displayName,
            RankMask usableFrom,
            TargetSide side,
            RankMask targetRanks,
            bool hitsAllTargets,
            int accuracy,
            int damagePercent,
            int critBonus,
            int selfMove,
            IReadOnlyList<ISkillEffect> effects)
        {
            Id = Guard.NotBlank(id, nameof(id));
            DisplayName = Guard.NotBlank(displayName, nameof(displayName));
            Guard.That(usableFrom != RankMask.None, id + ": 사용 가능 열이 없습니다.");
            Guard.That(side == TargetSide.Self || targetRanks != RankMask.None, id + ": 타격 열이 없습니다.");
            UsableFrom = usableFrom;
            Side = side;
            TargetRanks = side == TargetSide.Self ? RankMask.All : targetRanks;
            HitsAllTargets = hitsAllTargets;
            Accuracy = accuracy;
            DamagePercent = Guard.NonNegative(damagePercent, nameof(damagePercent));
            CritBonus = critBonus;
            SelfMove = selfMove;
            Effects = effects ?? System.Array.Empty<ISkillEffect>();
        }

        public string Id { get; }
        public string DisplayName { get; }
        public RankMask UsableFrom { get; }
        public TargetSide Side { get; }
        public RankMask TargetRanks { get; }
        public bool HitsAllTargets { get; }

        /// <summary>스킬 기본 명중치(%). 시전자 명중 스탯과 더해진다.</summary>
        public int Accuracy { get; }

        /// <summary>시전자 무기 피해의 몇 %를 줄지. 0이면 피해 없는 스킬(힐/버프).</summary>
        public int DamagePercent { get; }

        public int CritBonus { get; }

        /// <summary>사용 후 시전자 이동(음수=전진, 양수=후퇴).</summary>
        public int SelfMove { get; }

        public IReadOnlyList<ISkillEffect> Effects { get; }

        public bool IsAttack => Side == TargetSide.Enemy;
        public bool DealsDamage => DamagePercent > 0;

        public override string ToString() => DisplayName;
    }
}
