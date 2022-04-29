namespace CSharpCompiler;

internal class ParseArgumentsResult
{
    public ParseArgumentsResult(
        IReadOnlyList<string> filesPath,
        string programArguments
    )
    {
        FilesPath = filesPath;
        ProgramArguments = programArguments;
    }

    public IReadOnlyList<string> FilesPath { get; }
    public string ProgramArguments { get; }
}