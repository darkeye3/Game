namespace Darkest.Domain.Dungeon
{
    /// <summary>던전 생성 파라미터. 밸런스 SO에서 덮어쓴다.</summary>
    public sealed class DungeonGenerationRules
    {
        /// <summary>입구 → 보스까지 일직선 방 개수(입구/보스 제외).</summary>
        public int MainPathRooms { get; set; } = 5;

        /// <summary>메인 경로에서 곁가지로 붙는 방 개수.</summary>
        public int BranchRooms { get; set; } = 3;

        public int BattleWeight { get; set; } = 50;
        public int CurioWeight { get; set; } = 25;
        public int TreasureWeight { get; set; } = 10;
        public int EmptyWeight { get; set; } = 15;

        public int TreasureGoldMin { get; set; } = 150;
        public int TreasureGoldMax { get; set; } = 400;
    }

    /// <summary>탐험(이동/빛/허기) 규칙.</summary>
    public sealed class ExpeditionRules
    {
        public int CorridorSteps { get; set; } = 3;
        public int MaxLight { get; set; } = 100;
        public int LightDecayPerStep { get; set; } = 6;
        public int TorchLightRestore { get; set; } = 25;

        /// <summary>빛 단계 하한(이상이면 해당 단계). [밝음, 흐림, 어둑]. 그 아래는 암흑.</summary>
        public int RadiantThreshold { get; set; } = 75;
        public int DimThreshold { get; set; } = 50;
        public int ShadowyThreshold { get; set; } = 25;

        /// <summary>복도 1칸 이동 시 빛 단계별 스트레스.</summary>
        public int StressPerStepRadiant { get; set; } = 0;
        public int StressPerStepDim { get; set; } = 1;
        public int StressPerStepShadowy { get; set; } = 2;
        public int StressPerStepDark { get; set; } = 4;

        /// <summary>N칸 이동마다 허기 판정.</summary>
        public int HungerStepInterval { get; set; } = 8;
        public int HungerStress { get; set; } = 15;
        public int HungerDamage { get; set; } = 3;

        // ── 빛에 따른 전투/보상 보정 (밝음, 흐림, 어둑, 암흑 순) ──
        public int[] MonsterAccuracyBonusByLight { get; set; } = { 0, 3, 6, 10 };
        public int[] MonsterCritBonusByLight { get; set; } = { 0, 1, 3, 5 };
        public int[] LootBonusPercentByLight { get; set; } = { 0, 10, 25, 50 };

        // ── 야영 ──
        /// <summary>야영 1회에 파티원 1명당 필요한 식량.</summary>
        public int CampFoodPerHero { get; set; } = 1;
        /// <summary>최대 HP 대비 회복 비율(%).</summary>
        public int CampHealPercent { get; set; } = 35;
        public int CampStressRelief { get; set; } = 20;
        /// <summary>야영 후 빛 수치(장작불이 꺼지며 어두워짐을 표현).</summary>
        public int LightAfterCamp { get; set; } = 50;
        /// <summary>야영 1회에 쓸 수 있는 야영 스킬 포인트.</summary>
        public int CampRespitePoints { get; set; } = 6;

        // ── 장신구 전리품 ──
        public int TreasureTrinketChancePercent { get; set; } = 40;
        public int BossTrinketDrops { get; set; } = 1;
    }
}
