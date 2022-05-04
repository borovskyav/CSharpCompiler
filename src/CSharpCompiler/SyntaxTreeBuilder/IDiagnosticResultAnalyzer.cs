using Microsoft.CodeAnalysis;

namespace CSharpCompiler.SyntaxTreeBuilder;

internal interface IDiagnosticResultAnalyzer
{
    void Analyze(IReadOnlyList<Diagnostic> diagnostics, bool success, bool showWarningsOnSuccess);
}