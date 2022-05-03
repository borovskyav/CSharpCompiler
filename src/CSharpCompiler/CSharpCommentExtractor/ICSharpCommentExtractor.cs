using CSharpCompiler.SyntaxTreeBuilder;

namespace CSharpCompiler.CSharpCommentExtractor;

internal interface ICSharpCommentExtractor
{
    Task<IReadOnlyList<string>> ExtractAsync(CsharpSyntaxTree syntaxTree, CancellationToken token = default);
}