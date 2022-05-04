using NuGet.Packaging.Core;

namespace CSharpCompiler.NugetPackagesDownloader;

internal interface INugetPackagesDownloader
{
    Task<IReadOnlyList<DownloadPackageResult>> DownloadAsync(IReadOnlyList<PackageIdentity> packages, CancellationToken token = default);
}