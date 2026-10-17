using System.Collections.Generic;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Combat.Status;
using Darkest.Domain.Core;

namespace Darkest.Domain.Combat
{
    public enum Team
    {
        Heroes,
        Monsters
    }

    public enum DamageOutcome
    {
        None,
        Damaged,
        EnteredDeathsDoor,
        SurvivedDeathblow,
        Killed
    }

    /// <summary>
    /// 전투 참가자의 공통 런타임 상태. 영웅/몬스터의 차이(죽음의 문턱, 스트레스)는 파생 클래스가 덮어쓴다.
    /// </summary>
    public abstract class Combatant
    {
        protected Combatant(string id, string displayName, Team team, StatBlock baseStats,
            IReadOnlyList<SkillDefinition> skills, string portraitKey)
        {
            Id = Guard.NotBlank(id, nameof(id));
            DisplayName = Guard.NotBlank(displayName, nameof(displayName));
            Team = team;
            BaseStats = Guard.NotNull(baseStats, nameof(baseStats));
            Skills = Guard.NotNull(skills, nameof(skills));
            PortraitKey = portraitKey ?? string.Empty;
            CurrentHp = baseStats[StatType.MaxHp];
        }

        public string Id { get; }
        public string DisplayName { get; }
        public Team Team { get; }
        public StatBlock BaseStats { get; }
        public IReadOnlyList<SkillDefinition> Skills { get; }
        public string PortraitKey { get; }

        public StatModifierSet Modifiers { get; } = new StatModifierSet();
        public StatusEffectSet Statuses { get; } = new StatusEffectSet();

        public int CurrentHp { get; protected set; }
        public bool IsDead { get; protected set; }
        public bool IsAlive => !IsDead;
        public int MaxHp => System.Math.Max(1, GetStat(StatType.MaxHp));

        public int GetStat(StatType stat) => BaseStats[stat] + Modifiers.Sum(stat);

        public DamageOutcome ApplyDamage(int amount, IRandom random)
        {
            Guard.NonNegative(amount, nameof(amount));
            if (IsDead || amount == 0) return DamageOutcome.None;

            if (CurrentHp > amount)
            {
                CurrentHp -= amount;
                return DamageOutcome.Damaged;
            }
            return OnLethalDamage(random);
        }

        /// <returns>실제로 회복된 양</returns>
        public int Heal(int amount)
        {
            Guard.NonNegative(amount, nameof(amount));
            if (IsDead) return 0;
            int before = CurrentHp;
            CurrentHp = System.Math.Min(MaxHp, CurrentHp + amount);
            OnHealed();
            return CurrentHp - before;
        }

        /// <summary>세이브 복원 전용. 일반 로직에서는 ApplyDamage/Heal만 쓴다.</summary>
        internal void RestoreHp(int hp) => CurrentHp = System.Math.Max(0, System.Math.Min(MaxHp, hp));

        /// <summary>세이브 복원 전용: 원정 중 사망한 영웅을 사망 상태로 되돌린다.</summary>
        internal void RestoreDead() => Kill();

        protected virtual DamageOutcome OnLethalDamage(IRandom random)
        {
            Kill();
            return DamageOutcome.Killed;
        }

        protected virtual void OnHealed() { }

        protected void Kill()
        {
            CurrentHp = 0;
            IsDead = true;
            Statuses.Clear();
        }

        public override string ToString() => DisplayName + "(" + CurrentHp + "/" + MaxHp + ")";
    }
}
