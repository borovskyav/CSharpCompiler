using System.Text.RegularExpressions;

using NuGet.Versioning;

namespace CSharpCompiler;

internal static class NugetVersionParser
{
    public static Dictionary<string, SemanticVersion> Parse(IEnumerable<string> strings)
    {
        var list = new List<(string name, SemanticVersion version)>();
        foreach(var str in strings)
        foreach(Match match in includePackageRegex.Matches(str))
        {
            if(!match.Success)
                continue;

            if(SemanticVersion.TryParse(match.Groups["version"].Value, out var value))
                list.Add((match.Groups["package"].Value, value));
        }

        return list
               .GroupBy(x => x.name)
               .ToDictionary(x => x.Key, x => x.MaxBy(t => t.version)!.version);
    }

    private static readonly Regex includePackageRegex = new("\\s*(?<package>[\\w\\d.]+)\\s+(?<version>[\\w\\d.-]+)");
}