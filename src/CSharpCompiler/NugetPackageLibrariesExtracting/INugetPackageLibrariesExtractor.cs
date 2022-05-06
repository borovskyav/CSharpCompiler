using CSharpCompiler.NugetPackagesDownloader;

namespace CSharpCompiler.NugetPackageLibrariesExtracting;

internal interface INugetPackageLibrariesExtractor
{
    Task<IReadOnlyList<string>> ExtractAsync(IReadOnlyList<DownloadPackageResult> packages, string extractDirectory, CancellationToken token = default);
}