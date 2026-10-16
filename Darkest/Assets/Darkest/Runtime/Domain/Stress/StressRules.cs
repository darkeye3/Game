namespace Darkest.Domain.Stress
{
    public sealed class StressRules
    {
        public int MaxStress { get; set; } = 200;
        public int ResolveThreshold { get; set; } = 100;

        /// <summary>결의 판정에서 미덕이 나올 확률(%).</summary>
        public int VirtueChance { get; set; } = 25;

        /// <summary>미덕 발현 시 스트레스를 이 값으로 낮춘다.</summary>
        public int StressAfterVirtue { get; set; } = 45;

        /// <summary>심장마비 후 스트레스 값.</summary>
        public int StressAfterHeartAttack { get; set; } = 170;
    }
}
