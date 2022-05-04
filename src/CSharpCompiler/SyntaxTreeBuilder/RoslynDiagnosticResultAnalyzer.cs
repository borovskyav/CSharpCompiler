using System.Text;

using Microsoft.CodeAnalysis;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler.SyntaxTreeBuilder;

internal class RoslynDiagnosticResultAnalyzer : IDiagnosticResultAnalyzer
{
    public RoslynDiagnosticResultAnalyzer(ILog logger)
    {
        this.logger = logger.ForContext<RoslynDiagnosticResultAnalyzer>();
    }

    public void Analyze(IReadOnlyList<Diagnostic> diagnostics, bool success, bool showWarningsOnSuccess)
    {
        var isDiagnosticsHasErrors = !success || diagnostics.Any(IsDiagnosticError);
        if(!isDiagnosticsHasErrors && (!showWarningsOnSuccess || diagnostics.Count == 0))
            return;

        LogCompilationResult(diagnostics, isDiagnosticsHasErrors);
        if(isDiagnosticsHasErrors)
            throw new Exception("Compilation failed");
    }

    private void LogCompilationResult(IReadOnlyList<Diagnostic> diagnostics, bool compilationError)
    {
        var stringBuilder = new StringBuilder($"Compilation completed with {(compilationError ? "errors" : "warnings")}:");
        stringBuilder.AppendLine();
        var grouping = diagnostics.GroupBy(IsDiagnosticError).OrderBy(x => x.Key);
        foreach(var group in grouping)
        {
            var result = group.Key switch
                {
                    true => "Errors:",
                    false => "Warnings:",
                };
            stringBuilder.Append(result);
            foreach(var diagnostic in group.Select(x => x))
            {
                stringBuilder.AppendLine();
                stringBuilder.Append($"\t{diagnostic}:");
            }
        }

        if(compilationError)
            logger.Error(stringBuilder.ToString());
        else
            logger.Info(stringBuilder.ToString());
    }

    private bool IsDiagnosticError(Diagnostic diagnostic)
        => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;

    private readonly ILog logger;
}