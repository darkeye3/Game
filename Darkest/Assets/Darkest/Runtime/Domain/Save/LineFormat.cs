using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Darkest.Domain.Save
{
    /// <summary>
    /// 줄 단위 세이브 포맷의 필드 인코딩. 필드는 '|'로 나누고, 필드 안의 '|', '\', 줄바꿈은 역슬래시로 이스케이프한다.
    /// 파싱 실패는 모두 FormatException으로 통일해 호출자가 줄 번호를 붙여 다시 던질 수 있게 한다.
    /// </summary>
    internal static class LineFormat
    {
        private const char Separator = '|';
        private const char Escape = '\\';

        public static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);
        public static string ULong(ulong value) => value.ToString(CultureInfo.InvariantCulture);
        public static string Bool(bool value) => value ? "1" : "0";

        public static int ParseInt(string[] fields, int index)
        {
            if (!int.TryParse(Field(fields, index), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                throw new FormatException("정수 아님: " + fields[index]);
            return value;
        }

        public static ulong ParseULong(string[] fields, int index)
        {
            if (!ulong.TryParse(Field(fields, index), NumberStyles.None, CultureInfo.InvariantCulture, out ulong value))
                throw new FormatException("부호 없는 정수 아님: " + fields[index]);
            return value;
        }

        public static bool ParseBool(string[] fields, int index)
        {
            int value = ParseInt(fields, index);
            if (value != 0 && value != 1) throw new FormatException("0/1이 아님: " + fields[index]);
            return value == 1;
        }

        public static string Field(string[] fields, int index)
        {
            if (index >= fields.Length) throw new FormatException("필드 부족(" + index + ")");
            return fields[index];
        }

        public static string[] Tail(string[] fields, int from = 1)
        {
            var tail = new string[Math.Max(0, fields.Length - from)];
            Array.Copy(fields, from, tail, 0, tail.Length);
            return tail;
        }

        public static void AppendIds(StringBuilder sb, string tag, IReadOnlyList<string> ids)
        {
            var fields = new string[ids.Count + 1];
            fields[0] = tag;
            for (int i = 0; i < ids.Count; i++) fields[i + 1] = ids[i];
            AppendLine(sb, fields);
        }

        public static void AppendLine(StringBuilder sb, params string[] fields)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) sb.Append(Separator);
                AppendEscaped(sb, fields[i] ?? string.Empty);
            }
            sb.Append('\n');
        }

        public static string[] SplitEscaped(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == Escape && i + 1 < line.Length)
                {
                    char next = line[++i];
                    current.Append(next == 'n' ? '\n' : next == 'r' ? '\r' : next);
                }
                else if (c == Separator)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else current.Append(c);
            }
            fields.Add(current.ToString());
            return fields.ToArray();
        }

        private static void AppendEscaped(StringBuilder sb, string value)
        {
            foreach (char c in value)
            {
                if (c == Separator || c == Escape) sb.Append(Escape).Append(c);
                else if (c == '\n') sb.Append(Escape).Append('n');
                else if (c == '\r') sb.Append(Escape).Append('r');
                else sb.Append(c);
            }
        }
    }
}
