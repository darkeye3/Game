using System.Collections.Generic;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Core;

namespace Darkest.Domain.Content
{
    /// <summary>영웅 직업 정의(불변 데이터). 실제 영웅 인스턴스는 CombatantFactory가 만든다.</summary>
    public sealed class HeroClassDefinition
    {
        public HeroClassDefinition(string id, string displayName, StatBlock stats,
            IReadOnlyList<SkillDefinition> skills, string portraitKey)
        {
            Id = Guard.NotBlank(id, nameof(id));
            DisplayName = Guard.NotBlank(displayName, nameof(displayName));
            Stats = Guard.NotNull(stats, nameof(stats));
            Guard.NotEmpty(skills, nameof(skills));
            Skills = skills;
            PortraitKey = portraitKey ?? id;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public StatBlock Stats { get; }
        public IReadOnlyList<SkillDefinition> Skills { get; }
        public string PortraitKey { get; }
    }

    public sealed class MonsterDefinition
    {
        public MonsterDefinition(string id, string displayName, StatBlock stats,
            IReadOnlyList<SkillDefinition> skills, string portraitKey)
        {
            Id = Guard.NotBlank(id, nameof(id));
            DisplayName = Guard.NotBlank(displayName, nameof(displayName));
            Stats = Guard.NotNull(stats, nameof(stats));
            Guard.NotEmpty(skills, nameof(skills));
            Skills = skills;
            PortraitKey = portraitKey ?? id;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public StatBlock Stats { get; }
        public IReadOnlyList<SkillDefinition> Skills { get; }
        public string PortraitKey { get; }
    }

    /// <summary>한 번의 전투에 등장하는 몬스터 편성. 리스트 순서 = 1열부터.</summary>
    public sealed class MonsterGroupDefinition
    {
        public MonsterGroupDefinition(string id, IReadOnlyList<string> monsterIds, bool isBoss, int goldReward)
        {
            Id = Guard.NotBlank(id, nameof(id));
            Guard.NotEmpty(monsterIds, nameof(monsterIds));
            Guard.That(monsterIds.Count <= Combat.Formation.MaxRanks, id + ": 몬스터는 최대 4마리");
            MonsterIds = monsterIds;
            IsBoss = isBoss;
            GoldReward = Guard.NonNegative(goldReward, nameof(goldReward));
        }

        public string Id { get; }
        public IReadOnlyList<string> MonsterIds { get; }
        public bool IsBoss { get; }
        public int GoldReward { get; }
    }
}
