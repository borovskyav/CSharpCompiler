using CSharpCompiler.NugetPackagesDownloader;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using Vostok.Logging.Console;

namespace CSharpCompilerTests;

public class NugetPackagesParserTests
{
    [SetUp]
    public void SetUp()
    {
        parser = new NugetPackagesParser(new ConsoleLog());
    }

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
        var result = parser!.Parse(new[] { comment });
        result.Count.Should().Be(isParsed ? 1 : 0);
        if(isParsed)
            result.Should().BeEquivalentTo(new[] { new PackageIdentity(package, NuGetVersion.Parse(version)) });
    }

    [Test]
    public void ManyPackagesInRowParseTest()
    {
        var comment = "Package: FluentAssertions 6.6.0 Package: 3.13-beta3-3 NUnit3TestAdapter: Package: NUnit 3.13.3-dota2 6.6.0-beta1: NUnit";
        var result = parser!.Parse(new[] { comment }
        );
        result.Should().BeEquivalentTo(new[]
            {
                new PackageIdentity("FluentAssertions", new NuGetVersion(6, 6, 0)),
                new PackageIdentity("NUnit", new NuGetVersion(3, 13, 3, "dota2")),
            });
    }

    [Test]
    public void ParseStringWithLineBreaksTest()
    {
        var comment = "Package: FluentAssertions 6.6.0\rPackage: 3.13-beta3-3 NUnit3TestAdapter:\r\n  Package: NUnit 3.13.3-dota2 Package: 6.6.0-beta1 NUnit";
        var result = parser!.Parse(new[] { comment });
        result.Should().BeEquivalentTo(new[]
            {
                new PackageIdentity("FluentAssertions", new NuGetVersion(6, 6, 0)),
                new PackageIdentity("NUnit", new NuGetVersion(3, 13, 3, "dota2")),
            });
    }

    private NugetPackagesParser? parser;
}