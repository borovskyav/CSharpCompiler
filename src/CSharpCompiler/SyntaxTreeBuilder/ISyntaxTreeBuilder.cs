namespace CSharpCompiler.SyntaxTreeBuilder;

internal interface ISyntaxTreeBuilder
{
    Task<CsharpSyntaxTree> BuildAndAnalyzeTreeAsync(IReadOnlyList<string> filesPath, CancellationToken token = default);
}