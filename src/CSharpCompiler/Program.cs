using System.Runtime.CompilerServices;

using CSharpCompiler.CompileDirectoryManagement;
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
                new CompileDirectoryManager(logger, ApplicationConstants.ApplicationName, ApplicationConstants.OutputFileName),
                new RoslynSyntaxTreeBuilder(roslynDiagnosticResultAnalyzer),
                new RoslynSyntaxTreeCommentExtractor(),
                new NugetPackagesParser(logger),
                downloader,
                new NugetPackageLibrariesExtractor(logger, ApplicationConstants.Framework),
                new RoslynCSharpCompiler(roslynDiagnosticResultAnalyzer, logger),
                new InProcessExecutableRunner(logger));

            var (data, showHelp) = ConsoleArgumentsParser.Parse(arguments);
            if(showHelp)
            {
                PrintHelp();
                return 0;
            }
            return await codeRunner.RunAsync(data, cancellationToken);
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

    private static void PrintHelp()
    {
        Console.WriteLine(@"CSharp compiler:
Arguments: [Flags | -allowUnsafe] [Files | 1.cs 2.cs 3.cs] -- [Compiled program arguments | 1 2 3]

Additional flags:
    -allowUnsafe: this flag enables compiling in unsafe mode.

Files: A list of all relative paths of the source code files to compile, separated by spaces.
Compiled Program arguments: Command line arguments to be passed to the compiled program.
");
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