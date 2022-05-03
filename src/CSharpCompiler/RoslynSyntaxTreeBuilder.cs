using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpCompiler;

internal class RoslynSyntaxTreeBuilder : ISyntaxTreeBuilder
{
    public async Task<CsharpSyntaxTree> BuildAsync(IReadOnlyList<string> filesPath, CancellationToken token)
    {
        var tasks = filesPath.Select(x => ParseFile(x, token));
        var result = await Task.WhenAll(tasks);
        var trees = result.Select(x => x.Item1).ToList();
        var files = result.Select(x => x.Item2).ToList();
        return new CsharpSyntaxTree(trees, files);
    }

    private async Task<(SyntaxTree, string)> ParseFile(string filePath, CancellationToken token)
    {
        var source = await File.ReadAllTextAsync(filePath, token);
        return (CSharpSyntaxTree.ParseText(source, options, cancellationToken: token), filePath);
    }

    private const LanguageVersion languageVersion = LanguageVersion.Latest;

    private readonly CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
}