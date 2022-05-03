using NuGet.Common;
using NuGet.Frameworks;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler;

class NugetPackageLibrariesExtractor : INugetPackageLibrariesExtractor
{
    public NugetPackageLibrariesExtractor(
        ILog logger,
        string frameworkVersion
    )
    {
        this.logger = logger.ForContext<NugetPackageLibrariesExtractor>();
        this.frameworkVersion = NuGetFramework.Parse(frameworkVersion);
    }

    public async Task<IReadOnlyList<string>> ExtractAsync(
        IReadOnlyList<DownloadPackageResult> packages,
        string extractDirectory,
        CancellationToken token
    )
    {
        logger.Info("Extract libraries from the nuget packages");
        var tasks = packages.Select(package => ExtractAsync(package, extractDirectory, token));
        return (await Task.WhenAll(tasks)).SelectMany(x => x).ToList();
    }

    private async Task<IEnumerable<string>> ExtractAsync(
        DownloadPackageResult package,
        string extractDirectory,
        CancellationToken token
    )
    {
        var nugetResult = package.NugetResult!;
        var compatibleFramework = await GetCompatibleNugetFramework(package, token);
        logger.Debug("Use Framework {packageFramework} for Package: {packageId}", compatibleFramework, package.PackageId);

        var targetFrameworkFiles = (await nugetResult.PackageReader.GetLibItemsAsync(token))
            .First(x => x.TargetFramework == compatibleFramework);

        if(targetFrameworkFiles == null)
            throw new Exception("Nuget package {packageId} supports {compatibleFramework}, but folder not found");

        return await nugetResult.PackageReader.CopyFilesAsync(
                   extractDirectory,
                   targetFrameworkFiles.Items,
                   ExtractFile,
                   NullLogger.Instance,
                   token
               );
    }

    private string ExtractFile(string sourcePath, string targetPath, Stream sourceStream)
    {
        using var targetStream = File.OpenWrite(targetPath);
        sourceStream.CopyTo(targetStream);
        return targetPath;
    }

    private async Task<NuGetFramework> GetCompatibleNugetFramework(DownloadPackageResult package, CancellationToken token)
    {
        var supportedFrameworks = (await package
                                         .NugetResult!
                                         .PackageReader
                                         .GetSupportedFrameworksAsync(token))
            .ToArray();
        var provider = DefaultCompatibilityProvider.Instance;
        var compatibleFramework = supportedFrameworks.FirstOrDefault(x => provider.IsCompatible(frameworkVersion, x));
        if(compatibleFramework == null)
            throw new Exception($"Compatible framework for package {package.PackageId} not found. Available frameworks: {string.Join(", ", supportedFrameworks.Select(x => x))}");

        return compatibleFramework;
    }

    private readonly ILog logger;
    private readonly NuGetFramework frameworkVersion;
}