using System.Collections.Generic;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Items;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Content
{
    /// <summary>
    /// 메모리 기반 카탈로그. 생성 시점에 ID 중복·누락 참조를 검증해서 잘못된 데이터가 런타임 깊숙이 들어가지 않게 한다.
    /// </summary>
    public sealed class ContentCatalog : IContentCatalog
    {
        private readonly Dictionary<string, HeroClassDefinition> _heroClasses = new Dictionary<string, HeroClassDefinition>();
        private readonly Dictionary<string, MonsterDefinition> _monsters = new Dictionary<string, MonsterDefinition>();
        private readonly Dictionary<string, MonsterGroupDefinition> _groups = new Dictionary<string, MonsterGroupDefinition>();
        private readonly Dictionary<string, CurioDefinition> _curios = new Dictionary<string, CurioDefinition>();
        private readonly Dictionary<string, ResolveDefinition> _resolves = new Dictionary<string, ResolveDefinition>();
        private readonly List<HeroClassDefinition> _heroClassList = new List<HeroClassDefinition>();
        private readonly List<MonsterGroupDefinition> _normalGroups = new List<MonsterGroupDefinition>();
        private readonly List<MonsterGroupDefinition> _bossGroups = new List<MonsterGroupDefinition>();
        private readonly List<CurioDefinition> _curioList = new List<CurioDefinition>();
        private readonly List<ResolveDefinition> _afflictions = new List<ResolveDefinition>();
        private readonly List<ResolveDefinition> _virtues = new List<ResolveDefinition>();
        private readonly List<string> _heroNames = new List<string>();
        private readonly Dictionary<string, TrinketDefinition> _trinkets = new Dictionary<string, TrinketDefinition>();
        private readonly List<TrinketDefinition> _trinketList = new List<TrinketDefinition>();
        private readonly Dictionary<string, CampSkillDefinition> _campSkills = new Dictionary<string, CampSkillDefinition>();
        private readonly Dictionary<string, List<CampSkillDefinition>> _campSkillsByClass = new Dictionary<string, List<CampSkillDefinition>>();
        private static readonly CampSkillDefinition[] NoCampSkills = new CampSkillDefinition[0];

        public ContentCatalog(
            IEnumerable<HeroClassDefinition> heroClasses,
            IEnumerable<MonsterDefinition> monsters,
            IEnumerable<MonsterGroupDefinition> groups,
            IEnumerable<CurioDefinition> curios,
            IEnumerable<ResolveDefinition> resolves,
            IEnumerable<string> heroNames,
            IEnumerable<TrinketDefinition> trinkets = null,
            IEnumerable<CampSkillDefinition> campSkills = null)
        {
            foreach (var c in heroClasses) { AddUnique(_heroClasses, c.Id, c); _heroClassList.Add(c); }
            foreach (var m in monsters) AddUnique(_monsters, m.Id, m);
            foreach (var g in groups) AddGroup(g);
            foreach (var c in curios) { AddUnique(_curios, c.Id, c); _curioList.Add(c); }
            foreach (var r in resolves) { AddUnique(_resolves, r.Id, r); (r.IsVirtue ? _virtues : _afflictions).Add(r); }
            _heroNames.AddRange(heroNames);
            if (trinkets != null) foreach (var t in trinkets) { AddUnique(_trinkets, t.Id, t); _trinketList.Add(t); }
            if (campSkills != null) foreach (var c in campSkills) AddCampSkill(c);

            Guard.That(_heroClassList.Count > 0, "영웅 직업이 없습니다.");
            Guard.That(_normalGroups.Count > 0 && _bossGroups.Count > 0, "일반/보스 몬스터 편성이 각각 1개 이상 필요합니다.");
            Guard.That(_afflictions.Count > 0 && _virtues.Count > 0, "고통/미덕이 각각 1개 이상 필요합니다.");
            Guard.That(_heroNames.Count > 0, "영웅 이름 풀이 비었습니다.");
        }

        public IReadOnlyList<HeroClassDefinition> HeroClasses => _heroClassList;
        public IReadOnlyList<MonsterGroupDefinition> NormalGroups => _normalGroups;
        public IReadOnlyList<MonsterGroupDefinition> BossGroups => _bossGroups;
        public IReadOnlyList<CurioDefinition> Curios => _curioList;
        public IReadOnlyList<ResolveDefinition> Afflictions => _afflictions;
        public IReadOnlyList<ResolveDefinition> Virtues => _virtues;
        public IReadOnlyList<string> HeroNames => _heroNames;
        public IReadOnlyList<TrinketDefinition> Trinkets => _trinketList;

        public HeroClassDefinition GetHeroClass(string id) => Get(_heroClasses, id, "영웅 직업");
        public MonsterDefinition GetMonster(string id) => Get(_monsters, id, "몬스터");
        public MonsterGroupDefinition GetMonsterGroup(string id) => Get(_groups, id, "몬스터 편성");
        public CurioDefinition GetCurio(string id) => Get(_curios, id, "골동품");

        public ResolveDefinition FindResolve(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _resolves.TryGetValue(id, out var resolve) ? resolve : null;
        }

        public TrinketDefinition FindTrinket(string id) =>
            !string.IsNullOrEmpty(id) && _trinkets.TryGetValue(id, out var trinket) ? trinket : null;

        public IReadOnlyList<CampSkillDefinition> CampSkillsFor(string classId) =>
            classId != null && _campSkillsByClass.TryGetValue(classId, out var list) ? list : (IReadOnlyList<CampSkillDefinition>)NoCampSkills;

        public CampSkillDefinition FindCampSkill(string id) =>
            !string.IsNullOrEmpty(id) && _campSkills.TryGetValue(id, out var skill) ? skill : null;

        private void AddCampSkill(CampSkillDefinition skill)
        {
            if (!_heroClasses.ContainsKey(skill.ClassId))
                throw new KeyNotFoundException(skill.Id + " 야영 스킬의 직업이 없습니다: " + skill.ClassId);
            AddUnique(_campSkills, skill.Id, skill);
            if (!_campSkillsByClass.TryGetValue(skill.ClassId, out var list))
            {
                list = new List<CampSkillDefinition>();
                _campSkillsByClass.Add(skill.ClassId, list);
            }
            list.Add(skill);
        }

        private void AddGroup(MonsterGroupDefinition group)
        {
            AddUnique(_groups, group.Id, group);
            for (int i = 0; i < group.MonsterIds.Count; i++)
            {
                if (!_monsters.ContainsKey(group.MonsterIds[i]))
                    throw new KeyNotFoundException(group.Id + " 편성에 없는 몬스터: " + group.MonsterIds[i]);
            }
            (group.IsBoss ? _bossGroups : _normalGroups).Add(group);
        }

        private static void AddUnique<T>(Dictionary<string, T> map, string id, T value)
        {
            if (map.ContainsKey(id)) throw new System.ArgumentException("중복 ID: " + id);
            map.Add(id, value);
        }

        private static T Get<T>(Dictionary<string, T> map, string id, string kind)
        {
            if (id != null && map.TryGetValue(id, out var value)) return value;
            throw new KeyNotFoundException(kind + " ID를 찾을 수 없습니다: " + id);
        }
    }
}
