using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
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

internal class NugetPackagesDownloader
{
    public NugetPackagesDownloader()
    {
        logger = NullLogger.Instance;
        settings = Settings.LoadDefaultSettings(null);
        globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(settings);

        cache = new SourceCacheContext();
        repository = Repository.Factory.GetCoreV3(globalSource);
    }

    // todo any other sources?
    private const string globalSource = "https://api.nuget.org/v3/index.json";

    public async Task<IReadOnlyList<DownloadPackageResult>> DownloadAsync(Dictionary<string, NuGetVersion> packages, CancellationToken cancellationToken = default)
    {
        // todo CancellationToken handling when one of packages not found
        // todo Dependencies downloading
        var list = packages.Select(package => GetFromGlobalCacheOrDownloadPackage(package.Key, package.Value, cancellationToken));
        return await Task.WhenAll(list);
    }

    private async Task<DownloadPackageResult> GetFromGlobalCacheOrDownloadPackage(
        string packageId,
        NuGetVersion version,
        CancellationToken cancellationToken = default
    )
    {
        var packageIdentity = new PackageIdentity(packageId, version);
        var package = GetFromCache(packageIdentity);
        if(package != null)
            return new DownloadPackageResult(packageId, version, true, package, true);

        using var packageStream = new MemoryStream();
        if(await DownloadPackageAsync(packageIdentity, packageStream, cancellationToken))
            return new DownloadPackageResult(packageId, version, true, null, false);

        packageStream.Seek(0, SeekOrigin.Begin);

        package = await AddToGlobalCache(packageIdentity, packageStream, cancellationToken);
        return new DownloadPackageResult(packageId, version, true, package, false);
    }

    private DownloadResourceResult? GetFromCache(PackageIdentity packageIdentity)
        => GlobalPackagesFolderUtility.GetPackage(packageIdentity, globalPackagesFolder);

    private async Task<bool> DownloadPackageAsync(
        PackageIdentity packageIdentity,
        Stream packageStream,
        CancellationToken cancellationToken = default
    )
    {
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        if(!await resource.DoesPackageExistAsync(packageIdentity.Id, packageIdentity.Version, cache, logger, cancellationToken))
            return false;

        if(await resource.CopyNupkgToStreamAsync(
               packageIdentity.Id,
               packageIdentity.Version,
               packageStream,
               cache,
               logger,
               cancellationToken
           ))
            throw new Exception("Error when downloading package from remote source"); // todo text
        return true;
    }

    private Task<DownloadResourceResult> AddToGlobalCache(
        PackageIdentity packageIdentity,
        Stream packageStream,
        CancellationToken cancellationToken = default
    )
    {
        return GlobalPackagesFolderUtility.AddPackageAsync(
            globalSource,
            packageIdentity,
            packageStream,
            globalPackagesFolder,
            parentId: Guid.Empty,
            ClientPolicyContext.GetClientPolicy(settings, logger),
            logger,
            cancellationToken
        );
    }

    private readonly ILogger logger;
    private readonly string globalPackagesFolder;
    private readonly SourceRepository repository;
    private readonly SourceCacheContext cache;
    private readonly ISettings? settings;
}