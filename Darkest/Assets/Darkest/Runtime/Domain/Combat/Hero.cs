using System.Collections.Generic;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Core;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Combat
{
    /// <summary>
    /// 영웅 = 전투원 + 스트레스 + 죽음의 문턱(Death's Door).
    /// HP가 0이 되면 바로 죽지 않고 문턱 상태가 되며, 그 상태에서 피격 시 Deathblow 저항 판정을 한다.
    /// </summary>
    public sealed class Hero : Combatant
    {
        public Hero(string id, string displayName, string classId, StatBlock baseStats,
            IReadOnlyList<SkillDefinition> skills, string portraitKey, int maxStress)
            : base(id, displayName, Team.Heroes, baseStats, skills, portraitKey)
        {
            ClassId = Guard.NotBlank(classId, nameof(classId));
            Stress = new StressMeter(maxStress);
        }

        public string ClassId { get; }
        public StressMeter Stress { get; }
        public bool IsAtDeathsDoor { get; private set; }

        /// <summary>스트레스 최대치 도달(심장마비). 문턱이 아니면 문턱으로, 이미 문턱이면 사망.</summary>
        public DamageOutcome SufferHeartAttack()
        {
            if (IsDead) return DamageOutcome.None;
            if (IsAtDeathsDoor)
            {
                Kill();
                return DamageOutcome.Killed;
            }
            CurrentHp = 0;
            IsAtDeathsDoor = true;
            return DamageOutcome.EnteredDeathsDoor;
        }

        protected override DamageOutcome OnLethalDamage(IRandom random)
        {
            if (!IsAtDeathsDoor)
            {
                CurrentHp = 0;
                IsAtDeathsDoor = true;
                return DamageOutcome.EnteredDeathsDoor;
            }

            if (random.RollPercent(GetStat(StatType.DeathblowResist))) return DamageOutcome.SurvivedDeathblow;

            Kill();
            return DamageOutcome.Killed;
        }

        protected override void OnHealed()
        {
            if (CurrentHp > 0) IsAtDeathsDoor = false;
        }
    }
}
