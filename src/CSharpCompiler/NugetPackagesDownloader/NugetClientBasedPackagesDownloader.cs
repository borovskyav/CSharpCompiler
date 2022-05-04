using System.Collections.Concurrent;
using System.Text;

using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler.NugetPackagesDownloader;

internal class NugetClientBasedPackagesDownloader : INugetPackagesDownloader
{
    public NugetClientBasedPackagesDownloader(ILog logger, string targetFramework)
    {
        this.logger = logger.ForContext<NugetClientBasedPackagesDownloader>();
        nugetClientLogger = NullLogger.Instance;
        settings = Settings.LoadDefaultSettings(null);
        globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(settings);

        cache = new SourceCacheContext();
        repository = Repository.Factory.GetCoreV3(globalSource);

        platformPackage = new PackageIdentity("Microsoft.NETCore.App.Ref", new NuGetVersion(6, 0, 4));
        applicationFramework = NuGetFramework.Parse(targetFramework);
    }

    private const string globalSource = "https://api.nuget.org/v3/index.json";

    public async Task<IReadOnlyList<DownloadPackageResult>> DownloadAsync(IReadOnlyList<PackageIdentity> packages, CancellationToken token = default)
    {
        if(packages.Count == 0)
            logger.Info("No included nuget packages, downloading only refs package");
        else
            logger.Info("Found {packagesCount} packages included in source code, start download", packages.Count);

        var packagesToInstall = (await ResolveTransitiveDependencies(packages, token)).ToArray();
        var downloadTasks = packagesToInstall.Select(x => DownloadPackageAsync(x, token));
        var downloadResourceResults = await Task.WhenAll(downloadTasks);

        var canceled = downloadResourceResults.Any(x => x.Canceled);
        if(canceled)
            throw new OperationCanceledException("Download operation has been canceled");
        var notFound = downloadResourceResults.Any(x => x.NotFound);
        LogDownloadResult(downloadResourceResults, notFound);
        if(notFound)
            throw new Exception("Some packages not found");

        return downloadResourceResults;
    }

    private async Task<DownloadPackageResult> DownloadPackageAsync(
        SourcePackageDependencyInfo package,
        CancellationToken token
    )
    {
        var downloadResource = await package.Source.GetResourceAsync<DownloadResource>(token);
        var result = await downloadResource.GetDownloadResourceResultAsync(
                         package,
                         new PackageDownloadContext(cache),
                         globalPackagesFolder,
                         nugetClientLogger,
                         token);
        return new DownloadPackageResult(
            package.Id,
            package.Version,
            result,
            result.Status == DownloadResourceResultStatus.NotFound,
            result.Status == DownloadResourceResultStatus.Cancelled);
    }

    private async Task<IEnumerable<SourcePackageDependencyInfo>> ResolveTransitiveDependencies(
        IReadOnlyList<PackageIdentity> packages,
        CancellationToken token
    )
    {
        var dict = new ConcurrentDictionary<PackageIdentity, SourcePackageDependencyInfo>(PackageIdentityComparer.Default);

        await Task.WhenAll(packages.Select(x => GetPackageDependencies(x, dict, token)));
        await GetPackageDependencies(platformPackage, dict, token);

        var resolverContext = new PackageResolverContext(
            DependencyBehavior.Lowest,
            packages.Concat(new[] { platformPackage }).Select(x => x.Id),
            Enumerable.Empty<string>(),
            Enumerable.Empty<PackageReference>(),
            Enumerable.Empty<PackageIdentity>(),
            dict.Values,
            new[] { repository.PackageSource },
            NullLogger.Instance);

        var resolver = new PackageResolver();
        return resolver
               .Resolve(resolverContext, token)
               .Select(p => dict.TryGetValue(p, out var value) ? value : throw new Exception());
    }

    private async Task GetPackageDependencies(
        PackageIdentity package,
        ConcurrentDictionary<PackageIdentity, SourcePackageDependencyInfo> availablePackages,
        CancellationToken token
    )
    {
        if(availablePackages.ContainsKey(package))
            return;

        var dependencyInfoResource = await repository.GetResourceAsync<DependencyInfoResource>(token);
        var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                                 package,
                                 applicationFramework,
                                 cache,
                                 nugetClientLogger,
                                 token);

        if(dependencyInfo == null)
            return;

        availablePackages.TryAdd(dependencyInfo, dependencyInfo);
        var tasks = dependencyInfo
                    .Dependencies
                    .Select(async dependency =>
                                await GetPackageDependencies(new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion), availablePackages, token));
        await Task.WhenAll(tasks);
    }

    private void LogDownloadResult(IEnumerable<DownloadPackageResult> packages, bool notFound)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("Download process has been finished");
        if(notFound)
        {
            var notFoundPackages = packages.Where(x => x.NotFound);
            AppendArray(stringBuilder, "Not found packages:", notFoundPackages);
            logger.Error(stringBuilder.ToString());
            return;
        }

        logger.Info(stringBuilder.ToString());

        if(logger.IsEnabledForDebug())
        {
            var sb = new StringBuilder();
            var downloadedPackages = packages.Where(x => !x.NotFound);
            AppendArray(sb, "Downloaded packages:", downloadedPackages);
            logger.Debug(sb.ToString());
        }
    }

    private void AppendArray(StringBuilder stringBuilder, string header, IEnumerable<DownloadPackageResult> array)
    {
        stringBuilder.AppendLine();
        stringBuilder.Append(header);
        foreach(var item in array)
        {
            stringBuilder.AppendLine();
            stringBuilder.Append($"\t{item.PackageId}: {item.Version}");
        }
    }

    private readonly ILogger nugetClientLogger;
    private readonly string globalPackagesFolder;
    private readonly SourceRepository repository;
    private readonly SourceCacheContext cache;

    private readonly NuGetFramework applicationFramework;
    private readonly PackageIdentity platformPackage;

    private readonly ILog logger;
    private readonly ISettings? settings;
}