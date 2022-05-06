using Vostok.Logging.Abstractions;

namespace CSharpCompiler;

internal static class ApplicationConstants
{
    public static string Framework { get; set; } = "net6.0";

    public static string Runtime { get; set; } = "RuntimeInformation.RuntimeIdentifier";

    public static string ApplicationName { get; set; } = "CSharpCompiler";

    public static string OutputFileName { get; set; } = "Generated.dll";
#if DEBUG
    public static LogLevel LogLevel { get; set; } = LogLevel.Debug;
#else
    public static LogLevel LogLevel { get; set; } = LogLevel.Info;
#endif
}