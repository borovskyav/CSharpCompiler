using System.Runtime.CompilerServices;

using CSharpCompiler.CSharpCommentExtractor;
using CSharpCompiler.CSharpCompiler;
using CSharpCompiler.ExternalExecutableRunner;
using CSharpCompiler.NugetPackagesDownloader;
using CSharpCompiler.SyntaxTreeBuilder;

using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using Vostok.Logging.Formatting;

[assembly: InternalsVisibleTo("CSharpCompilerTests")]

namespace CSharpCompiler;

internal class Program
{
#if DEBUG
    private const LogLevel logLevel = LogLevel.Debug;
#else
    private const LogLevel logLevel = LogLevel.Info;
#endif

    public static async Task<int> Main(string[] arguments)
    {
        var logger = CreateLogger();
        const string frameworkVersion = "net6.0";
        var downloader = new NugetClientBasedPackagesDownloader(logger, frameworkVersion);
        var cancellationToken = ConfigureGracefulStop();
        try
        {
            var roslynDiagnosticResultAnalyzer = new RoslynDiagnosticResultAnalyzer(logger);
            var codeRunner = new CSharpSourceCodeRunner(
                logger,
                new RoslynSyntaxTreeBuilder(roslynDiagnosticResultAnalyzer),
                new RoslynSyntaxTreeCommentExtractor(),
                new NugetPackagesParser(logger),
                downloader,
                new NugetPackageLibrariesExtractor.NugetPackageLibrariesExtractor(logger, frameworkVersion),
                new RoslynCSharpCompiler(roslynDiagnosticResultAnalyzer, logger),
                new InProcessExecutableRunner(logger));

            var parseResult = ConsoleArgumentsParser.Parse(arguments);
            return await codeRunner.RunAsync(parseResult, cancellationToken);
        }
        catch(Exception exception)
        {
#if DEBUG
            throw;
#endif
            if(string.IsNullOrEmpty(exception.Message))
                throw;

            logger.Error(exception.Message);
            return 1;
        }
        finally
        {
            downloader.Dispose();
        }
    }

    private static ILog CreateLogger()
    {
        const string logFormat = "{Timestamp:hh:mm:ss.fff} {Level} {Prefix}{Message}{NewLine}";
        var logSettings = new ConsoleLogSettings { OutputTemplate = OutputTemplate.Parse(logFormat) };
        return new SynchronousConsoleLog(logSettings).WithMinimumLevel(logLevel);
    }

    private static CancellationToken ConfigureGracefulStop()
    {
        var source = new CancellationTokenSource();
        var sigintReceived = false;
        Console.CancelKeyPress += (_, ea) =>
            {
                ea.Cancel = true;
                source.Cancel();
                sigintReceived = true;
            };

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                if(!sigintReceived)
                    source.Cancel();
            };
        return source.Token;
    }
}