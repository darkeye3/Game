namespace Darkest.Domain.Core
{
    /// <summary>
    /// 모든 확률 판정은 이 인터페이스를 통한다. seed를 주입하면 같은 입력 → 같은 결과로 버그를 재현할 수 있다.
    /// </summary>
    public interface IRandom
    {
        int Seed { get; }

        /// <summary>[minInclusive, maxExclusive)</summary>
        int Range(int minInclusive, int maxExclusive);
    }

    public static class RandomExtensions
    {
        public const int PercentScale = 100;

        /// <summary>chancePercent(0~100) 확률로 true.</summary>
        public static bool RollPercent(this IRandom random, int chancePercent)
        {
            if (chancePercent <= 0) return false;
            if (chancePercent >= PercentScale) return true;
            return random.Range(0, PercentScale) < chancePercent;
        }

        /// <summary>[min, max] 양 끝 포함.</summary>
        public static int RangeInclusive(this IRandom random, int min, int max) => random.Range(min, max + 1);

        public static T Pick<T>(this IRandom random, System.Collections.Generic.IReadOnlyList<T> items)
        {
            Guard.NotEmpty(items, nameof(items));
            return items[random.Range(0, items.Count)];
        }
    }

    /// <summary>
    /// 진행 상태를 저장·복원할 수 있는 난수. 세이브를 불러온 뒤에도 "같은 입력 → 같은 결과"가 이어지게 한다.
    /// </summary>
    public interface IRestorableRandom : IRandom
    {
        ulong State { get; }
        void RestoreState(ulong state);
    }

    /// <summary>
    /// SplitMix64 기반 난수. System.Random은 내부 상태를 꺼낼 수 없어 세이브에 담을 수 없으므로 직접 구현한다.
    /// </summary>
    public sealed class SeededRandom : IRestorableRandom
    {
        private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;
        private const ulong MixA = 0xBF58476D1CE4E5B9UL;
        private const ulong MixB = 0x94D049BB133111EBUL;

        public SeededRandom(int seed)
        {
            Seed = seed;
            State = unchecked((ulong)(uint)seed * GoldenGamma);
        }

        public int Seed { get; }
        public ulong State { get; private set; }

        public void RestoreState(ulong state) => State = state;

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            ulong span = (ulong)((long)maxExclusive - minInclusive);
            return (int)(minInclusive + (long)(NextUInt64() % span));
        }

        private ulong NextUInt64()
        {
            unchecked
            {
                State += GoldenGamma;
                ulong z = State;
                z = (z ^ (z >> 30)) * MixA;
                z = (z ^ (z >> 27)) * MixB;
                return z ^ (z >> 31);
            }
        }
    }
}
