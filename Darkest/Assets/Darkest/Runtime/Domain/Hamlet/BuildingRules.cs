namespace Darkest.Domain.Hamlet
{
    public enum BuildingType
    {
        /// <summary>역마차: 모집 후보 수와 명부 크기 증가.</summary>
        Stagecoach,
        /// <summary>선술집: 치료량 증가, 비용 감소.</summary>
        Tavern,
        /// <summary>보급상: 보급품 할인.</summary>
        Provisioner,
        /// <summary>훈련소: 귀환 시 추가 경험치.</summary>
        Guild
    }

    /// <summary>마을 건물 업그레이드 규칙. 단계마다 같은 폭으로 효과가 오른다.</summary>
    public sealed class BuildingRules
    {
        public static readonly int BuildingCount = System.Enum.GetValues(typeof(BuildingType)).Length;

        public int MaxLevel { get; set; } = 3;

        /// <summary>[n] = n+1단계로 올리는 비용. 길이는 MaxLevel 이상이어야 한다.</summary>
        public int[] UpgradeCosts { get; set; } = { 600, 1200, 2000 };

        public int StagecoachRecruitsPerLevel { get; set; } = 1;
        public int StagecoachRosterPerLevel { get; set; } = 1;
        public int TavernReliefPerLevel { get; set; } = 10;
        public int TavernDiscountPerLevel { get; set; } = 25;
        public int ProvisionerDiscountPercentPerLevel { get; set; } = 10;
        public int GuildXpPerLevel { get; set; } = 1;
    }
}
