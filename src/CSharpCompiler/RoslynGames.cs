using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpCompiler;

public class CompilationResult
{
    public CompilationResult(
        bool success,
        IImmutableList<Diagnostic> diagnostics,
        string dllPath
    )
    {
        var errors = new List<Diagnostic>();
        var warnings = new List<Diagnostic>();
        foreach(var diagnostic in diagnostics)
            if(diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                errors.Add(diagnostic);
            else
                warnings.Add(diagnostic);

        Success = success && errors.Count == 0;
        Errors = errors;
        Warnings = warnings;
        DllPath = dllPath;
    }

    public bool Success { get; }
    public IReadOnlyList<Diagnostic> Errors { get; } //todo избавиться от Diagnostic
    public IReadOnlyList<Diagnostic> Warnings { get; } //todo избавиться от Diagnostic
    public string DllPath { get; }

    public override string ToString()
    {
        return (Success, Warnings.Count == 0) switch
            {
                (true, true) => BuildCompilationOutputString(),
                (false, false) => BuildCompilationErrorString() + Environment.NewLine + BuildCompilationWarningsString(),
                (true, false) => BuildCompilationWarningsString() + Environment.NewLine + BuildCompilationOutputString(),
                (false, true) => BuildCompilationOutputString(),
            };
    }

    private string BuildCompilationErrorString()
    {
        return $"Compilation errors:{Environment.NewLine}{string.Join(Environment.NewLine, Errors)}";
    }

    private string BuildCompilationWarningsString()
    {
        return $"Warnings:{Environment.NewLine}{string.Join(Environment.NewLine, Warnings)}";
    }

    private string BuildCompilationOutputString()
    {
        return $"Output: {DllPath}";
    }
}

public class RoslynGames
{
    // todo подумать про настройку дефолтных флагов. Какие ставить? Такие-же как в моем приложении стоят? Какой-то кастом?
    public static CompilationResult Compile(string workingDirectory, IReadOnlyList<SyntaxTree> trees, IReadOnlyList<string> externalLibs)
    {
        var dllName = $"{Guid.NewGuid()}.dll";

        var compilation = CSharpCompilation.Create(
            dllName,
            trees,
            defaultReferences.Concat(externalLibs.Select(x => MetadataReference.CreateFromFile(x))),
            defaultCompilationOptions);

        var dllPath = Path.Combine(workingDirectory, dllName);
        var result = compilation.Emit(dllPath);
        if(!result.Success)
            throw new Exception($"Compilation error:{Environment.NewLine}{string.Join(Environment.NewLine, result.Diagnostics)}");

        return new CompilationResult(result.Success, result.Diagnostics, dllPath);
    }

    private static readonly IEnumerable<string> defaultNamespaces =
        new[] { "System", "System.IO", "System.Net", "System.Linq", "System.Text", "System.Text.RegularExpressions", "System.Collections.Generic" };

    private static readonly IEnumerable<MetadataReference> defaultReferences =
        new[]
            {
                // todo Понять, какие либы подгружать? Такие же как в моем приложении?
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "System.dll")),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "System.Console.dll")),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "System.Core.dll")),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "System.Private.Uri.dll")),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "System.Net.Http.dll")),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "netstandard.dll")),
            };

    private static readonly CSharpCompilationOptions defaultCompilationOptions =
        new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            .WithOverflowChecks(true)
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithAllowUnsafe(true)
            .WithUsings(defaultNamespaces);
}