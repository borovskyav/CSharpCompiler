using CSharpCompiler.NugetPackagesDownloader;

using NuGet.Common;
using NuGet.Frameworks;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler.NugetPackageLibrariesExtractor;

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

        var targetFrameworkFiles = GetNearest(await nugetResult.PackageReader.GetLibItemsAsync(token), frameworkVersion);

        if(targetFrameworkFiles == null)
            throw new Exception("Nuget package {packageId} supports {compatibleFramework}, but folder not found");
        
        logger.Debug("Use Framework {packageFramework} for Package {packageId}", targetFrameworkFiles.TargetFramework, package.PackageId);

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
    
    private static T GetNearest<T>(IEnumerable<T> items, NuGetFramework projectFramework) where T : class, IFrameworkSpecific
        => NuGetFrameworkUtility.GetNearest(items, projectFramework, e => e.TargetFramework);

    private readonly ILog logger;
    private readonly NuGetFramework frameworkVersion;
}