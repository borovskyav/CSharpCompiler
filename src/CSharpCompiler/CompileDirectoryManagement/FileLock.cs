namespace CSharpCompiler.CompileDirectoryManagement;

internal class FileLock : IDisposable
{
    private FileLock(string fileDirectory)
    {
        FilePath = Path.Combine(fileDirectory, lockFileName);
        fileStream = File.Open(FilePath, FileMode.Create);
    }

    public string FilePath { get; }

    private const int maxAttempts = 60;
    private const int delayInMs = 1000;
    private const string lockFileName = ".lock";

    public void Dispose()
    {
        fileStream.Close();
        File.Delete(FilePath);
    }

    public static async Task<FileLock> CreateAsync(string filePath, CancellationToken token = default)
    {
        var attempts = 0;
        while(attempts < maxAttempts)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                return new FileLock(filePath);
            }
            catch(IOException)
            {
                await Task.Delay(delayInMs, token);
            }

            attempts++;
        }

        throw new Exception($"Can not create lock file by path: {filePath}, exit from program");
    }

    private readonly FileStream fileStream;
}