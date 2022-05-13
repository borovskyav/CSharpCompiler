namespace CSharpCompiler.CompileDirectoryManagement;

internal class FileLock : IDisposable
{
    private FileLock(string fileDirectory)
    {
        FilePath = Path.Combine(fileDirectory, lockFileName);
        fileStream = File.Open(FilePath, FileMode.Create);
    }

    public string FilePath { get; }

    private const int maxAttempts = 60000 / delayInMs;
    private const int delayInMs = 10;
    private const string lockFileName = ".lock";

    public void Dispose()
    {
        fileStream.Close();
        try
        {
            File.Delete(FilePath);
        }
        catch
        { // ignored
        }
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
            catch (Exception e) when (e is UnauthorizedAccessException or IOException)
            {
                await Task.Delay(delayInMs, token);
            }

            attempts++;
        }

        throw new Exception($"Can not create lock file by path: {filePath}, exit from program");
    }

    private readonly FileStream fileStream;
}