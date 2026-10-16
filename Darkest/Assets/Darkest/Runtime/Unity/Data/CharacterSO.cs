using System.Collections.Generic;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Content;
using UnityEngine;

namespace Darkest.Unity.Data
{
    /// <summary>영웅 직업/몬스터 공용 베이스. 차이는 변환 결과 타입뿐이다.</summary>
    public abstract class CharacterSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("Resources/Darkest/Placeholders/ 아래 이미지 이름")]
        [SerializeField] private string portraitKey;
        [SerializeField] private StatBlockData stats = new StatBlockData();
        [SerializeField] private List<SkillSO> skills = new List<SkillSO>();

        public string Id => id;
        protected string DisplayName => displayName;
        protected string PortraitKey => portraitKey;
        protected StatBlockData Stats => stats;

        protected SkillDefinition[] BuildSkills(System.Func<SkillSO, SkillDefinition> resolve)
        {
            var result = new SkillDefinition[skills.Count];
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] == null) throw new System.InvalidOperationException(name + ": 스킬 슬롯 " + i + "이 비어 있습니다.");
                result[i] = resolve(skills[i]);
            }
            return result;
        }

        internal void Populate(string newId, string newName, string newPortraitKey, StatBlockData newStats, List<SkillSO> newSkills)
        {
            id = newId; displayName = newName; portraitKey = newPortraitKey; stats = newStats; skills = newSkills;
        }
    }

    [CreateAssetMenu(menuName = "Darkest/Hero Class", fileName = "Hero_")]
    public sealed class HeroClassSO : CharacterSO
    {
        public HeroClassDefinition ToDefinition(System.Func<SkillSO, SkillDefinition> resolve) =>
            new HeroClassDefinition(Id, DisplayName, Stats.ToStatBlock(), BuildSkills(resolve), PortraitKey);
    }

    [CreateAssetMenu(menuName = "Darkest/Monster", fileName = "Monster_")]
    public sealed class MonsterSO : CharacterSO
    {
        public MonsterDefinition ToDefinition(System.Func<SkillSO, SkillDefinition> resolve) =>
            new MonsterDefinition(Id, DisplayName, Stats.ToStatBlock(), BuildSkills(resolve), PortraitKey);
    }
}
