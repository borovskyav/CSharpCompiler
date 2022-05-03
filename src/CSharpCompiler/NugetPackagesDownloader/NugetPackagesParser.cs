using System.Text;
using System.Text.RegularExpressions;

using NuGet.Versioning;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler.NugetPackagesDownloader;

internal class NugetPackagesParser
{
    public NugetPackagesParser(ILog logger)
    {
        this.logger = logger.ForContext<NugetPackagesParser>();
    }

    public Dictionary<string, NuGetVersion> Parse(IEnumerable<string> strings)
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
            LogDuplicatePackagesWarn(duplicatePackages);
        return groupedPackages.ToDictionary(x => x.Key, x => x.Max()!);
    }

    private void LogDuplicatePackagesWarn(IGrouping<string, NuGetVersion>[] nuGetVersions)
    {
        var sb = new StringBuilder("There are duplicate includes of packages with different versions, take max version:");
        foreach(var nuGetVersion in nuGetVersions)
        {
            sb.AppendLine();
            sb.Append($"\t{string.Join(", ", nuGetVersion)} => {nuGetVersion.Max()}");
        }
        logger.Warn(sb.ToString());
    }

    private readonly ILog logger;

    private static readonly Regex includePackageRegex = new("\\s*Package:\\s*(?<package>[\\w\\d.]+)\\s+(?<version>[\\d.-]+[\\w]+)");
}