using Microsoft.CodeAnalysis;

namespace CSharpCompiler;

internal interface ICSharpCompiler
{
    string Compile(IReadOnlyList<SyntaxTree> trees, IReadOnlyList<string> externalLibs, string workingDirectory, CancellationToken token = default);
}