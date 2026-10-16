using System;
using System.Collections.Generic;

namespace Darkest.Domain.Core
{
    /// <summary>가드 클로즈 헬퍼. 잘못된 입력은 조용히 넘기지 않고 즉시 throw 한다.</summary>
    public static class Guard
    {
        public static T NotNull<T>(T value, string name) where T : class
        {
            if (value == null) throw new ArgumentNullException(name);
            return value;
        }

        public static string NotBlank(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("값이 비어 있습니다.", name);
            return value;
        }

        public static int InRange(int value, int min, int max, string name)
        {
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(name, value, "허용 범위: " + min + "~" + max);
            return value;
        }

        public static int NonNegative(int value, string name)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(name, value, "음수 불가");
            return value;
        }

        public static void NotEmpty<T>(IReadOnlyCollection<T> items, string name)
        {
            if (items == null) throw new ArgumentNullException(name);
            if (items.Count == 0) throw new ArgumentException("비어 있는 컬렉션", name);
        }

        public static void That(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
