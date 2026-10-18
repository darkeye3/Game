using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Dungeon
{
    /// <summary>
    /// 야영 1회의 상태: 남은 야영 포인트와 이미 쓴 (영웅, 스킬) 쌍. 야영을 하면 생기고 방을 떠나면 사라진다.
    /// 효과 적용도 여기서 한다(회복 %, 스트레스, 다음 전투까지 버프).
    /// </summary>
    public sealed class CampSession
    {
        private readonly HashSet<string> _used = new HashSet<string>();
        private readonly Formation _party;
        private readonly IContentCatalog _catalog;
        private readonly StressService _stress;
        private readonly IEventBus _events;

        internal CampSession(int respitePoints, Formation party, IContentCatalog catalog, StressService stress, IEventBus events)
        {
            RespiteRemaining = Guard.NonNegative(respitePoints, nameof(respitePoints));
            _party = Guard.NotNull(party, nameof(party));
            _catalog = Guard.NotNull(catalog, nameof(catalog));
            _stress = Guard.NotNull(stress, nameof(stress));
            _events = Guard.NotNull(events, nameof(events));
        }

        public int RespiteRemaining { get; private set; }

        /// <summary>세이브용 "영웅ID:스킬ID" 목록.</summary>
        public IReadOnlyCollection<string> UsedKeys => _used;

        public static string Key(string heroId, string skillId) => heroId + ":" + skillId;

        public IReadOnlyList<CampSkillDefinition> SkillsOf(Hero hero) => _catalog.CampSkillsFor(hero.ClassId);

        /// <returns>대상과 상관없이 지금 쓸 수 없는 사유, 쓸 수 있으면 null.</returns>
        public string CanUse(Hero user, CampSkillDefinition skill)
        {
            if (user == null || !_party.Contains(user) || user.IsDead) return "파티에 없는 영웅입니다.";
            if (skill == null || skill.ClassId != user.ClassId) return "이 영웅의 야영 스킬이 아닙니다.";
            if (_used.Contains(Key(user.Id, skill.Id))) return "이번 야영에서 이미 썼습니다.";
            if (skill.Cost > RespiteRemaining) return "야영 포인트가 부족합니다. (남은 " + RespiteRemaining + ")";
            return null;
        }

        /// <param name="target">Ally 대상일 때만 필요. Self/Party는 무시한다.</param>
        public string TryUse(Hero user, CampSkillDefinition skill, Hero target)
        {
            string error = CanUse(user, skill);
            if (error != null) return error;
            if (skill.Target == CampSkillTarget.Ally && (target == null || !_party.Contains(target) || target.IsDead))
                return "대상 영웅을 고르세요.";

            RespiteRemaining -= skill.Cost;
            _used.Add(Key(user.Id, skill.Id));
            switch (skill.Target)
            {
                case CampSkillTarget.Self:
                    ApplyAll(skill, user);
                    target = user;
                    break;
                case CampSkillTarget.Ally:
                    ApplyAll(skill, target);
                    break;
                default:
                    for (int i = 0; i < _party.Members.Count; i++) ApplyAll(skill, (Hero)_party.Members[i]);
                    target = null;
                    break;
            }
            _events.Publish(new CampSkillUsed(user, skill, target));
            return null;
        }

        internal void Restore(int respiteRemaining, IEnumerable<string> usedKeys)
        {
            RespiteRemaining = Guard.NonNegative(respiteRemaining, nameof(respiteRemaining));
            _used.Clear();
            foreach (var key in usedKeys) _used.Add(key);
        }

        private void ApplyAll(CampSkillDefinition skill, Hero target)
        {
            for (int i = 0; i < skill.Effects.Count; i++) Apply(skill.Effects[i], target);
        }

        private void Apply(CampEffect effect, Hero target)
        {
            switch (effect.Kind)
            {
                case CampEffectKind.HealPercent:
                    target.Heal(System.Math.Max(1, target.MaxHp * effect.Amount / RandomExtensions.PercentScale));
                    break;
                case CampEffectKind.Stress:
                    _stress.Apply(target, effect.Amount);
                    break;
                case CampEffectKind.Buff:
                    target.Modifiers.Add(effect.ToBonus().WithSource(CampSkillDefinition.BuffSourceId));
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(effect), effect.Kind, "처리하지 않은 야영 효과");
            }
        }
    }
}
