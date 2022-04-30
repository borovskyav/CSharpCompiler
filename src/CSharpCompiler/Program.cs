﻿using System.Runtime.CompilerServices;

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
    private const LogLevel logLevel = LogLevel.Information;
#endif

    public static async Task<int> Main(string[] arguments)
    {
        var consoleLogger = CreateLogger();
        var cancellationToken = ConfigureGracefulStop();
        try
        {
            var runner = new InProcessLibraryRunner();
            var codeRunner = new CSharpSourceCodeRunner(consoleLogger, runner);
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

            consoleLogger.Error(exception.Message);
            return 1;
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
                if (!sigintReceived)
                    source.Cancel();
            };
        return source.Token;
    }
}