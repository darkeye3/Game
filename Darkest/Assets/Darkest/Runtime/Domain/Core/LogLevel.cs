namespace Darkest.Domain.Core
{
    /// <summary>로그 필터 기준. 릴리즈에서는 Warning 이상만 남기는 식으로 토글한다.</summary>
    public enum LogLevel
    {
        Verbose = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        None = 4
    }
}
