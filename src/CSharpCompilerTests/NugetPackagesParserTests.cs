using CSharpCompiler.NugetPackagesDownloader;

using NuGet.Versioning;

namespace CSharpCompilerTests;

public class NugetPackagesParserTests
{
    [Test]
    [TestCase("Package: FluentAssertions 6.6.0", true, "FluentAssertions", "6.6.0")]
    [TestCase("Package:FluentAssertions 6.6.0-beta1", true, "FluentAssertions", "6.6.0-beta1")]
    [TestCase("Package: NUnit 3.13.3-dota2", true, "NUnit", "3.13.3-dota2")]
    [TestCase("Package: NUnit: 3.13.3-beta1", false, null, null)]
    [TestCase("Package: NUnit beta1-3.13.3", false, null, null)]
    [TestCase("6.6.0-beta1 NUnit:", false, null, null)]
    [TestCase("3.13-beta3-3: NUnit3TestAdapter", false, null, null)]
    public void SinglePackageParseTest(string comment, bool isParsed, string? package, string? version)
    {
        var result = NugetPackagesParser.Parse(new[] { comment });
        result.Count.Should().Be(isParsed ? 1 : 0);
        if(isParsed)
            result[package!].Should().Be(NuGetVersion.Parse(version));
    }

    [Test]
    public void ManyPackagesInRowParseTest()
    {
        var comment = "Package: FluentAssertions 6.6.0 Package: 3.13-beta3-3 NUnit3TestAdapter: Package: NUnit 3.13.3-dota2 6.6.0-beta1: NUnit";
        var result = NugetPackagesParser.Parse(new[] { comment }
        );
        result.Should().BeEquivalentTo(
            new Dictionary<string, NuGetVersion> { { "FluentAssertions", new NuGetVersion(6, 6, 0) }, { "NUnit", new NuGetVersion(3, 13, 3, "dota2") } });
    }

    [Test]
    public void ParseStringWithLineBreaksTest()
    {
        var comment = "Package: FluentAssertions 6.6.0\rPackage: 3.13-beta3-3 NUnit3TestAdapter:\r\n  Package: NUnit 3.13.3-dota2 Package: 6.6.0-beta1 NUnit";
        var result = NugetPackagesParser.Parse(new[] { comment }
        );
        result.Should().BeEquivalentTo(
            new Dictionary<string, NuGetVersion> { { "FluentAssertions", new NuGetVersion(6, 6, 0) }, { "NUnit", new NuGetVersion(3, 13, 3, "dota2") } });
    }

    [Test]
    public void GetMaxVersionsTest()
    {
        var list = new List<string>
            {
                "Package: FluentAssertions 6.6.0-beta1",
                " Package:  NUnit 3.13.3-dota2",
                "  Package: FluentAssertions 6.6.2",
                "Package:  NUnit 3.13.3-dota3 Package: NUnit3TestAdapter 4.2.1",
                "Package: NUnit3TestAdapter 4.2.1",
            };

        var result = NugetPackagesParser.Parse(list);
        result.Should().BeEquivalentTo(new Dictionary<string, NuGetVersion>
                {
                    { "NUnit3TestAdapter", new NuGetVersion(4, 2, 1) },
                    { "FluentAssertions", new NuGetVersion(6, 6, 2) },
                    { "NUnit", new NuGetVersion(3, 13, 3, "dota3") },
                }
        );
    }
}