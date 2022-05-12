using System.Security.Cryptography;
using System.Text;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler.CompileDirectoryDetecting;

internal class CompileDirectoryDetector : ICompileDirectoryDetector
{
    public CompileDirectoryDetector(ILog logger, string tempDirectoryName, string outputFileName)
    {
        this.logger = logger.ForContext<CompileDirectoryDetector>();
        this.tempDirectoryName = tempDirectoryName;
        this.outputFileName = outputFileName;
    }

    public CompileDirectoryDetectResult Detect(string[] fileContents, bool allowUnsafe)
    {
        var globalTempDirectoryPath = Path.GetTempPath();
        var tempDirectoryPath = Path.Combine(globalTempDirectoryPath, tempDirectoryName);
        var compileDirectoryName = CalculateHashCode(fileContents.Concat(new[] { allowUnsafe.ToString() }).ToArray());
        var compileDirectoryPath = Path.Combine(tempDirectoryPath, compileDirectoryName);
        var dllPath = Path.Combine(compileDirectoryPath, outputFileName);
        if(File.Exists(dllPath))
        {
            logger.Info("Given files have been compiled already, reuse previous build from {dllPath}", dllPath);
            return new CompileDirectoryDetectResult(compileDirectoryPath, dllPath, true, true);
        }

        if(Directory.Exists(compileDirectoryPath))
        {
            logger.Info("Compile directory exists, but entry point file did not found, cleanup it...");
            Directory.Delete(Path.Combine(compileDirectoryPath), true);
            return new CompileDirectoryDetectResult(compileDirectoryPath, dllPath, true);
        }

        Directory.CreateDirectory(compileDirectoryPath);
        return new CompileDirectoryDetectResult(compileDirectoryPath, dllPath);
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