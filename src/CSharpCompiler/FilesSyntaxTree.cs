using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpCompiler;

internal class FilesSyntaxTree
{
    public FilesSyntaxTree(IReadOnlyList<string> filesPath)
    {
        this.filesPath = filesPath;
        Trees = new List<SyntaxTree>(filesPath.Count);
        var options = CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
        foreach(var filePath in filesPath)
        {
            var source = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(source, options);
            Trees.Add(tree);
        }
    }

    public List<SyntaxTree> Trees { get; }

    private const LanguageVersion languageVersion = LanguageVersion.Latest;

    public IEnumerable<string> GetAllCommentRows()
    {
        foreach(var filePath in filesPath)
        foreach(var line in File.ReadAllLines(filePath))
        {
            if(line.Trim().StartsWith("//"))
                yield return line;
        }
    }

    private readonly IReadOnlyList<string> filesPath;
}