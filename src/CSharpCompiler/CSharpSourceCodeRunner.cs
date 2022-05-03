using System.Reflection;
using System.Text;

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
        IExternalExecutableRunner externalExecutableRunner
    )
    {
        this.syntaxTreeBuilder = syntaxTreeBuilder;
        this.externalExecutableRunner = externalExecutableRunner;
        this.cSharpCommentExtractor = cSharpCommentExtractor;
        this.logger = logger.ForContext<CSharpSourceCodeRunner>();
    }

    public async Task<int> RunAsync(
        CSharpSourceCodeRunnerData data,
        CancellationToken cancellationToken = default
    )
    {
        // todo прибраться в выводах файлов на консоль
        if(data.FilesPath.Count == 0)
            throw new ArgumentException("There are no files to compile");

        var unexistFiles = GetUnexistFiles(data.FilesPath);
        if(unexistFiles.Count > 0)
            throw new FileNotFoundException($"Some files not found: {string.Join(Environment.NewLine, unexistFiles)}");

        var syntaxTree = await syntaxTreeBuilder.BuildAsync(data.FilesPath, cancellationToken);

        var dllDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(dllDirectory);

        var packagesFiles = await GetExternalLibrariesAsync(syntaxTree, dllDirectory, cancellationToken);

        foreach(var packagesFile in packagesFiles)
            Assembly.LoadFrom(packagesFile);

        var compilationResult = RoslynGames.Compile(dllDirectory, syntaxTree.Trees, packagesFiles);
        if(!compilationResult.Success)
            throw new Exception(compilationResult.ToString());
        logger.Info(compilationResult.ToString());

        return await externalExecutableRunner.Run(compilationResult.DllPath, data.Arguments);
    }

    private IReadOnlyList<string> GetUnexistFiles(IReadOnlyList<string> filesPath)
    {
        return filesPath
               .Select(filePath => Path.IsPathRooted(filePath) ? filePath : Path.GetFullPath(filePath))
               .Where(filePath => !File.Exists(filePath))
               .ToList();
    }

    private async Task<IReadOnlyList<string>> GetExternalLibrariesAsync(CsharpSyntaxTree tree, string dllDirectory, CancellationToken token)
    {
        var comments = await cSharpCommentExtractor.ExtractAsync(tree, token);
        var nugetPackages = NugetPackagesParser.Parse(comments);
        if(nugetPackages.Count != 0)
        {
            var packages = await new NugetPackagesDownloader().DownloadAsync(nugetPackages, token);
            if(packages.Any(x => !x.Found))
            {
                LogNotFoundPackages(packages);
                Environment.Exit(1);
            }

            LogDownloadedPackages(packages);
            return (await GetPackagesFiles(packages, dllDirectory, token))
                   .Where(x => Path.GetExtension(x) == ".dll")
                   .ToArray();
        }

        logger.Info("No included nuget packages, skip downloading step...");
        return Array.Empty<string>();
    }

    private async Task<IReadOnlyList<string>> GetPackagesFiles(IReadOnlyList<DownloadPackageResult> packages, string dllDirectory, CancellationToken token)
    {
        var myFramework = NuGetFramework.Parse("net6.0");
        var list = new List<string>();
        foreach(var package in packages)
        {
            var nugetResult = package.NugetResult!;
            var supportedFrameworks = (await nugetResult.PackageReader.GetSupportedFrameworksAsync(token)).ToArray();
            var packageFramework = supportedFrameworks.FirstOrDefault(x => DefaultCompatibilityProvider.Instance.IsCompatible(myFramework, x));
            if(packageFramework == null)
                throw new Exception(
                    $"Compatible framework for package {package.PackageId} not found. Available frameworks: {string.Join(", ", supportedFrameworks.Select(x => x.ToString()))}"); //todo text
            logger.Debug("Use Framework {packageFramework} for Package: {packageId}", packageFramework, package.PackageId);

            var targetFrameworkFiles = (await nugetResult
                                              .PackageReader
                                              .GetLibItemsAsync(token))
                .First(x => x.TargetFramework == packageFramework);

            if(targetFrameworkFiles == null)
                throw new Exception("targetFrameworkFiles");

            // todo caching (??)
            list.AddRange(await nugetResult.PackageReader.CopyFilesAsync(
                              dllDirectory,
                              targetFrameworkFiles.Items,
                              ExtractFile,
                              NullLogger.Instance,
                              token
                          ));
        }

        return list;
    }

    public void LogDownloadedPackages(IReadOnlyList<DownloadPackageResult> packages)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append($"Packages downloaded: {packages.Count}");
        var dict = packages
                   .GroupBy(x => x.FromCache)
                   .ToDictionary(x => x.Key);
        if(dict.ContainsKey(true))
        {
            stringBuilder.AppendLine();
            stringBuilder.Append($"From cache:{Environment.NewLine}"
                                 + $"{string.Join(Environment.NewLine, dict[true].Select(x => $"{x.PackageId}: {x.Version}"))}");
        }

        if(dict.ContainsKey(false))
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"From remote:{Environment.NewLine}"
                                     + $"{string.Join(Environment.NewLine, dict[false].Select(x => $"{x.PackageId}: {x.Version}"))}");
        }

        logger.Info(stringBuilder.ToString());
    }

    public void LogNotFoundPackages(IReadOnlyList<DownloadPackageResult> packages)
    {
        var notFound = packages.Where(x => !x.Found);
        logger.Info($"Error when download packages, some packages not found:{Environment.NewLine}"
                    + $"{string.Join(Environment.NewLine + "\t", notFound.Select(x => $"{x.PackageId}: {x.Version}"))}");
    }

    private static string ExtractFile(string sourcePath, string targetPath, Stream sourceStream)
    {
        using var targetStream = File.OpenWrite(targetPath);
        sourceStream.CopyTo(targetStream);
        return targetPath;
    }

    private readonly ISyntaxTreeBuilder syntaxTreeBuilder;
    private readonly ICSharpCommentExtractor cSharpCommentExtractor;
    private readonly IExternalExecutableRunner externalExecutableRunner;
    private readonly ILog logger;
}