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

    public async Task<int> RunAsync(
        CSharpSourceCodeRunnerData data,
        CancellationToken token = default
    )
    {
        if(data.FilesPath.Count == 0)
            throw new ArgumentException("There are no files to compile");

        var unexistFiles = GetUnexistFiles(data.FilesPath);
        if(unexistFiles.Count > 0)
            throw new FileNotFoundException($"Some files not found: {string.Join(Environment.NewLine, unexistFiles)}");

        var syntaxTree = await syntaxTreeBuilder.BuildAndAnalyzeAsync(data.FilesPath, token);

        var dllDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(dllDirectory);

        var externalLibs = await GetExternalLibrariesAsync(syntaxTree, dllDirectory, token);
        var dllPath = cSharpCompiler.Compile(syntaxTree.Trees, externalLibs, dllDirectory, data.AllowUnsafe, token);
        return await externalExecutableRunner.RunAsync(dllPath, data.ProcessArguments);
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