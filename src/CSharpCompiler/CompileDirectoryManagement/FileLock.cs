using System.Runtime.InteropServices;

namespace CSharpCompiler.CompileDirectoryManagement;

internal class FileLock : IDisposable
{
    private FileLock(string fileDirectory)
    {
        FilePath = Path.Combine(fileDirectory, lockFileName);
        fileStream = new FileStream(FilePath,
                                    FileMode.OpenOrCreate,
                                    FileAccess.ReadWrite,
                                    FileShare.None,
                                    32,
                                    options: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? FileOptions.DeleteOnClose : FileOptions.None
        );
    }

    public string FilePath { get; }

    private const int maxAttempts = 60000 / delayInMs;
    private const int delayInMs = 10;
    private const string lockFileName = ".lock";

    public void Dispose()
    {
        fileStream.Close();
    }

    public static async Task<FileLock> CreateAsync(string filePath, CancellationToken token = default)
    {
        var attempts = 0;
        while(attempts < maxAttempts)
        {
            try
            {
                return new FileLock(filePath);
            }
            catch(Exception e) when(e is UnauthorizedAccessException or IOException)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(delayInMs, token);
            }

            attempts++;
        }

        throw new Exception($"Can not create lock file by path: {filePath}");
    }

    private readonly FileStream fileStream;
}