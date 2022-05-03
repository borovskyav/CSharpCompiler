using System.Text.RegularExpressions;

using NuGet.Versioning;

namespace CSharpCompiler.NugetPackagesDownloader;

internal static class NugetPackagesParser
{
    public static Dictionary<string, NuGetVersion> Parse(IEnumerable<string> strings)
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

        return list
               .GroupBy(x => x.name)
               .ToDictionary(x => x.Key, x => x.MaxBy(t => t.version)!.version);
    }

    private static readonly Regex includePackageRegex = new("\\s*Package:\\s*(?<package>[\\w\\d.]+)\\s+(?<version>[\\d.-]+[\\w]+)");
}