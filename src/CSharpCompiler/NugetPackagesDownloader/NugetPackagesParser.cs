using System.Text.RegularExpressions;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler.NugetPackagesDownloader;

internal class NugetPackagesParser
{
    public NugetPackagesParser(ILog logger)
    {
        this.logger = logger.ForContext<NugetPackagesParser>();
    }

    public IReadOnlyList<PackageIdentity> Parse(IEnumerable<string> strings)
    {
        var list = new List<(string name, NuGetVersion version)>();
        foreach(var str in strings)
        foreach(Match match in includePackageRegex.Matches(str))
        {
            if(!match.Success)
                continue;

            if(NuGetVersion.TryParse(match.Groups["version"].Value, out var value))
                list.Add((match.Groups["package"].Value, value));
        }

        var groupedPackages = list
                              .GroupBy(x => x.name, x => x.version)
                              .ToArray();
        var duplicatePackages = groupedPackages
                                .Where(x => x.Distinct().Count() > 1)
                                .ToArray();

        if(duplicatePackages.Length != 0)
            logger.Warn("There are duplicate includes of packages with different versions, will try to take suitable version");
        return list.Select(x => new PackageIdentity(x.name, x.version)).ToArray();
    }

    private readonly ILog logger;

    private static readonly Regex includePackageRegex = new(@"\s*Package:\s*(?<package>[\w,.-]+)\s+(?<version>[\d]+[\w.+-]+)");
}