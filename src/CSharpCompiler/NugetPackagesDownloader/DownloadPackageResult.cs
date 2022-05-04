using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace CSharpCompiler.NugetPackagesDownloader;

internal class DownloadPackageResult
{
    public DownloadPackageResult(
        string packageId,
        NuGetVersion version,
        DownloadResourceResult? nugetResult,
        bool notFound,
        bool canceled
    )
    {
        PackageId = packageId;
        Version = version;
        NugetResult = nugetResult;
        NotFound = notFound;
        Canceled = canceled;
    }

    public string PackageId { get; }
    public NuGetVersion Version { get; }
    public bool NotFound { get; }

    public DownloadResourceResult? NugetResult { get; }
    public bool Canceled { get; }
}