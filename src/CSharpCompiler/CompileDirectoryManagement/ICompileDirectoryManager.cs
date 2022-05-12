namespace CSharpCompiler.CompileDirectoryManagement;

internal interface ICompileDirectoryManager
{
    Task<CompileDirectoryAcquireResult> AcquireCompileDirectoryAsync(string[] fileContents, bool allowUnsafe, CancellationToken token = default);
}