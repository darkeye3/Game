using System.Collections.Generic;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Items;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Content
{
    /// <summary>
    /// 게임 콘텐츠 조회 창구. 데이터 출처(ScriptableObject / 코드 더미 / 추후 원격 데이터)를 숨긴다.
    /// </summary>
    public interface IContentCatalog
    {
        IReadOnlyList<HeroClassDefinition> HeroClasses { get; }
        IReadOnlyList<MonsterGroupDefinition> NormalGroups { get; }
        IReadOnlyList<MonsterGroupDefinition> BossGroups { get; }
        IReadOnlyList<CurioDefinition> Curios { get; }
        IReadOnlyList<ResolveDefinition> Afflictions { get; }
        IReadOnlyList<ResolveDefinition> Virtues { get; }
        IReadOnlyList<string> HeroNames { get; }
        IReadOnlyList<TrinketDefinition> Trinkets { get; }

        HeroClassDefinition GetHeroClass(string id);
        MonsterDefinition GetMonster(string id);
        MonsterGroupDefinition GetMonsterGroup(string id);
        CurioDefinition GetCurio(string id);
        ResolveDefinition FindResolve(string id);

        /// <summary>세이브 복원용. 없으면 null(삭제된 콘텐츠는 호출자가 로그 후 건너뛴다).</summary>
        TrinketDefinition FindTrinket(string id);

        /// <summary>해당 직업의 야영 스킬. 없으면 빈 목록.</summary>
        IReadOnlyList<CampSkillDefinition> CampSkillsFor(string classId);
        CampSkillDefinition FindCampSkill(string id);
    }
}
