using System.Reflection;

using NuGet.Common;
using NuGet.Frameworks;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler;

internal class CSharpSourceCodeRunner
{
    public CSharpSourceCodeRunner(
        ILog logger,
        ISyntaxTreeBuilder syntaxTreeBuilder,
        ICSharpCommentExtractor cSharpCommentExtractor,
        INugetPackagesDownloader nugetPackagesDownloader,
        INugetPackageLibrariesExtractor nugetPackageLibrariesExtractor,
        IExternalExecutableRunner externalExecutableRunner
    )
    {
        this.syntaxTreeBuilder = syntaxTreeBuilder;
        this.externalExecutableRunner = externalExecutableRunner;
        this.nugetPackageLibrariesExtractor = nugetPackageLibrariesExtractor;
        this.nugetPackagesDownloader = nugetPackagesDownloader;
        this.cSharpCommentExtractor = cSharpCommentExtractor;
        this.logger = logger.ForContext<CSharpSourceCodeRunner>();
    }

    public async Task<int> RunAsync(
        CSharpSourceCodeRunnerData data,
        CancellationToken cancellationToken = default
    )
    {
        if(data.FilesPath.Count == 0)
            throw new ArgumentException("There are no files to compile");

        var unexistFiles = GetUnexistFiles(data.FilesPath);
        if(unexistFiles.Count > 0)
            throw new FileNotFoundException($"Some files not found: {string.Join(Environment.NewLine, unexistFiles)}");

        var syntaxTree = await syntaxTreeBuilder.BuildAsync(data.FilesPath, cancellationToken);

        var dllDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(dllDirectory);

        var packagesFiles = await GetExternalLibrariesAsync(syntaxTree, dllDirectory, cancellationToken);
        
        var compilationResult = RoslynGames.Compile(dllDirectory, syntaxTree.Trees, packagesFiles);
        if(!compilationResult.Success)
            throw new Exception(compilationResult.ToString());
        logger.Info(compilationResult.ToString());

        return await externalExecutableRunner.Run(compilationResult.DllPath, data.Arguments);
    }

    private async Task<IReadOnlyList<string>> GetExternalLibrariesAsync(CsharpSyntaxTree tree, string dllDirectory, CancellationToken token)
    {
        var comments = await cSharpCommentExtractor.ExtractAsync(tree, token);
        var nugetPackages = NugetPackagesParser.Parse(comments);
        if(nugetPackages.Count == 0)
        {
            logger.Info("No included nuget packages, skip downloading step");
            return Array.Empty<string>();
        }
        
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
    private readonly INugetPackagesDownloader nugetPackagesDownloader;
    private readonly INugetPackageLibrariesExtractor nugetPackageLibrariesExtractor;
    private readonly IExternalExecutableRunner externalExecutableRunner;
    private readonly ILog logger;
}