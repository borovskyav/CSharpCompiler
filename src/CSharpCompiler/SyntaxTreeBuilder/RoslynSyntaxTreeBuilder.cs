using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpCompiler.SyntaxTreeBuilder;

internal class RoslynSyntaxTreeBuilder : ISyntaxTreeBuilder
{
    public RoslynSyntaxTreeBuilder(IDiagnosticResultAnalyzer diagnosticResultAnalyzer)
    {
        this.diagnosticResultAnalyzer = diagnosticResultAnalyzer;
    }

    public SyntaxTree[] BuildAndAnalyzeTreeAsync(IReadOnlyList<string> fileContents, CancellationToken token)
    {
        var trees = fileContents.Select(fileContent => CSharpSyntaxTree.ParseText(fileContent, options, cancellationToken: token)).ToArray();
        diagnosticResultAnalyzer.Analyze(trees.SelectMany(tree => tree.GetDiagnostics()).ToList(), true, false);
        return trees;
    }

    private const LanguageVersion languageVersion = LanguageVersion.Latest;
    private readonly IDiagnosticResultAnalyzer diagnosticResultAnalyzer;

    private readonly CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
}