using System.Collections.Generic;

namespace Darkest.Domain.Combat.Status
{
    public enum StatusKind
    {
        Bleed,
        Blight,
        Stun
    }

    /// <summary>지속 효과 1개. 출혈/역병은 턴 시작마다 피해, 기절은 다음 턴 1회 행동 불가.</summary>
    public sealed class StatusEffect
    {
        public StatusEffect(StatusKind kind, int damagePerTurn, int durationTurns)
        {
            Kind = kind;
            DamagePerTurn = damagePerTurn;
            RemainingTurns = durationTurns;
        }

        public StatusKind Kind { get; }
        public int DamagePerTurn { get; }
        public int RemainingTurns { get; private set; }

        internal void ConsumeTurn() => RemainingTurns--;
    }

    public sealed class StatusEffectSet
    {
        private readonly List<StatusEffect> _effects = new List<StatusEffect>();

        public IReadOnlyList<StatusEffect> All => _effects;

        public void Add(StatusEffect effect)
        {
            // 기절은 중첩하지 않는다(다키스트 규칙과 동일). DoT는 중첩.
            if (effect.Kind == StatusKind.Stun && Has(StatusKind.Stun)) return;
            _effects.Add(effect);
        }

        public bool Has(StatusKind kind)
        {
            for (int i = 0; i < _effects.Count; i++)
            {
                if (_effects[i].Kind == kind) return true;
            }
            return false;
        }

        /// <summary>기절 상태였다면 해제하고 true. 턴 시작 시 1회 호출.</summary>
        public bool TryConsumeStun()
        {
            for (int i = 0; i < _effects.Count; i++)
            {
                if (_effects[i].Kind != StatusKind.Stun) continue;
                _effects.RemoveAt(i);
                return true;
            }
            return false;
        }

        /// <summary>출혈/역병 피해 합계를 반환하고 지속시간을 1 줄인다. 만료된 효과는 제거.</summary>
        public int TickDamageOverTime()
        {
            int total = 0;
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                var effect = _effects[i];
                if (effect.Kind == StatusKind.Stun) continue;
                total += effect.DamagePerTurn;
                effect.ConsumeTurn();
                if (effect.RemainingTurns <= 0) _effects.RemoveAt(i);
            }
            return total;
        }

        public void Clear() => _effects.Clear();
    }
}
