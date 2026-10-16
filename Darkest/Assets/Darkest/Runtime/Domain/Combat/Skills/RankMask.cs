using System;

namespace Darkest.Domain.Combat.Skills
{
    /// <summary>열 집합 비트마스크. "1,2열에서 사용 / 3,4열 타격" 같은 다키스트식 위치 규칙을 표현한다.</summary>
    [Flags]
    public enum RankMask
    {
        None = 0,
        Rank1 = 1 << 0,
        Rank2 = 1 << 1,
        Rank3 = 1 << 2,
        Rank4 = 1 << 3,
        Front = Rank1 | Rank2,
        Back = Rank3 | Rank4,
        All = Rank1 | Rank2 | Rank3 | Rank4
    }

    public static class RankMaskExtensions
    {
        public static bool Contains(this RankMask mask, int rank)
        {
            if (rank < 1 || rank > Formation.MaxRanks) return false;
            return (mask & (RankMask)(1 << (rank - 1))) != 0;
        }

        /// <summary>마스크 → "1,2" 문자열. Parse의 역함수.</summary>
        public static string ToRankString(this RankMask mask)
        {
            var sb = new System.Text.StringBuilder();
            for (int rank = 1; rank <= Formation.MaxRanks; rank++)
            {
                if (!mask.Contains(rank)) continue;
                if (sb.Length > 0) sb.Append(',');
                sb.Append(rank);
            }
            return sb.ToString();
        }

        /// <summary>"1,2" 같은 문자열 → 마스크. SO/디버그 입력용.</summary>
        public static RankMask Parse(string ranks)
        {
            if (string.IsNullOrWhiteSpace(ranks)) return RankMask.None;
            var mask = RankMask.None;
            foreach (var token in ranks.Split(','))
            {
                if (!int.TryParse(token.Trim(), out int rank) || rank < 1 || rank > Formation.MaxRanks)
                    throw new FormatException("잘못된 열 표기: " + ranks);
                mask |= (RankMask)(1 << (rank - 1));
            }
            return mask;
        }
    }
}
