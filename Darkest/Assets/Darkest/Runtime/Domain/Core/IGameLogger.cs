namespace Darkest.Domain.Core
{
    /// <summary>
    /// 도메인은 UnityEngine.Debug를 모른다. Unity 쪽에서는 어댑터(UnityGameLogger)가 이 인터페이스를 구현한다.
    /// </summary>
    public interface IGameLogger
    {
        LogLevel MinimumLevel { get; set; }
        bool IsEnabled(LogLevel level);
        void Log(LogLevel level, string category, string message);
    }

    public static class GameLoggerExtensions
    {
        public static void Verbose(this IGameLogger logger, string category, string message) => logger.Log(LogLevel.Verbose, category, message);
        public static void Info(this IGameLogger logger, string category, string message) => logger.Log(LogLevel.Info, category, message);
        public static void Warn(this IGameLogger logger, string category, string message) => logger.Log(LogLevel.Warning, category, message);
        public static void Error(this IGameLogger logger, string category, string message) => logger.Log(LogLevel.Error, category, message);
    }

    /// <summary>테스트/기본값용. 아무 것도 출력하지 않는다.</summary>
    public sealed class NullLogger : IGameLogger
    {
        public static readonly NullLogger Instance = new NullLogger();

        public LogLevel MinimumLevel { get; set; } = LogLevel.None;
        public bool IsEnabled(LogLevel level) => false;
        public void Log(LogLevel level, string category, string message) { }
    }

    /// <summary>순수 C# 환경(테스트, 콘솔)에서 쓰는 로거. 기록을 메모리에 남겨 검증에도 쓸 수 있다.</summary>
    public sealed class MemoryLogger : IGameLogger
    {
        private readonly System.Collections.Generic.List<string> _lines = new System.Collections.Generic.List<string>();

        public LogLevel MinimumLevel { get; set; } = LogLevel.Verbose;
        public System.Collections.Generic.IReadOnlyList<string> Lines => _lines;

        public bool IsEnabled(LogLevel level) => level >= MinimumLevel && level != LogLevel.None;

        public void Log(LogLevel level, string category, string message)
        {
            if (!IsEnabled(level)) return;
            _lines.Add("[" + level + "][" + category + "] " + message);
        }
    }
}
