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

    public sealed class SeededRandom : IRandom
    {
        private readonly System.Random _random;

        public SeededRandom(int seed)
        {
            Seed = seed;
            _random = new System.Random(seed);
        }

        public int Seed { get; }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return _random.Next(minInclusive, maxExclusive);
        }
    }
}
