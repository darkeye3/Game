using Darkest.Domain.Core;
using UnityEngine;

namespace Darkest.Unity.Infrastructure
{
    /// <summary>IGameLogger → UnityEngine.Debug Adapter. MinimumLevel로 릴리즈 빌드 로그를 끈다.</summary>
    public sealed class UnityGameLogger : IGameLogger
    {
        public UnityGameLogger(LogLevel minimumLevel)
        {
            MinimumLevel = minimumLevel;
        }

        public LogLevel MinimumLevel { get; set; }

        public bool IsEnabled(LogLevel level) => level != LogLevel.None && level >= MinimumLevel;

        public void Log(LogLevel level, string category, string message)
        {
            if (!IsEnabled(level)) return;
            string line = "[Darkest][" + category + "] " + message;
            switch (level)
            {
                case LogLevel.Error: Debug.LogError(line); break;
                case LogLevel.Warning: Debug.LogWarning(line); break;
                default: Debug.Log(line); break;
            }
        }
    }
}
