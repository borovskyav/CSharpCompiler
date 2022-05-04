using CSharpCompiler.NugetPackagesDownloader;

using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;

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
        RemoveUnusedFolders(extractDirectory);
        return files;
    }

    private void RemoveUnusedFolders(string extractDirectory)
    {
        var folders = new[] { "lib", "ref" };
        foreach(var folder in folders)
        {
            if(Directory.Exists(Path.Combine(extractDirectory, folder)))
                Directory.Delete(Path.Combine(extractDirectory, folder), true);
        }
    }

    private async Task<IEnumerable<string>> ExtractAsync(
        DownloadPackageResult package,
        string extractDirectory,
        CancellationToken token
    )
    {
        var nugetResult = package.NugetResult!;
        var items = await GetFilesFromPackage(package.PackageId, nugetResult, token);
        return await nugetResult.PackageReader.CopyFilesAsync(
                   extractDirectory,
                   items,
                   (sourcePath, _, sourceStream) => ExtractFile(sourcePath, extractDirectory, sourceStream),
                   NullLogger.Instance,
                   token
               );
    }

    private async Task<IEnumerable<string>> GetFilesFromPackage(string packageId, DownloadResourceResult package, CancellationToken token)
    {
        var tasks = new[] { package.PackageReader.GetLibItemsAsync(token), package.PackageReader.GetItemsAsync(PackagingConstants.Folders.Ref, token), };

        var fileGroups = await Task.WhenAll(tasks);
        foreach(var fileGroup in fileGroups)
        {
            var array = fileGroup.ToArray();
            var nearestFramework = frameworkReducer.GetNearest(frameworkVersion, array.Select(x => x.TargetFramework));
            if(nearestFramework != null)
            {
                logger.Debug("Use Framework {packageFramework} for Package {packageId}", nearestFramework, packageId);
                return array
                       .First(x => x.TargetFramework == nearestFramework)
                       .Items
                       .Where(x => Path.GetExtension(x) == ".dll");
            }
        }

        return Array.Empty<string>();
    }

    private string ExtractFile(string sourcePath, string targetDirectory, Stream sourceStream)
    {
        var fileName = Path.GetFileName(sourcePath);
        var targetPath = Path.Combine(targetDirectory, fileName);
        if(File.Exists(targetPath))
            return null;
        using var targetStream = File.OpenWrite(targetPath);
        sourceStream.CopyTo(targetStream);
        return targetPath;
    }

    private readonly ILog logger;
    private readonly NuGetFramework frameworkVersion;
    private readonly FrameworkReducer frameworkReducer;
}