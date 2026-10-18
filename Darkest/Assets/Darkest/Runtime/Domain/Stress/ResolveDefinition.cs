using System.Collections.Generic;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Core;

namespace Darkest.Domain.Stress
{
    /// <summary>
    /// 고통(Affliction) 또는 미덕(Virtue) 정의. 스탯 보정 + 턴 시작 시 아군 스트레스 영향.
    /// </summary>
    public sealed class ResolveDefinition
    {
        public ResolveDefinition(string id, string displayName, bool isVirtue,
            IReadOnlyList<StatModifier> modifiers, int turnStartChance, int turnStartAllyStress)
        {
            Id = Guard.NotBlank(id, nameof(id));
            DisplayName = Guard.NotBlank(displayName, nameof(displayName));
            IsVirtue = isVirtue;
            Modifiers = modifiers ?? System.Array.Empty<StatModifier>();
            TurnStartChance = turnStartChance;
            TurnStartAllyStress = turnStartAllyStress;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public bool IsVirtue { get; }
        public IReadOnlyList<StatModifier> Modifiers { get; }

        /// <summary>턴 시작 시 발동 확률(%). 고통이면 '폭주', 미덕이면 '격려'.</summary>
        public int TurnStartChance { get; }

        /// <summary>발동 시 다른 아군에게 줄 스트레스(양수=피해, 음수=회복).</summary>
        public int TurnStartAllyStress { get; }

        public string ModifierSourceId => SourceIdFor(Id);

        /// <summary>결의 보정치의 출처 ID 규칙. 정의를 만드는 쪽(코드/SO)이 같은 규칙을 쓰도록 한 곳에 둔다.</summary>
        public static string SourceIdFor(string resolveId) => "resolve:" + resolveId;
    }
}
