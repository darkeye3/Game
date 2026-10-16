namespace Darkest.Domain.Combat.Resolution
{
    /// <summary>
    /// 전투 밸런스 상수 모음. 매직넘버를 코드에 박지 않고 여기(→ BalanceConfig SO)에서 주입한다.
    /// </summary>
    public sealed class CombatRules
    {
        public int MinHitChance { get; set; } = 5;
        public int MaxHitChance { get; set; } = 95;
        public int CritMultiplierPercent { get; set; } = 150;
        public int MaxProtection { get; set; } = 80;

        /// <summary>영웅이 치명타를 맞았을 때 받는 스트레스.</summary>
        public int StressOnCritReceived { get; set; } = 10;

        /// <summary>영웅이 치명타를 냈을 때 파티원 전원이 회복하는 스트레스.</summary>
        public int StressReliefOnHeroCrit { get; set; } = 3;

        /// <summary>영웅이 죽음의 문턱에 들어설 때 받는 스트레스.</summary>
        public int StressOnDeathsDoor { get; set; } = 15;

        /// <summary>아군 사망을 목격한 영웅들이 받는 스트레스.</summary>
        public int StressOnAllyDeath { get; set; } = 20;

        /// <summary>턴 순서 결정 시 속도에 더해지는 무작위 값의 최대치(1~N).</summary>
        public int InitiativeDieSides { get; set; } = 8;

        /// <summary>무한 루프 방지용 안전장치.</summary>
        public int MaxRounds { get; set; } = 100;
    }
}
