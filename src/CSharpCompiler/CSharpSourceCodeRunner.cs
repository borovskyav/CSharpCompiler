using System.Security.Cryptography;
using System.Text;

using CSharpCompiler.CSharpCommentExtractor;
using CSharpCompiler.CSharpCompiler;
using CSharpCompiler.ExternalExecutableRunner;
using CSharpCompiler.NugetPackageLibrariesExtractor;
using CSharpCompiler.NugetPackagesDownloader;
using CSharpCompiler.SyntaxTreeBuilder;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler;

internal class CSharpSourceCodeRunner
{
    public CSharpSourceCodeRunner(
        ILog logger,
        ISyntaxTreeBuilder syntaxTreeBuilder,
        ICSharpCommentExtractor cSharpCommentExtractor,
        NugetPackagesParser nugetPackagesParser,
        INugetPackagesDownloader nugetPackagesDownloader,
        INugetPackageLibrariesExtractor nugetPackageLibrariesExtractor,
        ICSharpCompiler cSharpCompiler,
        IExternalExecutableRunner externalExecutableRunner
    )
    {
        this.syntaxTreeBuilder = syntaxTreeBuilder;
        this.externalExecutableRunner = externalExecutableRunner;
        this.nugetPackageLibrariesExtractor = nugetPackageLibrariesExtractor;
        this.nugetPackagesDownloader = nugetPackagesDownloader;
        this.cSharpCompiler = cSharpCompiler;
        this.cSharpCommentExtractor = cSharpCommentExtractor;
        this.nugetPackagesParser = nugetPackagesParser;
        this.logger = logger.ForContext<CSharpSourceCodeRunner>();
    }

    public async Task<int> RunAsync(CSharpSourceCodeRunnerData data, CancellationToken token = default)
    {
        if(data.FilesPath.Count == 0)
            throw new ArgumentException("There are no files to compile");

        var unexistFiles = GetUnexistFiles(data.FilesPath);
        if(unexistFiles.Count > 0)
            throw new FileNotFoundException($"Some files not found: {string.Join(Environment.NewLine, unexistFiles)}");

        var dllPath = await BuildSourcesAsync(data, token);
        return await externalExecutableRunner.RunAsync(dllPath, data.ProcessArguments);
    }

    private async Task<string> BuildSourcesAsync(CSharpSourceCodeRunnerData data, CancellationToken token)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "CSharpCompiler");
        var folderHashCodeSources = data.FilesPath.Select(Path.GetFileName).Where(x => !string.IsNullOrEmpty(x));
        var directoryName = CalculateHashCode(folderHashCodeSources.ToArray()!);
        var directoryPath = Path.Combine(tempDirectory, directoryName);
        var readFilesTasks = data.FilesPath.Select(async x => await File.ReadAllTextAsync(x, token));
        var files = await Task.WhenAll(readFilesTasks);
        var dllName = CalculateHashCode(files.Concat(new []{data.AllowUnsafe.ToString()}).ToArray());
        var dllPath = Path.Combine(directoryPath, dllName + ".dll");

        if(File.Exists(dllPath))
        {
            logger.Info("Given files have been compiled already, reuse previous build from {dllPath}", dllPath);
            return dllPath;
        }
        
        var directoryExists = Directory.Exists(directoryPath);
        if(directoryExists)
        {
            logger.Info("Directory exists, but the functionality of partial replacement of libraries has not yet been implemented. Delete the directory.");
            Directory.Delete(Path.Combine(directoryPath), true);
        }
        
        Directory.CreateDirectory(directoryPath);
        var syntaxTree = await syntaxTreeBuilder.BuildAndAnalyzeTreeAsync(data.FilesPath, token);

        var externalLibs = await GetExternalLibrariesAsync(syntaxTree, directoryPath, token);
        return cSharpCompiler.Compile(syntaxTree.Trees, externalLibs, dllPath, data.AllowUnsafe, token);
    }

    private string CalculateHashCode(IReadOnlyList<string> array)
    {
        using var md5 = MD5.Create();
        var encoder = Encoding.UTF8;
        var bytes = array.Aggregate(new List<byte>(),
                                    (total, next) =>
                                        {
                                            total.AddRange(md5.ComputeHash(encoder.GetBytes(next)));
                                            return total;
                                        });
        return BitConverter.ToString(md5.ComputeHash(bytes.ToArray())).Replace("-", string.Empty).ToLowerInvariant();
    }

    private async Task<IReadOnlyList<string>> GetExternalLibrariesAsync(CsharpSyntaxTree tree, string dllDirectory, CancellationToken token)
    {
        var comments = await cSharpCommentExtractor.ExtractAsync(tree, token);
        var nugetPackages = nugetPackagesParser.Parse(comments);

        var packages = await nugetPackagesDownloader.DownloadAsync(nugetPackages, token);
        var libraryFilesPath = await nugetPackageLibrariesExtractor.ExtractAsync(packages, dllDirectory, token);
        return libraryFilesPath.Where(x => Path.GetExtension(x) == ".dll").ToArray();
    }

    private IReadOnlyList<string> GetUnexistFiles(IReadOnlyList<string> filesPath)
    {
        return filesPath
               .Select(filePath => Path.IsPathRooted(filePath) ? filePath : Path.GetFullPath(filePath))
               .Where(filePath => !File.Exists(filePath))
               .ToList();
    }

    private readonly ISyntaxTreeBuilder syntaxTreeBuilder;
    private readonly ICSharpCommentExtractor cSharpCommentExtractor;
    private readonly NugetPackagesParser nugetPackagesParser;
    private readonly INugetPackagesDownloader nugetPackagesDownloader;
    private readonly INugetPackageLibrariesExtractor nugetPackageLibrariesExtractor;
    private readonly ICSharpCompiler cSharpCompiler;
    private readonly IExternalExecutableRunner externalExecutableRunner;
    private readonly ILog logger;
}
