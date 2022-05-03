using NuGet.Versioning;

namespace CSharpCompiler.NugetPackagesDownloader;

internal interface INugetPackagesDownloader
{
    Task<IReadOnlyList<DownloadPackageResult>> DownloadAsync(Dictionary<string, NuGetVersion> packages, CancellationToken token = default);
}