using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace CSharpCompiler;

internal class DownloadPackageResult
{
    public DownloadPackageResult(
        string packageId,
        NuGetVersion version,
        bool found,
        DownloadResourceResult? nugetResult,
        bool fromCache
    )
    {
        PackageId = packageId;
        Version = version;
        NugetResult = nugetResult;
        Found = found;
        FromCache = fromCache;
    }

    public string PackageId { get; }
    public NuGetVersion Version { get; }
    public bool Found { get; }

    public DownloadResourceResult? NugetResult { get; }
    public bool FromCache { get; }
}