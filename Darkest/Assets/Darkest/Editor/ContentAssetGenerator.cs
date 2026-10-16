using System.Collections.Generic;
using System.IO;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Content;
using Darkest.Domain.Stress;
using Darkest.Unity.Data;
using UnityEditor;
using UnityEngine;

namespace Darkest.Editor
{
    /// <summary>
    /// DefaultContent(코드 더미) → ScriptableObject 에셋 내보내기. 이후 기획 수정은 에셋에서 한다.
    /// 여러 번 실행해도 같은 경로의 에셋을 덮어쓴다(멱등).
    /// </summary>
    public static class ContentAssetGenerator
    {
        private const string Root = "Assets/Darkest/Data";
        private const string MenuPath = "Darkest/Generate Content Assets";

        [MenuItem(MenuPath)]
        public static void Generate()
        {
            var catalog = DefaultContent.Create();
            var skills = new Dictionary<string, SkillSO>();

            var heroes = new List<HeroClassSO>();
            foreach (var def in catalog.HeroClasses)
            {
                var so = LoadOrCreate<HeroClassSO>("Heroes", def.Id);
                so.Populate(def.Id, def.DisplayName, def.PortraitKey, StatBlockData.From(def.Stats), ExportSkills(def.Skills, skills));
                Save(so, heroes);
            }

            var monsters = new List<MonsterSO>();
            var monsterById = new Dictionary<string, MonsterSO>();
            var groups = new List<MonsterGroupSO>();
            foreach (var group in Concat(catalog.NormalGroups, catalog.BossGroups))
            {
                var members = new List<MonsterSO>();
                foreach (var monsterId in group.MonsterIds)
                {
                    if (!monsterById.TryGetValue(monsterId, out var monsterSo))
                    {
                        var def = catalog.GetMonster(monsterId);
                        monsterSo = LoadOrCreate<MonsterSO>("Monsters", def.Id);
                        monsterSo.Populate(def.Id, def.DisplayName, def.PortraitKey, StatBlockData.From(def.Stats), ExportSkills(def.Skills, skills));
                        Save(monsterSo, monsters);
                        monsterById.Add(monsterId, monsterSo);
                    }
                    members.Add(monsterSo);
                }
                var groupSo = LoadOrCreate<MonsterGroupSO>("Groups", group.Id);
                groupSo.Populate(group.Id, members, group.IsBoss, group.GoldReward);
                Save(groupSo, groups);
            }

            var curios = new List<CurioSO>();
            foreach (var def in catalog.Curios)
            {
                var outcomes = new List<CurioOutcomeData>();
                foreach (var outcome in def.Outcomes) outcomes.Add(CurioOutcomeData.From(outcome));
                var so = LoadOrCreate<CurioSO>("Curios", def.Id);
                so.Populate(def.Id, def.DisplayName, outcomes);
                Save(so, curios);
            }

            var resolves = new List<ResolveSO>();
            foreach (var def in Concat(catalog.Afflictions, catalog.Virtues)) Save(ExportResolve(def), resolves);

            var database = LoadOrCreate<ContentDatabaseSO>(string.Empty, "ContentDatabase");
            database.Populate(heroes, monsters, groups, curios, resolves, new List<string>(catalog.HeroNames));
            EditorUtility.SetDirty(database);
            LoadOrCreate<BalanceConfigSO>(string.Empty, "BalanceConfig");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = database;
            Debug.Log("[Darkest][Editor] 콘텐츠 에셋 생성 완료: " + Root + " (GameBootstrap의 Content 슬롯에 ContentDatabase를 넣으세요)");
        }

        private static List<SkillSO> ExportSkills(IReadOnlyList<SkillDefinition> definitions, Dictionary<string, SkillSO> cache)
        {
            var result = new List<SkillSO>();
            foreach (var def in definitions)
            {
                if (!cache.TryGetValue(def.Id, out var so))
                {
                    var effects = new List<SkillEffectData>();
                    foreach (var effect in def.Effects)
                    {
                        var spec = effect.ToSpec();
                        effects.Add(SkillEffectData.Create(spec.Kind, spec.Amount, spec.AmountMax, spec.Duration, spec.Chance));
                    }
                    so = LoadOrCreate<SkillSO>("Skills", def.Id);
                    so.Populate(def.Id, def.DisplayName, def.UsableFrom.ToRankString(), def.Side, def.TargetRanks.ToRankString(),
                        def.HitsAllTargets, def.Accuracy, def.DamagePercent, def.CritBonus, def.SelfMove, effects);
                    EditorUtility.SetDirty(so);
                    cache.Add(def.Id, so);
                }
                result.Add(so);
            }
            return result;
        }

        private static ResolveSO ExportResolve(ResolveDefinition def)
        {
            var modifiers = new List<StatModifierData>();
            foreach (var modifier in def.Modifiers) modifiers.Add(StatModifierData.Create(modifier.Stat, modifier.Amount));
            var so = LoadOrCreate<ResolveSO>("Resolves", def.Id);
            so.Populate(def.Id, def.DisplayName, def.IsVirtue, modifiers, def.TurnStartChance, def.TurnStartAllyStress);
            return so;
        }

        private static T LoadOrCreate<T>(string folder, string assetName) where T : ScriptableObject
        {
            string directory = string.IsNullOrEmpty(folder) ? Root : Root + "/" + folder;
            Directory.CreateDirectory(directory);
            string path = directory + "/" + assetName + ".asset";

            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void Save<T>(T so, List<T> into) where T : ScriptableObject
        {
            EditorUtility.SetDirty(so);
            into.Add(so);
        }

        private static IEnumerable<T> Concat<T>(IReadOnlyList<T> a, IReadOnlyList<T> b)
        {
            foreach (var item in a) yield return item;
            foreach (var item in b) yield return item;
        }
    }
}
