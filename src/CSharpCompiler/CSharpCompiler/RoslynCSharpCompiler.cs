using System.Reflection;

using CSharpCompiler.SyntaxTreeBuilder;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler.CSharpCompiler;

internal class RoslynCSharpCompiler : ICSharpCompiler
{
    public RoslynCSharpCompiler(IDiagnosticResultAnalyzer diagnosticResultAnalyzer, ILog logger)
    {
        this.diagnosticResultAnalyzer = diagnosticResultAnalyzer;
        this.logger = logger.ForContext<RoslynCSharpCompiler>();
    }

    public string Compile(
        IReadOnlyList<SyntaxTree> trees,
        IReadOnlyList<string> externalLibs,
        string dllPath,
        bool allowUnsafe,
        CancellationToken token = default
    )
    {
        var compilationOptions = defaultCompilationOptions.WithAllowUnsafe(allowUnsafe);

        var references = externalLibs.Select(x => MetadataReference.CreateFromFile(x));
        var dllName = Path.GetFileName(dllPath);
        var compilation = CSharpCompilation.Create(
            dllName,
            trees,
            references,
            compilationOptions);

        foreach(var packagesFile in externalLibs)
            Assembly.LoadFrom(packagesFile);

        try
        {
            logger.Info("Start compilation");
            var result = compilation.Emit(dllPath, cancellationToken: token);
            logger.Info("Compilation completed, output file: {output}", dllPath);
        
            diagnosticResultAnalyzer.Analyze(result.Diagnostics, result.Success, showWarningsOnSuccess: true);
            CopyRuntimeConfig(dllPath);
            return dllPath;
        }
        catch(Exception)
        {
            if (File.Exists(dllPath))
                File.Delete(dllPath);
            throw;
        }
    }

    private void CopyRuntimeConfig(string dllPath)
    {
        const string runtimeconfig = "runtimeconfig.json";
        var outputRuntimeConfigPath = Path.ChangeExtension(dllPath, runtimeconfig);
        var currentRuntimeConfigPath = Path.ChangeExtension(typeof(Program).Assembly.Location, runtimeconfig);

        logger.Info($"Copying current {runtimeconfig} to {outputRuntimeConfigPath}");
        File.Copy(currentRuntimeConfigPath, outputRuntimeConfigPath);
    }

    private readonly IDiagnosticResultAnalyzer diagnosticResultAnalyzer;

    private CSharpCompilationOptions defaultCompilationOptions =
        new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            .WithOverflowChecks(true)
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithUsings(defaultNamespaces);

    private static readonly IEnumerable<string> defaultNamespaces =
        new[] { "System", "System.IO", "System.Net", "System.Linq", "System.Text", "System.Text.RegularExpressions", "System.Collections.Generic" };

    private readonly ILog logger;
}