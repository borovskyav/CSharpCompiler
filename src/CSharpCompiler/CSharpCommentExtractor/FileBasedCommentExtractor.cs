using CSharpCompiler.SyntaxTreeBuilder;

namespace CSharpCompiler.CSharpCommentExtractor;

internal class FileBasedCommentExtractor : ICSharpCommentExtractor
{
    public async Task<IReadOnlyList<string>> ExtractAsync(CsharpSyntaxTree syntaxTree, CancellationToken token = default)
    {
        var list = new List<string>();
        foreach(var filePath in syntaxTree.FilesPath)
        foreach(var line in await File.ReadAllLinesAsync(filePath, token))
            if(line.Trim().StartsWith("//"))
                list.Add(line.Trim());

        return list;
    }
}