using Darkest.Domain.Combat.Resolution;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Hamlet;
using Darkest.Domain.Progression;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Game
{
    /// <summary>모든 밸런스 규칙 묶음. Unity에서는 BalanceConfigSO가 이 객체를 채워 넘긴다.</summary>
    public sealed class GameConfig
    {
        public CombatRules Combat { get; } = new CombatRules();
        public StressRules Stress { get; } = new StressRules();
        public DungeonGenerationRules Dungeon { get; } = new DungeonGenerationRules();
        public ExpeditionRules Expedition { get; } = new ExpeditionRules();
        public TownRules Town { get; } = new TownRules();
        public ProgressionRules Progression { get; } = new ProgressionRules();
    }
}
