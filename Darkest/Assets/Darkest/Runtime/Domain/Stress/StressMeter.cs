using Darkest.Domain.Core;

namespace Darkest.Domain.Stress
{
    public enum ResolveState
    {
        None,
        Afflicted,
        Virtuous
    }

    /// <summary>
    /// 스트레스 수치 + 결의(고통/미덕) 상태. 판정(확률)은 StressService가 하고, 여기는 값만 보관한다.
    /// </summary>
    public sealed class StressMeter
    {
        public StressMeter(int max)
        {
            Max = Guard.InRange(max, 1, 1000, nameof(max));
        }

        public int Current { get; private set; }
        public int Max { get; }
        public ResolveDefinition Resolve { get; private set; }

        public ResolveState State =>
            Resolve == null ? ResolveState.None : Resolve.IsVirtue ? ResolveState.Virtuous : ResolveState.Afflicted;

        /// <returns>실제 변화량(클램프 반영)</returns>
        internal int Change(int delta)
        {
            int before = Current;
            Set(Current + delta);
            return Current - before;
        }

        internal void Set(int value) => Current = value < 0 ? 0 : value > Max ? Max : value;

        internal void SetResolve(ResolveDefinition resolve) => Resolve = resolve;

        internal void ClearResolve() => Resolve = null;
    }
}
