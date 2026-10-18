namespace Darkest.Domain.Progression
{
    /// <summary>경험치/레벨 규칙. 레벨 0에서 시작해 원정 정산 때 경험치를 받는다.</summary>
    public sealed class ProgressionRules
    {
        /// <summary>레벨 n이 되는 데 필요한 누적 경험치. [0]은 항상 0, 길이-1이 최대 레벨.</summary>
        public int[] XpThresholds { get; set; } = { 0, 4, 10, 18, 28, 40 };

        public int XpPerBattleWon { get; set; } = 1;
        public int XpOnExpeditionCompleted { get; set; } = 4;
        public int XpOnExpeditionRetreated { get; set; } = 1;

        // 레벨당 스탯 상승치
        public int MaxHpPerLevel { get; set; } = 3;
        public int AccuracyPerLevel { get; set; } = 2;
        public int DamagePerLevel { get; set; } = 1;
        public int DodgePerLevel { get; set; } = 1;

        public int MaxLevel => XpThresholds.Length - 1;
    }
}
