using System.Security.Cryptography;
using System.Text;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler.CompileDirectoryManagement;

internal class CompileDirectoryManager : ICompileDirectoryManager
{
    public CompileDirectoryManager(ILog logger, string tempDirectoryName, string outputFileName)
    {
        this.logger = logger.ForContext<CompileDirectoryManager>();
        this.tempDirectoryName = tempDirectoryName;
        this.outputFileName = outputFileName;
    }

    public async Task<CompileDirectoryAcquireResult> AcquireCompileDirectoryAsync(string[] fileContents, bool allowUnsafe, CancellationToken token = default)
    {
        var globalTempDirectoryPath = Path.GetTempPath();
        var tempDirectoryPath = Path.Combine(globalTempDirectoryPath, tempDirectoryName);
        var compileDirectoryName = CalculateHashCode(fileContents.Concat(new[] { allowUnsafe.ToString() }).ToArray());
        var compileDirectoryPath = Path.Combine(tempDirectoryPath, compileDirectoryName);
        var dllPath = Path.Combine(compileDirectoryPath, outputFileName);
        if(File.Exists(dllPath))
        {
            logger.Info("Given files have been compiled already, reuse previous build from {dllPath}", dllPath);
            return new CompileDirectoryAcquireResult(compileDirectoryPath, dllPath, null, true);
        }
        
        Directory.CreateDirectory(compileDirectoryPath);

        var fileLock = await FileLock.CreateAsync(compileDirectoryPath, token);
        try
        {
            if(File.Exists(dllPath))
            {
                logger.Info("Given files have been compiled already, reuse previous build from {dllPath}", dllPath);
                return new CompileDirectoryAcquireResult(compileDirectoryPath, dllPath, fileLock, true);
            }

            DeleteAllFilesInDirectoryExcept(compileDirectoryPath, fileLock.FilePath);
            return new CompileDirectoryAcquireResult(compileDirectoryPath, dllPath, fileLock);
        }
        catch(Exception)
        {
            fileLock.Dispose();
            throw;
        }
    }

    private void DeleteAllFilesInDirectoryExcept(string directoryPath, string excludePaths)
    {
        var directoryInfo = new DirectoryInfo(directoryPath);

        foreach (var file in directoryInfo.GetFiles().Where(x => x.FullName != excludePaths))
            file.Delete();
        foreach (var dir in directoryInfo.GetDirectories().Where(x => x.FullName != excludePaths))
            dir.Delete(true);
    }

    private string CalculateHashCode(IReadOnlyList<string> array)
    {
        using var md5 = MD5.Create();
        var bytes = array.SelectMany(x => md5.ComputeHash(Encoding.UTF8.GetBytes(x))).ToArray();
        return BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
    }

    private readonly ILog logger;
    private readonly string tempDirectoryName;
    private readonly string outputFileName;
}