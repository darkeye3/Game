using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Resolution;
using Darkest.Domain.Core;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Dungeon
{
    /// <summary>
    /// 전투 밖 파티 피해(굶주림, 골동품 함정). 전투의 DamageReactions와 같은 CombatRules 값으로
    /// 죽음의 문턱 진입·동료 사망 스트레스를 준다. 규칙 값이 두 군데로 갈라지지 않게 하려는 것.
    /// </summary>
    public sealed class PartyHazard
    {
        private readonly CombatRules _rules;
        private readonly StressService _stress;
        private readonly IRandom _random;

        public PartyHazard(CombatRules rules, StressService stress, IRandom random)
        {
            _rules = Guard.NotNull(rules, nameof(rules));
            _stress = Guard.NotNull(stress, nameof(stress));
            _random = Guard.NotNull(random, nameof(random));
        }

        /// <summary>살아 있는 파티원 전원에게 피해. 사망자는 진형에 남으며 Expedition이 이후에 정리한다.</summary>
        public void DamageAll(Formation party, int amount)
        {
            Guard.NotNull(party, nameof(party));
            int deaths = 0;
            for (int i = 0; i < party.Members.Count; i++)
            {
                var member = party.Members[i];
                if (member.IsDead) continue;

                var outcome = member.ApplyDamage(amount, _random);
                if (!(member is Hero hero)) continue;
                if (hero.IsDead) deaths++;
                else if (outcome == DamageOutcome.EnteredDeathsDoor) _stress.Apply(hero, _rules.StressOnDeathsDoor);
            }

            if (deaths == 0) return;
            for (int i = 0; i < party.Members.Count; i++)
            {
                if (party.Members[i] is Hero survivor && survivor.IsAlive)
                    _stress.Apply(survivor, _rules.StressOnAllyDeath * deaths);
            }
        }
    }
}
