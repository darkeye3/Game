using System.Collections.Generic;
using Darkest.Domain.Core;

namespace Darkest.Domain.Items
{
    /// <summary>가중치 기반 장신구 뽑기. 장신구가 하나도 없으면 아무것도 주지 않는다(콘텐츠 선택 사항).</summary>
    public sealed class TrinketDropTable
    {
        private readonly IReadOnlyList<TrinketDefinition> _trinkets;
        private readonly int _totalWeight;

        public TrinketDropTable(IReadOnlyList<TrinketDefinition> trinkets)
        {
            _trinkets = Guard.NotNull(trinkets, nameof(trinkets));
            for (int i = 0; i < trinkets.Count; i++) _totalWeight += trinkets[i].DropWeight;
        }

        public bool IsEmpty => _totalWeight == 0;

        /// <returns>뽑힌 장신구. 표가 비었으면 null.</returns>
        public TrinketDefinition Roll(IRandom random)
        {
            if (IsEmpty) return null;
            int roll = random.Range(0, _totalWeight);
            for (int i = 0; i < _trinkets.Count; i++)
            {
                roll -= _trinkets[i].DropWeight;
                if (roll < 0) return _trinkets[i];
            }
            return _trinkets[_trinkets.Count - 1];
        }
    }
}
