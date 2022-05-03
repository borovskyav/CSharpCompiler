namespace CSharpCompiler.SyntaxTreeBuilder;

internal interface ISyntaxTreeBuilder
{
    Task<CsharpSyntaxTree> BuildAsync(IReadOnlyList<string> filesPath, CancellationToken token = default);
}