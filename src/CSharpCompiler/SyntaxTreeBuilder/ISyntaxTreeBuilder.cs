namespace CSharpCompiler.SyntaxTreeBuilder;

internal interface ISyntaxTreeBuilder
{
    Task<CsharpSyntaxTree> BuildAndAnalyzeAsync(IReadOnlyList<string> filesPath, CancellationToken token = default);
}