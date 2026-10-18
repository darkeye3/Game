using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Events;

namespace Darkest.Domain.Game
{
    /// <summary>끝난 전투 한 번의 결과. 결과 화면이 도메인 상태를 다시 계산하지 않고 이것만 그린다.</summary>
    public sealed class BattleSummary
    {
        public BattleSummary(BattleResult result, int rounds, int goldGained, IReadOnlyList<Hero> fallen)
        {
            Result = result;
            Rounds = rounds;
            GoldGained = goldGained;
            Fallen = fallen;
        }

        public BattleResult Result { get; }
        public int Rounds { get; }
        public int GoldGained { get; }

        /// <summary>이번 전투에서 쓰러진 영웅.</summary>
        public IReadOnlyList<Hero> Fallen { get; }

        public override string ToString() => "Battle " + Result + " rounds=" + Rounds + " gold=" + GoldGained + " fallen=" + Fallen.Count;
    }
}
