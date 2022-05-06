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
            GenerateRuntimeConfig(dllPath);
            return dllPath;
        }
        catch(Exception)
        {
            if(File.Exists(dllPath))
                File.Delete(dllPath);
            throw;
        }
    }

    private void GenerateRuntimeConfig(string dllPath)
        => File.WriteAllText(Path.ChangeExtension(dllPath, runtimeConfigExtension), RuntimeConfigContent);

    private const string runtimeConfigExtension = "runtimeconfig.json";

    private string RuntimeConfigContent => @"{
  ""runtimeOptions"": {
    ""tfm"": ""net6.0"",
    ""framework"": {
      ""name"": ""Microsoft.NETCore.App"",
      ""version"": ""6.0.0""
    }
  }
}";

    private readonly IDiagnosticResultAnalyzer diagnosticResultAnalyzer;

    private readonly CSharpCompilationOptions defaultCompilationOptions =
        new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            .WithOverflowChecks(true)
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithUsings(defaultNamespaces);

    private static readonly IEnumerable<string> defaultNamespaces =
        new[] { "System", "System.IO", "System.Net", "System.Linq", "System.Text", "System.Text.RegularExpressions", "System.Collections.Generic" };

    private readonly ILog logger;
}