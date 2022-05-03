using Microsoft.CodeAnalysis;

namespace CSharpCompiler.SyntaxTreeBuilder;

internal class CsharpSyntaxTree
{
    public CsharpSyntaxTree(
        IReadOnlyList<SyntaxTree> trees,
        IReadOnlyList<string> filesPath
    )
    {
        Trees = trees;
        FilesPath = filesPath;
    }

    public IReadOnlyList<SyntaxTree> Trees { get; }
    public IReadOnlyList<string> FilesPath { get; }
}