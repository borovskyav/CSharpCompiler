using Microsoft.CodeAnalysis;

namespace CSharpCompiler.CSharpCommentExtractor;

internal interface ICSharpCommentExtractor
{
    Task<IReadOnlyList<string>> ExtractAsync(IReadOnlyList<SyntaxTree> syntaxTrees, CancellationToken token = default);
}