namespace CSharpCompiler;

internal interface ICSharpCommentExtractor
{
    Task<IReadOnlyList<string>> ExtractAsync(CsharpSyntaxTree syntaxTree, CancellationToken token = default);
}