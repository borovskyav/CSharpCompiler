using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler;

// todo подумать про настройку дефолтных флагов. Какие ставить? Такие-же как в моем приложении стоят? Какой-то кастом?
// todo Понять, какие либы подгружать? Такие же как в моем приложении?
internal class RoslynCSharpCompiler : ICSharpCompiler
{
    public RoslynCSharpCompiler(ILog logger)
    {
        this.logger = logger.ForContext<RoslynCSharpCompiler>();
    }

    public string Compile(
        IReadOnlyList<SyntaxTree> trees,
        IReadOnlyList<string> externalLibs,
        string workingDirectory,
        CancellationToken token = default
    )
    {
        var dllName = $"{Guid.NewGuid()}.dll";

        var compilation = CSharpCompilation.Create(
            dllName,
            trees,
            defaultReferences.Concat(externalLibs.Select(x => MetadataReference.CreateFromFile(x))),
            defaultCompilationOptions);

        foreach(var packagesFile in externalLibs)
            Assembly.LoadFrom(packagesFile);

        logger.Info("Start compilation");

        var dllPath = Path.Combine(workingDirectory, dllName);
        var result = compilation.Emit(dllPath, cancellationToken: token);

        var compilationError = !result.Success || result.Diagnostics.Any(IsDiagnosticError);
        LogCompilationResult(result, compilationError);
        if(compilationError)
            throw new Exception("Compilation failed");

        return dllPath;
    }

    private void LogCompilationResult(EmitResult emitResult, bool compilationError)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Compilation has been finished:");
        var grouping = emitResult
                       .Diagnostics
                       .GroupBy(IsDiagnosticError)
                       .OrderBy(x => x.Key);
        foreach(var group in grouping)
        {
            var result = group.Key switch
                {
                    true => "Errors:",
                    false => "Warnings:",
                };
            stringBuilder.AppendLine(result);
            foreach(var diagnostic in group.Select(x => x))
                stringBuilder.AppendLine($"\t{diagnostic}:");
        }

        if(compilationError)
            logger.Error(stringBuilder.ToString());
        else
            logger.Info(stringBuilder.ToString());
    }

    private bool IsDiagnosticError(Diagnostic diagnostic)
        => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;

    private readonly IEnumerable<MetadataReference> defaultReferences =
        new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "System.dll")),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "System.Console.dll")),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "System.Core.dll")),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "System.Private.Uri.dll")),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "System.Net.Http.dll")),
                MetadataReference.CreateFromFile(Path.Combine(typeof(object).Assembly.Location, "..", "netstandard.dll")),
            };

    private readonly CSharpCompilationOptions defaultCompilationOptions =
        new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            .WithOverflowChecks(true)
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithAllowUnsafe(true)
            .WithUsings(defaultNamespaces);

    private static readonly IEnumerable<string> defaultNamespaces =
        new[] { "System", "System.IO", "System.Net", "System.Linq", "System.Text", "System.Text.RegularExpressions", "System.Collections.Generic" };

    private readonly ILog logger;
}