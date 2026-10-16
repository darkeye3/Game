using System.Collections.Generic;
using Darkest.Domain.Dungeon;
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

        HeroClassDefinition GetHeroClass(string id);
        MonsterDefinition GetMonster(string id);
        MonsterGroupDefinition GetMonsterGroup(string id);
        CurioDefinition GetCurio(string id);
        ResolveDefinition FindResolve(string id);
    }
}
