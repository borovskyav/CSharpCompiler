namespace CSharpCompiler;

internal class CSharpSourceCodeRunnerData
{
    public CSharpSourceCodeRunnerData(
        IReadOnlyList<string> filesPath,
        IReadOnlyList<string> arguments
    )
    {
        FilesPath = filesPath;
        Arguments = arguments;
    }

    public IReadOnlyList<string> FilesPath { get; }
    public IReadOnlyList<string> Arguments { get; }
}