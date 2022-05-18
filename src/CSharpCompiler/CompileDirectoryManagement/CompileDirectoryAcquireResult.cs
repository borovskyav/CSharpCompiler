namespace CSharpCompiler.CompileDirectoryManagement;

internal class CompileDirectoryAcquireResult: IDisposable
{
    public CompileDirectoryAcquireResult(
        string directoryPath,
        string dllPath,
        FileLock? fileLock,
        bool dllExists = false
    )
    {
        DirectoryPath = directoryPath;
        DllPath = dllPath;
        FileLock = fileLock;
        DllExists = dllExists;
    }

    public string DirectoryPath { get; }
    public string DllPath { get; }
    public FileLock? FileLock { get; }
    public bool DllExists { get; }

    public void Dispose()
    {
        FileLock?.Dispose();
    }
}