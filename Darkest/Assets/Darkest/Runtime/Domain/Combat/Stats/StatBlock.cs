using System;
using System.Text;

namespace Darkest.Domain.Combat.Stats
{
    /// <summary>
    /// 기본 스탯 묶음(불변). 캐릭터 정의(Definition)가 소유하고, 런타임 변화는 StatModifierSet이 담당한다.
    /// </summary>
    public sealed class StatBlock
    {
        private readonly int[] _values;

        private StatBlock(int[] values)
        {
            _values = values;
        }

        public int this[StatType type] => _values[(int)type];

        public static Builder Create() => new Builder();

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < (int)StatType.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append((StatType)i).Append('=').Append(_values[i]);
            }
            return sb.ToString();
        }

        /// <summary>읽기 쉬운 체이닝 생성기. 데이터(SO)·코드 콘텐츠 양쪽에서 같은 경로로 만든다.</summary>
        public sealed class Builder
        {
            private readonly int[] _values = new int[(int)StatType.Count];

            public Builder Set(StatType type, int value)
            {
                if (type == StatType.Count) throw new ArgumentOutOfRangeException(nameof(type));
                _values[(int)type] = value;
                return this;
            }

            public Builder Hp(int value) => Set(StatType.MaxHp, value);
            public Builder Speed(int value) => Set(StatType.Speed, value);
            public Builder Accuracy(int value) => Set(StatType.Accuracy, value);
            public Builder Crit(int value) => Set(StatType.CritChance, value);
            public Builder Dodge(int value) => Set(StatType.Dodge, value);
            public Builder Protection(int value) => Set(StatType.Protection, value);
            public Builder Damage(int min, int max) => Set(StatType.DamageMin, min).Set(StatType.DamageMax, max);

            public Builder Resist(int stun, int bleed, int blight, int deathblow = 0) =>
                Set(StatType.StunResist, stun).Set(StatType.BleedResist, bleed)
                    .Set(StatType.BlightResist, blight).Set(StatType.DeathblowResist, deathblow);

            public StatBlock Build()
            {
                if (_values[(int)StatType.MaxHp] <= 0) throw new InvalidOperationException("MaxHp는 1 이상이어야 합니다.");
                if (_values[(int)StatType.DamageMax] < _values[(int)StatType.DamageMin])
                    throw new InvalidOperationException("DamageMax < DamageMin");
                return new StatBlock((int[])_values.Clone());
            }
        }
    }
}
