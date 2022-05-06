using System.Text;

using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.DependencyResolver;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.RuntimeModel;
using NuGet.Versioning;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler.NugetPackagesDownloader;

internal class NugetClientBasedPackagesDownloader : INugetPackagesDownloader, IDisposable
{
    public NugetClientBasedPackagesDownloader(ILog logger, string framework, string runtime)
    {
        nugetClientLogger = NullLogger.Instance;
        settings = Settings.LoadDefaultSettings(AppDomain.CurrentDomain.BaseDirectory);
        globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(settings);

        cache = new SourceCacheContext();
        sourceRepositoryProvider = new SourceRepositoryProvider(new PackageSourceProvider(settings), Repository.Provider.GetCoreV3());
        repositories = sourceRepositoryProvider.GetRepositories().ToDictionary(x => x.PackageSource);

        this.logger = logger.ForContext<NugetClientBasedPackagesDownloader>();
        this.framework = NuGetFramework.Parse(framework);
        this.runtime = runtime;
    }

    public async Task<IReadOnlyList<DownloadPackageResult>> DownloadAsync(IReadOnlyList<PackageIdentity> packages, CancellationToken token = default)
    {
        var appRefPackage = await ResolveNetCoreAppRefPackageAsync(token);
        if(packages.Count == 0)
        {
            logger.Info("No included nuget packages, downloading only refs package");
            return new[] { appRefPackage };
        }

        logger.Info("Found {packagesCount} packages included in source code, start download", packages.Count);

        var packagesToInstall = await GetPackagesToInstall(packages);

        var downloadTasks = packagesToInstall
            .Select(x => DownloadPackageAsync(x, token));
        var downloadResourceResults = await Task.WhenAll(downloadTasks);

        var canceled = downloadResourceResults.Any(x => x.Canceled);
        if(canceled)
            throw new OperationCanceledException("Download operation has been canceled");
        var notFound = downloadResourceResults.Any(x => x.NotFound);
        LogDownloadResult(downloadResourceResults, notFound);
        if(notFound)
            throw new Exception("Some packages not found");

        return new[] { appRefPackage }.Concat(downloadResourceResults).ToArray();
    }

    private async Task<IEnumerable<RemoteMatch>> GetPackagesToInstall(IReadOnlyList<PackageIdentity> packages)
    {
        var libraryRange = new LibraryRange("CSharpCompiler", new VersionRange(new NuGetVersion(1, 0, 0)), LibraryDependencyTarget.Project);
        var (walker, context) = BuildRemoteDependencyWalker(libraryRange, packages);
        var graph = RuntimeGraph.Empty;
        var graphNode = await walker.WalkAsync(libraryRange, framework, runtime, graph, true);
        var restoreTargetGraph = RestoreTargetGraph.Create(graph, new[] { graphNode }, context, nugetClientLogger, framework, this.runtime);

        if(ValidateAndLogAnalyzeResult(restoreTargetGraph.AnalyzeResult))
            throw new Exception("Nuget packages restore fail, see log messages");

        var packagesToInstall = restoreTargetGraph
                                .Flattened
                                .Select(graphItem => graphItem.Data.Match)
                                .Where(remoteMatch => remoteMatch.Library.Type == LibraryType.Package);
        return packagesToInstall;
    }

    private async Task<DownloadPackageResult> ResolveNetCoreAppRefPackageAsync(CancellationToken token)
    {
        DownloadResourceResult? result = null;
        var platformPackage1 = new PackageIdentity("Microsoft.NETCore.App.Ref", new NuGetVersion(6, 0, 4));
        foreach(var repository in repositories.Values)
        {
            var downloadResource = await repository.GetResourceAsync<DownloadResource>(token);
            result = await downloadResource.GetDownloadResourceResultAsync(
                         platformPackage1,
                         new PackageDownloadContext(cache),
                         globalPackagesFolder,
                         nugetClientLogger,
                         token);

            if(result.Status is DownloadResourceResultStatus.Available or DownloadResourceResultStatus.AvailableWithoutStream)
                return new DownloadPackageResult(platformPackage1.Id, platformPackage1.Version, result, false, false);
            if(result.Status is DownloadResourceResultStatus.Cancelled)
                throw new OperationCanceledException("The operation was canceled.");
        }

        if(result == null || result.Status == DownloadResourceResultStatus.NotFound)
            throw new Exception("Some packages not found");

        return new DownloadPackageResult(
            platformPackage1.Id,
            platformPackage1.Version,
            result,
            false,
            result.Status == DownloadResourceResultStatus.Cancelled);
    }

