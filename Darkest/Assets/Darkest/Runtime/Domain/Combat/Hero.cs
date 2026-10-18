using System.Collections.Generic;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Core;
using Darkest.Domain.Items;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Combat
{
    /// <summary>
    /// 영웅 = 전투원 + 스트레스 + 죽음의 문턱(Death's Door).
    /// HP가 0이 되면 바로 죽지 않고 문턱 상태가 되며, 그 상태에서 피격 시 Deathblow 저항 판정을 한다.
    /// </summary>
    public sealed class Hero : Combatant
    {
        /// <summary>장신구 슬롯 수(다키스트 던전과 같은 2칸). UI가 슬롯을 고정 개수로 그린다.</summary>
        public const int TrinketSlots = 2;

        private readonly TrinketDefinition[] _trinkets = new TrinketDefinition[TrinketSlots];

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

        /// <summary>누적 경험치와 레벨. HeroProgression만 바꾼다.</summary>
        public int Experience { get; private set; }
        public int Level { get; private set; }

        /// <returns>슬롯의 장신구. 비었으면 null.</returns>
        public TrinketDefinition GetTrinket(int slot) => _trinkets[slot];

        public static string TrinketSourceId(int slot) => "trinket#" + slot;

        /// <summary>슬롯 교체: 이전 장신구 보정을 떼고 새 보정을 붙인다. 최대 HP가 줄면 현재 HP도 맞춰 깎는다.</summary>
        internal void SetTrinket(int slot, TrinketDefinition trinket)
        {
            string source = TrinketSourceId(slot);
            Modifiers.RemoveBySource(source);
            _trinkets[slot] = trinket;
            if (trinket != null)
            {
                for (int i = 0; i < trinket.Bonuses.Count; i++) Modifiers.Add(trinket.Bonuses[i].WithSource(source));
            }
            RestoreHp(CurrentHp);
        }

        internal void SetProgress(int experience, int level)
        {
            Experience = experience;
            Level = level;
        }

        /// <summary>세이브 복원 전용. HP 0인 생존 영웅만 죽음의 문턱으로 되돌린다.</summary>
        internal void RestoreDeathsDoor()
        {
            if (!IsDead && CurrentHp == 0) IsAtDeathsDoor = true;
        }

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
