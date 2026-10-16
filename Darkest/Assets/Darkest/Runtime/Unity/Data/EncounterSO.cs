using System.Collections.Generic;
using Darkest.Domain.Content;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Stress;
using UnityEngine;

namespace Darkest.Unity.Data
{
    [CreateAssetMenu(menuName = "Darkest/Monster Group", fileName = "Group_")]
    public sealed class MonsterGroupSO : ScriptableObject
    {
        [SerializeField] private string id;
        [Tooltip("1열부터 순서대로, 최대 4")]
        [SerializeField] private List<MonsterSO> monsters = new List<MonsterSO>();
        [SerializeField] private bool isBoss;
        [SerializeField, Min(0)] private int goldReward = 100;

        public MonsterGroupDefinition ToDefinition()
        {
            var ids = new string[monsters.Count];
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i] == null) throw new System.InvalidOperationException(name + ": 몬스터 슬롯 " + i + "이 비어 있습니다.");
                ids[i] = monsters[i].Id;
            }
            return new MonsterGroupDefinition(id, ids, isBoss, goldReward);
        }

        internal void Populate(string newId, List<MonsterSO> newMonsters, bool boss, int gold)
        {
            id = newId; monsters = newMonsters; isBoss = boss; goldReward = gold;
        }
    }

    [CreateAssetMenu(menuName = "Darkest/Curio", fileName = "Curio_")]
    public sealed class CurioSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private List<CurioOutcomeData> outcomes = new List<CurioOutcomeData>();

        public CurioDefinition ToDefinition()
        {
            var list = new CurioOutcome[outcomes.Count];
            for (int i = 0; i < outcomes.Count; i++) list[i] = outcomes[i].ToOutcome();
            return new CurioDefinition(id, displayName, list);
        }

        internal void Populate(string newId, string newName, List<CurioOutcomeData> newOutcomes)
        {
            id = newId; displayName = newName; outcomes = newOutcomes;
        }
    }

    [CreateAssetMenu(menuName = "Darkest/Resolve (Affliction or Virtue)", fileName = "Resolve_")]
    public sealed class ResolveSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private bool isVirtue;
        [SerializeField] private List<StatModifierData> modifiers = new List<StatModifierData>();
        [SerializeField, Range(0, 100)] private int turnStartChance = 30;
        [Tooltip("발동 시 다른 아군 스트레스. 양수=피해(고통), 음수=회복(미덕)")]
        [SerializeField] private int turnStartAllyStress = 8;

        public ResolveDefinition ToDefinition()
        {
            string source = ResolveDefinition.SourceIdFor(id);
            var list = new Domain.Combat.Stats.StatModifier[modifiers.Count];
            for (int i = 0; i < modifiers.Count; i++) list[i] = modifiers[i].ToModifier(source);
            return new ResolveDefinition(id, displayName, isVirtue, list, turnStartChance, turnStartAllyStress);
        }

        internal void Populate(string newId, string newName, bool virtue, List<StatModifierData> newModifiers, int chance, int allyStress)
        {
            id = newId; displayName = newName; isVirtue = virtue; modifiers = newModifiers;
            turnStartChance = chance; turnStartAllyStress = allyStress;
        }
    }
}
