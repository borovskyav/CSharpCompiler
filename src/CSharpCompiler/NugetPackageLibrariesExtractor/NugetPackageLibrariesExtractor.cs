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

        frameworkReducer = new FrameworkReducer();
    }

    public async Task<IReadOnlyList<string>> ExtractAsync(
        IReadOnlyList<DownloadPackageResult> packages,
        string extractDirectory,
        CancellationToken token
    )
    {
        logger.Info("Extract libraries from the nuget packages");
        var tasks = packages.Select(package => ExtractAsync(package, extractDirectory, token));
        var files = (await Task.WhenAll(tasks)).SelectMany(x => x).ToList();
        Directory.Delete(Path.Combine(extractDirectory, "lib"), true); // костыль...
        return files;
    }

    private async Task<IEnumerable<string>> ExtractAsync(
        DownloadPackageResult package,
        string extractDirectory,
        CancellationToken token
    )
    {
        var nugetResult = package.NugetResult!;
        var libItems = (await nugetResult.PackageReader.GetLibItemsAsync(token)).ToArray();
        var nearestFramework = frameworkReducer.GetNearest(frameworkVersion, libItems.Select(x => x.TargetFramework));

        if(nearestFramework == null)
            return Array.Empty<string>();

        logger.Debug("Use Framework {packageFramework} for Package {packageId}", nearestFramework, package.PackageId);

        return await nugetResult.PackageReader.CopyFilesAsync(
                   extractDirectory,
                   libItems.First(x => x.TargetFramework == nearestFramework).Items,
                   (sourcePath, _, sourceStream) => ExtractFile(sourcePath, extractDirectory, sourceStream),
                   NullLogger.Instance,
                   token
               );
    }

    private string ExtractFile(string sourcePath, string targetDirectory, Stream sourceStream)
    {
        var fileName = Path.GetFileName(sourcePath);
        var targetPath = Path.Combine(targetDirectory, fileName);
        using var targetStream = File.OpenWrite(targetPath);
        sourceStream.CopyTo(targetStream);
        return targetPath;
    }

    private readonly ILog logger;
    private readonly NuGetFramework frameworkVersion;
    private readonly FrameworkReducer frameworkReducer;
}