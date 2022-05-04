namespace CSharpCompiler.CompileDirectoryDetecting;

internal class CompileDirectoryDetectResult
{
    public CompileDirectoryDetectResult(
        string directoryPath,
        string dllPath,
        bool directoryExists = false,
        bool dllExists = false
    )
    {
        DirectoryPath = directoryPath;
        DllPath = dllPath;
        DirectoryExists = directoryExists;
        DllExists = dllExists;
    }

    public string DirectoryPath { get; }
    public string DllPath { get; }
    public bool DirectoryExists { get; }
    public bool DllExists { get; }
}