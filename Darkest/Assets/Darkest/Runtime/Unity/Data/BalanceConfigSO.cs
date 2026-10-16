using System;
using Darkest.Domain.Game;
using UnityEngine;

namespace Darkest.Unity.Data
{
    /// <summary>
    /// 밸런스 수치 SO. 도메인 기본값(GameConfig)을 인스펙터에서 덮어쓴다.
    /// 새 규칙을 추가할 때: 도메인 Rules 클래스에 프로퍼티 → 여기 필드 + ApplyTo 한 줄.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkest/Balance Config", fileName = "BalanceConfig")]
    public sealed class BalanceConfigSO : ScriptableObject
    {
        [Serializable]
        private sealed class CombatSection
        {
            [SerializeField, Range(0, 100)] internal int minHitChance = 5;
            [SerializeField, Range(0, 100)] internal int maxHitChance = 95;
            [SerializeField, Min(100)] internal int critMultiplierPercent = 150;
            [SerializeField, Range(0, 100)] internal int maxProtection = 80;
            [SerializeField] internal int stressOnCritReceived = 10;
            [SerializeField] internal int stressReliefOnHeroCrit = 3;
            [SerializeField] internal int stressOnDeathsDoor = 15;
            [SerializeField] internal int stressOnAllyDeath = 20;
            [SerializeField, Min(1)] internal int initiativeDieSides = 8;
        }

        [Serializable]
        private sealed class StressSection
        {
            [SerializeField, Min(1)] internal int maxStress = 200;
            [SerializeField, Min(1)] internal int resolveThreshold = 100;
            [SerializeField, Range(0, 100)] internal int virtueChance = 25;
            [SerializeField] internal int stressAfterVirtue = 45;
            [SerializeField] internal int stressAfterHeartAttack = 170;
        }

        [Serializable]
        private sealed class DungeonSection
        {
            [SerializeField, Min(1)] internal int mainPathRooms = 5;
            [SerializeField, Min(0)] internal int branchRooms = 3;
            [SerializeField, Min(0)] internal int battleWeight = 50;
            [SerializeField, Min(0)] internal int curioWeight = 25;
            [SerializeField, Min(0)] internal int treasureWeight = 10;
            [SerializeField, Min(0)] internal int emptyWeight = 15;
            [SerializeField, Min(1)] internal int corridorSteps = 3;
            [SerializeField, Min(0)] internal int lightDecayPerStep = 6;
            [SerializeField, Min(0)] internal int torchLightRestore = 25;
            [SerializeField, Min(0)] internal int hungerStepInterval = 8;
            [SerializeField] internal int hungerStress = 15;
            [SerializeField] internal int hungerDamage = 3;
        }

        [Serializable]
        private sealed class TownSection
        {
            [SerializeField, Min(0)] internal int startingGold = 1500;
            [SerializeField, Range(1, 9)] internal int startingHeroes = 4;
            [SerializeField, Min(0)] internal int recruitCost = 300;
            [SerializeField, Min(0)] internal int tavernCost = 250;
            [SerializeField, Min(0)] internal int tavernStressRelief = 45;
            [SerializeField, Min(0)] internal int foodPrice = 25;
            [SerializeField, Min(0)] internal int torchPrice = 40;
        }

        // 섹션 필드는 internal + [SerializeField]: 인스펙터에는 보이고, 어셈블리 밖에서는 CreateConfig()로만 읽힌다.
        [SerializeField] private CombatSection combat = new CombatSection();
        [SerializeField] private StressSection stress = new StressSection();
        [SerializeField] private DungeonSection dungeon = new DungeonSection();
        [SerializeField] private TownSection town = new TownSection();

        public GameConfig CreateConfig()
        {
            var config = new GameConfig();

            var c = config.Combat;
            c.MinHitChance = combat.minHitChance; c.MaxHitChance = combat.maxHitChance;
            c.CritMultiplierPercent = combat.critMultiplierPercent; c.MaxProtection = combat.maxProtection;
            c.StressOnCritReceived = combat.stressOnCritReceived; c.StressReliefOnHeroCrit = combat.stressReliefOnHeroCrit;
            c.StressOnDeathsDoor = combat.stressOnDeathsDoor; c.StressOnAllyDeath = combat.stressOnAllyDeath;
            c.InitiativeDieSides = combat.initiativeDieSides;

            var s = config.Stress;
            s.MaxStress = stress.maxStress; s.ResolveThreshold = stress.resolveThreshold; s.VirtueChance = stress.virtueChance;
            s.StressAfterVirtue = stress.stressAfterVirtue; s.StressAfterHeartAttack = stress.stressAfterHeartAttack;

            var d = config.Dungeon;
            d.MainPathRooms = dungeon.mainPathRooms; d.BranchRooms = dungeon.branchRooms;
            d.BattleWeight = dungeon.battleWeight; d.CurioWeight = dungeon.curioWeight;
            d.TreasureWeight = dungeon.treasureWeight; d.EmptyWeight = dungeon.emptyWeight;

            var e = config.Expedition;
            e.CorridorSteps = dungeon.corridorSteps; e.LightDecayPerStep = dungeon.lightDecayPerStep;
            e.TorchLightRestore = dungeon.torchLightRestore; e.HungerStepInterval = dungeon.hungerStepInterval;
            e.HungerStress = dungeon.hungerStress; e.HungerDamage = dungeon.hungerDamage;

            var t = config.Town;
            t.StartingGold = town.startingGold; t.StartingHeroes = town.startingHeroes; t.RecruitCost = town.recruitCost;
            t.TavernCost = town.tavernCost; t.TavernStressRelief = town.tavernStressRelief;
            t.FoodPrice = town.foodPrice; t.TorchPrice = town.torchPrice;

            return config;
        }
    }
}
