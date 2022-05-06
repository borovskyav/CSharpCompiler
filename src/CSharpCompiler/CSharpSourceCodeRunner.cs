using CSharpCompiler.CompileDirectoryDetecting;
using CSharpCompiler.CSharpCommentExtractor;
using CSharpCompiler.CSharpCompiler;
using CSharpCompiler.ExternalExecutableRunner;
using CSharpCompiler.NugetPackageLibrariesExtracting;
using CSharpCompiler.NugetPackagesDownloader;
using CSharpCompiler.SyntaxTreeBuilder;

using Microsoft.CodeAnalysis;

namespace CSharpCompiler;

internal class CSharpSourceCodeRunner
{
    public CSharpSourceCodeRunner(
        ICompileDirectoryDetector compileDirectoryDetector,
        ISyntaxTreeBuilder syntaxTreeBuilder,
        ICSharpCommentExtractor cSharpCommentExtractor,
        NugetPackagesParser nugetPackagesParser,
        INugetPackagesDownloader nugetPackagesDownloader,
        INugetPackageLibrariesExtractor nugetPackageLibrariesExtractor,
        ICSharpCompiler cSharpCompiler,
        IExternalExecutableRunner externalExecutableRunner
    )
    {
        this.compileDirectoryDetector = compileDirectoryDetector;
        this.syntaxTreeBuilder = syntaxTreeBuilder;
        this.externalExecutableRunner = externalExecutableRunner;
        this.nugetPackageLibrariesExtractor = nugetPackageLibrariesExtractor;
        this.nugetPackagesDownloader = nugetPackagesDownloader;
        this.cSharpCompiler = cSharpCompiler;
        this.cSharpCommentExtractor = cSharpCommentExtractor;
        this.nugetPackagesParser = nugetPackagesParser;
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
        var fileContents = await Task.WhenAll(data.FilesPath.Select(async x => await File.ReadAllTextAsync(x, token)));
        var result = compileDirectoryDetector.Detect(fileContents, data.AllowUnsafe);
        if(result.DllExists)
            return result.DllPath;
        var syntaxTree = syntaxTreeBuilder.BuildAndAnalyzeTreeAsync(fileContents, token);
        var externalLibs = await GetExternalLibrariesAsync(syntaxTree, result.DirectoryPath, token);
        return cSharpCompiler.Compile(syntaxTree, externalLibs, result.DllPath, data.AllowUnsafe, token);
    }

    private async Task<IReadOnlyList<string>> GetExternalLibrariesAsync(IReadOnlyList<SyntaxTree> tree, string dllDirectory, CancellationToken token)
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

    private readonly ICompileDirectoryDetector compileDirectoryDetector;
    private readonly ISyntaxTreeBuilder syntaxTreeBuilder;
    private readonly ICSharpCommentExtractor cSharpCommentExtractor;
    private readonly NugetPackagesParser nugetPackagesParser;
    private readonly INugetPackagesDownloader nugetPackagesDownloader;
    private readonly INugetPackageLibrariesExtractor nugetPackageLibrariesExtractor;
    private readonly ICSharpCompiler cSharpCompiler;
    private readonly IExternalExecutableRunner externalExecutableRunner;
}