    private async Task<DownloadPackageResult> DownloadPackageAsync(
        RemoteMatch package,
        CancellationToken token
    )
    {
        var packageIdentity = new PackageIdentity(package.Library.Name, package.Library.Version);
        var repository = repositories.TryGetValue(package.Provider.Source, out var value)
                             ? value
                             : sourceRepositoryProvider.CreateRepository(package.Provider.Source);
        var downloadResource = await repository.GetResourceAsync<DownloadResource>(token);
        var result = await downloadResource.GetDownloadResourceResultAsync(
                         packageIdentity,
                         new PackageDownloadContext(cache),
                         globalPackagesFolder,
                         nugetClientLogger,
                         token);
        return new DownloadPackageResult(
            packageIdentity.Id,
            packageIdentity.Version,
            result,
            result.Status == DownloadResourceResultStatus.NotFound,
            result.Status == DownloadResourceResultStatus.Cancelled);
    }

    private (RemoteDependencyWalker, RemoteWalkContext) BuildRemoteDependencyWalker(LibraryRange libraryRange, IReadOnlyList<PackageIdentity> packages)
    {
        var dependencyProvider = new CustomPackagesDependencyProvider(libraryRange, packages);
        var remoteWalkContext = new RemoteWalkContext(
            cache,
            PackageSourceMapping.GetPackageSourceMapping(settings),
            nugetClientLogger
        );
        var dependencyProviders = repositories
                                  .Select(x => new SourceRepositoryDependencyProvider(x.Value, nugetClientLogger, cache, true, true))
                                  .ToArray();
        remoteWalkContext.RemoteLibraryProviders.AddRange(dependencyProviders);
        remoteWalkContext.ProjectLibraryProviders.Add(dependencyProvider);
        return (new RemoteDependencyWalker(remoteWalkContext), remoteWalkContext);
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

    private bool ValidateAndLogAnalyzeResult(AnalyzeResult<RemoteResolveResult> analyzeResult)
    {
        var stringBuilder = new StringBuilder("Nuget packages restore fail:");
        var hasErrors = false;
        foreach(var cycle in analyzeResult.Cycles)
        {
            hasErrors = true;
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Cycle detected:");
            stringBuilder.Append(cycle.GetPath());
        }

        foreach(var versionConflict in analyzeResult.VersionConflicts)
        {
            hasErrors = true;
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(
                $"Version conflict detected for {versionConflict.Selected.Key.Name}. Reference {versionConflict.Selected.GetIdAndVersionOrRange()} directly to files to resolve this issue.");
            stringBuilder.AppendLine(versionConflict.Selected.GetPathWithLastRange());
            stringBuilder.Append(versionConflict.Conflicting.GetPathWithLastRange());
        }

        if(hasErrors)
            logger.Error(stringBuilder.ToString());
        return hasErrors;
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

    public void Dispose()
    {
        cache.Dispose();
    }

    private readonly ILogger nugetClientLogger;
    private readonly string globalPackagesFolder;
    private readonly Dictionary<PackageSource, SourceRepository> repositories;
    private readonly SourceCacheContext cache;

    private readonly NuGetFramework framework;

    private readonly ILog logger;
    private readonly string runtime;
    private readonly ISettings settings;
    private readonly SourceRepositoryProvider sourceRepositoryProvider;
}