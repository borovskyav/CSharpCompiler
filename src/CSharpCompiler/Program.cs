using System.Runtime.CompilerServices;

using CSharpCompiler.CompileDirectoryDetecting;
using CSharpCompiler.CSharpCommentExtractor;
using CSharpCompiler.CSharpCompiler;
using CSharpCompiler.ExternalExecutableRunner;
using CSharpCompiler.NugetPackageLibrariesExtracting;
using CSharpCompiler.NugetPackagesDownloader;
using CSharpCompiler.SyntaxTreeBuilder;

using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using Vostok.Logging.Formatting;

[assembly: InternalsVisibleTo("CSharpCompilerTests")]

namespace CSharpCompiler;

internal class Program
{
    public static async Task<int> Main(string[] arguments)
    {
        ILog? logger = null;
        NugetClientBasedPackagesDownloader? downloader = null;
        try
        {
            logger = CreateLogger();
            downloader = new NugetClientBasedPackagesDownloader(logger, ApplicationConstants.Framework, ApplicationConstants.Runtime);
            var cancellationToken = ConfigureGracefulStop();
            var roslynDiagnosticResultAnalyzer = new RoslynDiagnosticResultAnalyzer(logger);
            var codeRunner = new CSharpSourceCodeRunner(
                new CompileDirectoryDetector(logger, ApplicationConstants.ApplicationName, ApplicationConstants.OutputFileName),
                new RoslynSyntaxTreeBuilder(roslynDiagnosticResultAnalyzer),
                new RoslynSyntaxTreeCommentExtractor(),
                new NugetPackagesParser(logger),
                downloader,
                new NugetPackageLibrariesExtractor(logger, ApplicationConstants.Framework),
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

            if(logger != null)
                logger.Error(exception.Message);
            else
                Console.WriteLine(exception.Message);
            return 1;
        }
        finally
        {
            downloader?.Dispose();
        }
    }

    private static ILog CreateLogger()
    {
        const string logFormat = "{Timestamp:hh:mm:ss.fff} {Level} {Prefix}{Message}{NewLine}";
        var logSettings = new ConsoleLogSettings { OutputTemplate = OutputTemplate.Parse(logFormat) };
        return new SynchronousConsoleLog(logSettings).WithMinimumLevel(ApplicationConstants.LogLevel);
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