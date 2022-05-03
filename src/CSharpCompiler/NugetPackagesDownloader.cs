using System.Text;

using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler;

internal class NugetPackagesDownloader : INugetPackagesDownloader
{
    public NugetPackagesDownloader(ILog logger)
    {
        this.logger = logger.ForContext<NugetPackagesDownloader>();
        nugetClientLogger = NullLogger.Instance;
        settings = Settings.LoadDefaultSettings(null);
        globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(settings);

        cache = new SourceCacheContext();
        repository = Repository.Factory.GetCoreV3(globalSource);
    }

    private const string globalSource = "https://api.nuget.org/v3/index.json";

    public async Task<IReadOnlyList<DownloadPackageResult>> DownloadAsync(Dictionary<string, NuGetVersion> packages, CancellationToken token = default)
    {
        logger.Info("Found {packagesCount} included in source code, start download");

        var list = packages
            .Select(package => GetFromGlobalCacheOrDownloadPackage(package.Key, package.Value, token));
        var resultPackages = await Task.WhenAll(list);

        var notFound = resultPackages.Any(x => !x.Found);
        LogDownloadResult(resultPackages, notFound);
        if(notFound)
            throw new Exception("Some packages not found");
        return resultPackages;
    }

    private async Task<DownloadPackageResult> GetFromGlobalCacheOrDownloadPackage(
        string packageId,
        NuGetVersion version,
        CancellationToken token
    )
    {
        var packageIdentity = new PackageIdentity(packageId, version);
        var package = GetFromCache(packageIdentity);
        if(package != null)
            return new DownloadPackageResult(packageId, version, true, package, true);

        using var packageStream = new MemoryStream();
        if(!await DownloadPackageAsync(packageIdentity, packageStream, token))
            return new DownloadPackageResult(packageId, version, false, null, false);

        packageStream.Seek(0, SeekOrigin.Begin);

        package = await AddToGlobalCache(packageIdentity, packageStream, token);
        return new DownloadPackageResult(packageId, version, true, package, false);
    }

    private DownloadResourceResult? GetFromCache(PackageIdentity packageIdentity)
        => GlobalPackagesFolderUtility.GetPackage(packageIdentity, globalPackagesFolder);

    private async Task<bool> DownloadPackageAsync(
        PackageIdentity packageIdentity,
        Stream packageStream,
        CancellationToken token
    )
    {
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>(token);
        if(!await resource.DoesPackageExistAsync(packageIdentity.Id, packageIdentity.Version, cache, nugetClientLogger, token))
            return false;

        if(!await resource.CopyNupkgToStreamAsync(
                packageIdentity.Id,
                packageIdentity.Version,
                packageStream,
                cache,
                nugetClientLogger,
                token
            ))
            throw new Exception("Error when downloading package from remote source");
        return true;
    }

    private Task<DownloadResourceResult> AddToGlobalCache(
        PackageIdentity packageIdentity,
        Stream packageStream,
        CancellationToken token
    )
    {
        return GlobalPackagesFolderUtility.AddPackageAsync(
            globalSource,
            packageIdentity,
            packageStream,
            globalPackagesFolder,
            Guid.Empty,
            ClientPolicyContext.GetClientPolicy(settings, nugetClientLogger),
            nugetClientLogger,
            token
        );
    }

    private void LogDownloadResult(IEnumerable<DownloadPackageResult> packages, bool notFound)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Download process has been finished:");
        var grouping = packages
                       .GroupBy(x => (x.Found, x.FromCache))
                       .OrderBy(x => x.Key);
        foreach(var group in grouping)
        {
            var result = (group.Key.Found, group.Key.FromCache) switch
                {
                    (false, false) => "Not found packages:",
                    (true, false) => "Downloaded from remote:",
                    (true, true) => "From cache:",
                    _ => throw new ArgumentOutOfRangeException(),
                };
            stringBuilder.AppendLine(result);
            foreach(var package in group.Select(x => x))
                stringBuilder.AppendLine($"\t{package.PackageId}: {package.Version}");
        }

        if(notFound)
            logger.Error(stringBuilder.ToString());
        else
            logger.Info(stringBuilder.ToString());
    }

    private readonly ILog logger;

    private readonly ILogger nugetClientLogger;
    private readonly string globalPackagesFolder;
    private readonly SourceRepository repository;
    private readonly SourceCacheContext cache;
    private readonly ISettings? settings;
}