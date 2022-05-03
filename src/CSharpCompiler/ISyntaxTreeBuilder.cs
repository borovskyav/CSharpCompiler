namespace CSharpCompiler;

internal interface ISyntaxTreeBuilder
{
    Task<CsharpSyntaxTree> BuildAsync(IReadOnlyList<string> filesPath, CancellationToken token = default);
}