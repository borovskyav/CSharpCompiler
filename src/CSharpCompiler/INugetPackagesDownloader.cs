using NuGet.Versioning;

namespace CSharpCompiler;

internal interface INugetPackagesDownloader
{
    Task<IReadOnlyList<DownloadPackageResult>> DownloadAsync(Dictionary<string, NuGetVersion> packages, CancellationToken token = default);
}