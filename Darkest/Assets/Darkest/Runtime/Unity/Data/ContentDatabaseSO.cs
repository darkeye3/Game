using System.Collections.Generic;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Content;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Stress;
using UnityEngine;

namespace Darkest.Unity.Data
{
    /// <summary>
    /// 콘텐츠 SO 묶음 → 도메인 ContentCatalog 변환 진입점.
    /// 에셋이 없으면 GameBootstrap이 DefaultContent(코드 더미)로 fallback 한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkest/Content Database", fileName = "ContentDatabase")]
    public sealed class ContentDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<HeroClassSO> heroClasses = new List<HeroClassSO>();
        [SerializeField] private List<MonsterSO> monsters = new List<MonsterSO>();
        [SerializeField] private List<MonsterGroupSO> monsterGroups = new List<MonsterGroupSO>();
        [SerializeField] private List<CurioSO> curios = new List<CurioSO>();
        [SerializeField] private List<ResolveSO> resolves = new List<ResolveSO>();
        [SerializeField] private List<string> heroNames = new List<string>();

        public IContentCatalog BuildCatalog()
        {
            // 같은 SkillSO를 여러 캐릭터가 공유해도 정의는 하나만 만든다(참조 동일성 유지).
            var skillCache = new Dictionary<SkillSO, SkillDefinition>();
            SkillDefinition Resolve(SkillSO so)
            {
                if (!skillCache.TryGetValue(so, out var def))
                {
                    def = so.ToDefinition();
                    skillCache.Add(so, def);
                }
                return def;
            }

            var heroDefs = new List<HeroClassDefinition>(heroClasses.Count);
            foreach (var so in NonNull(heroClasses)) heroDefs.Add(so.ToDefinition(Resolve));
            var monsterDefs = new List<MonsterDefinition>(monsters.Count);
            foreach (var so in NonNull(monsters)) monsterDefs.Add(so.ToDefinition(Resolve));
            var groupDefs = new List<MonsterGroupDefinition>(monsterGroups.Count);
            foreach (var so in NonNull(monsterGroups)) groupDefs.Add(so.ToDefinition());
            var curioDefs = new List<CurioDefinition>(curios.Count);
            foreach (var so in NonNull(curios)) curioDefs.Add(so.ToDefinition());
            var resolveDefs = new List<ResolveDefinition>(resolves.Count);
            foreach (var so in NonNull(resolves)) resolveDefs.Add(so.ToDefinition());

            return new ContentCatalog(heroDefs, monsterDefs, groupDefs, curioDefs, resolveDefs, heroNames);
        }

        private IEnumerable<T> NonNull<T>(List<T> list) where T : Object
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    Debug.LogError("[Darkest][Content] " + name + ": " + typeof(T).Name + " 목록 " + i + "번이 비어 있습니다.", this);
                    continue;
                }
                yield return list[i];
            }
        }

        internal void Populate(List<HeroClassSO> newHeroes, List<MonsterSO> newMonsters, List<MonsterGroupSO> newGroups,
            List<CurioSO> newCurios, List<ResolveSO> newResolves, List<string> newNames)
        {
            heroClasses = newHeroes; monsters = newMonsters; monsterGroups = newGroups;
            curios = newCurios; resolves = newResolves; heroNames = newNames;
        }
    }
}
