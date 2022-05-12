namespace CSharpCompiler.CompileDirectoryDetecting;

internal class CompileDirectoryDetectResult
{
    public CompileDirectoryDetectResult(
        string directoryPath,
        string dllPath,
        bool dllExists = false
    )
    {
        DirectoryPath = directoryPath;
        DllPath = dllPath;
        DllExists = dllExists;
    }

    public string DirectoryPath { get; }
    public string DllPath { get; }
    public bool DllExists { get; }
}