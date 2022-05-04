using Microsoft.CodeAnalysis;

namespace CSharpCompiler.CSharpCompiler;

internal interface ICSharpCompiler
{
    string Compile(
        IReadOnlyList<SyntaxTree> trees,
        IReadOnlyList<string> externalLibs,
        string dllPath,
        bool allowUnsafe,
        CancellationToken token = default
    );
}