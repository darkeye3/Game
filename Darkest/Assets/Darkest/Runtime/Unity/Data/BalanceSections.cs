using System;
using Darkest.Domain.Combat.Resolution;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Hamlet;
using Darkest.Domain.Progression;
using Darkest.Domain.Stress;
using UnityEngine;

namespace Darkest.Unity.Data
{
    // BalanceConfigSO의 인스펙터 섹션들. 각 섹션은 자기 도메인 Rules에만 값을 넣는다(ApplyTo).
    // 필드는 internal + [SerializeField]: 인스펙터에는 보이고 어셈블리 밖에서는 BalanceConfigSO.CreateConfig()로만 읽힌다.

    [Serializable]
    internal sealed class CombatSection
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
        [SerializeField, Range(0, 100)] internal int retreatChancePercent = 75;
        [SerializeField] internal int stressOnRetreat = 10;
        [SerializeField, Min(1)] internal int maxRounds = 100;

        internal void ApplyTo(CombatRules c)
        {
            c.MinHitChance = minHitChance; c.MaxHitChance = maxHitChance;
            c.CritMultiplierPercent = critMultiplierPercent; c.MaxProtection = maxProtection;
            c.StressOnCritReceived = stressOnCritReceived; c.StressReliefOnHeroCrit = stressReliefOnHeroCrit;
            c.StressOnDeathsDoor = stressOnDeathsDoor; c.StressOnAllyDeath = stressOnAllyDeath;
            c.InitiativeDieSides = initiativeDieSides;
            c.RetreatChancePercent = retreatChancePercent; c.StressOnRetreat = stressOnRetreat;
            c.MaxRounds = maxRounds;
        }
    }

    [Serializable]
    internal sealed class ProgressionSection
    {
        [SerializeField, Tooltip("레벨 n에 필요한 누적 경험치. 0으로 시작, 증가해야 함")] internal int[] xpThresholds = { 0, 4, 10, 18, 28, 40 };
        [SerializeField, Min(0)] internal int xpPerBattleWon = 1;
        [SerializeField, Min(0)] internal int xpOnExpeditionCompleted = 4;
        [SerializeField, Min(0)] internal int xpOnExpeditionRetreated = 1;
        [SerializeField] internal int maxHpPerLevel = 3;
        [SerializeField] internal int accuracyPerLevel = 2;
        [SerializeField] internal int damagePerLevel = 1;
        [SerializeField] internal int dodgePerLevel = 1;

        internal void ApplyTo(ProgressionRules p)
        {
            p.XpThresholds = (int[])xpThresholds.Clone();
            p.XpPerBattleWon = xpPerBattleWon; p.XpOnExpeditionCompleted = xpOnExpeditionCompleted;
            p.XpOnExpeditionRetreated = xpOnExpeditionRetreated;
            p.MaxHpPerLevel = maxHpPerLevel; p.AccuracyPerLevel = accuracyPerLevel;
            p.DamagePerLevel = damagePerLevel; p.DodgePerLevel = dodgePerLevel;
        }
    }

    [Serializable]
    internal sealed class StressSection
    {
        [SerializeField, Min(1)] internal int maxStress = 200;
        [SerializeField, Min(1)] internal int resolveThreshold = 100;
        [SerializeField, Range(0, 100)] internal int virtueChance = 25;
        [SerializeField] internal int stressAfterVirtue = 45;
        [SerializeField] internal int stressAfterHeartAttack = 170;

        internal void ApplyTo(StressRules s)
        {
            s.MaxStress = maxStress; s.ResolveThreshold = resolveThreshold; s.VirtueChance = virtueChance;
            s.StressAfterVirtue = stressAfterVirtue; s.StressAfterHeartAttack = stressAfterHeartAttack;
        }
    }

    [Serializable]
    internal sealed class DungeonSection
    {
        private const string ByLight = "밝음/흐림/어둑/암흑";

        [SerializeField, Min(1)] internal int mainPathRooms = 5;
        [SerializeField, Min(0)] internal int branchRooms = 3;
        [SerializeField, Min(0)] internal int battleWeight = 50;
        [SerializeField, Min(0)] internal int curioWeight = 25;
        [SerializeField, Min(0)] internal int treasureWeight = 10;
        [SerializeField, Min(0)] internal int emptyWeight = 15;
        [SerializeField, Min(0)] internal int treasureGoldMin = 150;
        [SerializeField, Min(0)] internal int treasureGoldMax = 400;
        [SerializeField, Min(1)] internal int corridorSteps = 3;
        [SerializeField, Min(0)] internal int lightDecayPerStep = 6;
        [SerializeField, Min(0)] internal int torchLightRestore = 25;
        [SerializeField, Tooltip("밝음/흐림/어둑 하한 (그 아래는 암흑)")] internal int[] lightThresholds = { 75, 50, 25 };
        [SerializeField, Tooltip(ByLight)] internal int[] stressPerStepByLight = { 0, 1, 2, 4 };
        [SerializeField, Min(0)] internal int hungerStepInterval = 8;
        [SerializeField] internal int hungerStress = 15;
        [SerializeField] internal int hungerDamage = 3;
        [SerializeField, Min(0)] internal int campFoodPerHero = 1;
        [SerializeField, Range(0, 100)] internal int campHealPercent = 35;
        [SerializeField, Min(0)] internal int campStressRelief = 20;
        [SerializeField, Range(0, 100)] internal int lightAfterCamp = 50;
        [SerializeField, Min(0)] internal int campRespitePoints = 6;
        [SerializeField, Range(0, 100)] internal int treasureTrinketChancePercent = 40;
        [SerializeField, Min(0)] internal int bossTrinketDrops = 1;
        [SerializeField, Tooltip(ByLight)] internal int[] monsterAccuracyBonusByLight = { 0, 3, 6, 10 };
        [SerializeField, Tooltip(ByLight)] internal int[] monsterCritBonusByLight = { 0, 1, 3, 5 };
        [SerializeField, Tooltip(ByLight + " (%)")] internal int[] lootBonusPercentByLight = { 0, 10, 25, 50 };

        internal void ApplyTo(DungeonGenerationRules d)
        {
            d.MainPathRooms = mainPathRooms; d.BranchRooms = branchRooms;
            d.BattleWeight = battleWeight; d.CurioWeight = curioWeight;
            d.TreasureWeight = treasureWeight; d.EmptyWeight = emptyWeight;
            d.TreasureGoldMin = treasureGoldMin; d.TreasureGoldMax = Mathf.Max(treasureGoldMin, treasureGoldMax);
        }

        internal void ApplyTo(ExpeditionRules e)
        {
            e.CorridorSteps = corridorSteps; e.LightDecayPerStep = lightDecayPerStep;
            e.TorchLightRestore = torchLightRestore;
            e.RadiantThreshold = lightThresholds[0]; e.DimThreshold = lightThresholds[1]; e.ShadowyThreshold = lightThresholds[2];
            e.StressPerStepRadiant = stressPerStepByLight[0]; e.StressPerStepDim = stressPerStepByLight[1];
            e.StressPerStepShadowy = stressPerStepByLight[2]; e.StressPerStepDark = stressPerStepByLight[3];
            e.HungerStepInterval = hungerStepInterval; e.HungerStress = hungerStress; e.HungerDamage = hungerDamage;
            e.CampFoodPerHero = campFoodPerHero; e.CampHealPercent = campHealPercent;
            e.CampStressRelief = campStressRelief; e.LightAfterCamp = lightAfterCamp; e.CampRespitePoints = campRespitePoints;
            e.TreasureTrinketChancePercent = treasureTrinketChancePercent; e.BossTrinketDrops = bossTrinketDrops;
            e.MonsterAccuracyBonusByLight = (int[])monsterAccuracyBonusByLight.Clone();
            e.MonsterCritBonusByLight = (int[])monsterCritBonusByLight.Clone();
            e.LootBonusPercentByLight = (int[])lootBonusPercentByLight.Clone();
        }
    }

    [Serializable]
    internal sealed class TownSection
    {
        [SerializeField, Min(0)] internal int startingGold = 1500;
        [SerializeField, Range(1, 9)] internal int startingHeroes = 4;
        [SerializeField, Min(1)] internal int maxRosterSize = 9;
        [SerializeField, Min(1)] internal int recruitPoolSize = 3;
        [SerializeField, Min(0)] internal int recruitCost = 300;
        [SerializeField, Min(0)] internal int tavernCost = 250;
        [SerializeField, Min(0)] internal int tavernStressRelief = 45;
        [SerializeField, Min(0)] internal int foodPrice = 25;
        [SerializeField, Min(0)] internal int torchPrice = 40;
        [SerializeField, Min(0)] internal int firewoodPrice = 75;

        [Header("건물 업그레이드")]
        [SerializeField, Min(0)] internal int buildingMaxLevel = 3;
        [SerializeField, Tooltip("[n] = n+1단계 비용. 길이는 최대 단계 이상")] internal int[] buildingUpgradeCosts = { 600, 1200, 2000 };
        [SerializeField, Min(0)] internal int stagecoachRecruitsPerLevel = 1;
        [SerializeField, Min(0)] internal int stagecoachRosterPerLevel = 1;
        [SerializeField, Min(0)] internal int tavernReliefPerLevel = 10;
        [SerializeField, Min(0)] internal int tavernDiscountPerLevel = 25;
        [SerializeField, Range(0, 100)] internal int provisionerDiscountPercentPerLevel = 10;
        [SerializeField, Min(0)] internal int guildXpPerLevel = 1;

        internal void ApplyTo(TownRules t)
        {
            t.StartingGold = startingGold; t.StartingHeroes = startingHeroes;
            t.MaxRosterSize = maxRosterSize; t.RecruitPoolSize = recruitPoolSize; t.RecruitCost = recruitCost;
            t.TavernCost = tavernCost; t.TavernStressRelief = tavernStressRelief;
            t.FoodPrice = foodPrice; t.TorchPrice = torchPrice; t.FirewoodPrice = firewoodPrice;

            var b = t.Buildings;
            b.MaxLevel = buildingMaxLevel; b.UpgradeCosts = (int[])buildingUpgradeCosts.Clone();
            b.StagecoachRecruitsPerLevel = stagecoachRecruitsPerLevel; b.StagecoachRosterPerLevel = stagecoachRosterPerLevel;
            b.TavernReliefPerLevel = tavernReliefPerLevel; b.TavernDiscountPerLevel = tavernDiscountPerLevel;
            b.ProvisionerDiscountPercentPerLevel = provisionerDiscountPercentPerLevel; b.GuildXpPerLevel = guildXpPerLevel;
        }
    }
}
