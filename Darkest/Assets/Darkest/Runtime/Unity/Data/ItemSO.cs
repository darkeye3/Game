using System;
using System.Collections.Generic;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Items;
using UnityEngine;

namespace Darkest.Unity.Data
{
    [CreateAssetMenu(menuName = "Darkest/Trinket", fileName = "Trinket_")]
    public sealed class TrinketSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private TrinketRarity rarity;
        [SerializeField] private List<StatModifierData> bonuses = new List<StatModifierData>();
        [SerializeField, Min(0)] private int sellValue = 150;
        [SerializeField, Min(1), Tooltip("전리품으로 뽑힐 상대 가중치")] private int dropWeight = 3;

        public TrinketDefinition ToDefinition()
        {
            var list = new StatBonus[bonuses.Count];
            for (int i = 0; i < bonuses.Count; i++) list[i] = bonuses[i].ToBonus();
            return new TrinketDefinition(id, displayName, rarity, list, sellValue, dropWeight);
        }

        internal void Populate(TrinketDefinition def)
        {
            id = def.Id; displayName = def.DisplayName; rarity = def.Rarity; sellValue = def.SellValue; dropWeight = def.DropWeight;
            bonuses = new List<StatModifierData>();
            foreach (var bonus in def.Bonuses) bonuses.Add(StatModifierData.Create(bonus.Stat, bonus.Amount));
        }
    }

    [Serializable]
    public sealed class CampEffectData
    {
        [SerializeField] private CampEffectKind kind;
        [SerializeField] private int amount;
        [SerializeField, Tooltip("Buff일 때만 사용")] private StatType stat;

        public CampEffect ToEffect() => new CampEffect(kind, amount, stat);

        internal static CampEffectData From(CampEffect effect) =>
            new CampEffectData { kind = effect.Kind, amount = effect.Amount, stat = effect.Stat };
    }

    [CreateAssetMenu(menuName = "Darkest/Camp Skill", fileName = "CampSkill_")]
    public sealed class CampSkillSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private HeroClassSO heroClass;
        [SerializeField, Min(1)] private int cost = 2;
        [SerializeField] private CampSkillTarget target;
        [SerializeField] private List<CampEffectData> effects = new List<CampEffectData>();

        public CampSkillDefinition ToDefinition()
        {
            if (heroClass == null) throw new InvalidOperationException(name + ": 직업이 비어 있습니다.");
            var list = new CampEffect[effects.Count];
            for (int i = 0; i < effects.Count; i++) list[i] = effects[i].ToEffect();
            return new CampSkillDefinition(id, displayName, heroClass.Id, cost, target, list);
        }

        internal void Populate(CampSkillDefinition def, HeroClassSO owner)
        {
            id = def.Id; displayName = def.DisplayName; heroClass = owner; cost = def.Cost; target = def.Target;
            effects = new List<CampEffectData>();
            foreach (var effect in def.Effects) effects.Add(CampEffectData.From(effect));
        }
    }
}
