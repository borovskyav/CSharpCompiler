using Microsoft.CodeAnalysis;

namespace CSharpCompiler.SyntaxTreeBuilder;

internal interface ISyntaxTreeBuilder
{
    SyntaxTree[] BuildAndAnalyzeTreeAsync(IReadOnlyList<string> fileContents, CancellationToken token = default);
}