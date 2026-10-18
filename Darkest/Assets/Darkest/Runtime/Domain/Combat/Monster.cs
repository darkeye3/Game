using System.Collections.Generic;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Stats;

namespace Darkest.Domain.Combat
{
    public sealed class Monster : Combatant
    {
        public Monster(string id, string displayName, string definitionId, StatBlock baseStats,
            IReadOnlyList<SkillDefinition> skills, string portraitKey)
            : base(id, displayName, Team.Monsters, baseStats, skills, portraitKey)
        {
            DefinitionId = definitionId;
        }

        public string DefinitionId { get; }
    }
}
