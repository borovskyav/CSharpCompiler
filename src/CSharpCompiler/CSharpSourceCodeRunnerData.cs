namespace CSharpCompiler;

internal class CSharpSourceCodeRunnerData
{
    public CSharpSourceCodeRunnerData(
        IReadOnlyList<string> filesPath,
        IReadOnlyList<string> processArguments,
        bool allowUnsafe
    )
    {
        FilesPath = filesPath;
        ProcessArguments = processArguments;
        AllowUnsafe = allowUnsafe;
    }

    public IReadOnlyList<string> FilesPath { get; }
    public IReadOnlyList<string> ProcessArguments { get; }

    public bool AllowUnsafe { get; set; }
}