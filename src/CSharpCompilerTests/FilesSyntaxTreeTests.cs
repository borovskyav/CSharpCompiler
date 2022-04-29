using NuGet.Versioning;

namespace CSharpCompilerTests;

public class FilesSyntaxTreeTests
{
    [Test]
    [TestCase("FluentAssertions 6.6.0", true, "FluentAssertions", "6.6.0")]
    [TestCase("FluentAssertions 6.6.0-beta1", true, "FluentAssertions", "6.6.0-beta1")]
    [TestCase("NUnit 3.13.3-dota2", true, "NUnit", "3.13.3-dota2")]
    [TestCase("NUnit beta1-3.13.3", false, null, null)]
    [TestCase("6.6.0-beta1 NUnit", false, null, null)]
    [TestCase("3.13-beta3-3 NUnit3TestAdapter", false, null, null)]
    public void SinglePackageParseTest(string comment, bool isParsed, string? package, string? version)
    {
        var result = NugetVersionParser.Parse(new[] { comment });
        result.Count.Should().Be(isParsed ? 1 : 0);
        if(isParsed)
            result[package].Should().Be(SemanticVersion.Parse(version));
    }

    [Test]
    public void ManyPackagesInRowParseTest()
    {
        var comment = "FluentAssertions 6.6.0 3.13-beta3-3 NUnit3TestAdapter NUnit 3.13.3-dota2 6.6.0-beta1 NUnit";
        var result = NugetVersionParser.Parse(new[] { comment });
        result.Count.Should().Be(2);
        result["FluentAssertions"].Should().Be(new SemanticVersion(6, 6, 0));
        result["NUnit"].Should().Be(new SemanticVersion(3, 13, 3, "dota2"));
    }

    [Test]
    public void ParseStringWithLineBreaksTest()
    {
        var comment = "FluentAssertions 6.6.0\r3.13-beta3-3 NUnit3TestAdapter\r\nNUnit 3.13.3-dota2 6.6.0-beta1 NUnit";
        var result = NugetVersionParser.Parse(new[] { comment });
        result.Count.Should().Be(2);
        result["FluentAssertions"].Should().Be(new SemanticVersion(6, 6, 0));
        result["NUnit"].Should().Be(new SemanticVersion(3, 13, 3, "dota2"));
    }

    [Test]
    public void GetMaxVersionsTest()
    {
        var list = new List<string>
            {
                "FluentAssertions 6.6.0-beta1",
                "NUnit 3.13.3-dota2",
                "FluentAssertions 6.6.2",
                "NUnit 3.13.3-dota3 NUnit3TestAdapter 4.2.1",
                "NUnit3TestAdapter 4.2.1",
            };

        var result = NugetVersionParser.Parse(list);
        result.Count.Should().Be(3);
        result["NUnit"].Should().Be(new SemanticVersion(3, 13, 3, "dota3"));
        result["FluentAssertions"].Should().Be(new SemanticVersion(6, 6, 2));
        result["NUnit3TestAdapter"].Should().Be(new SemanticVersion(4, 2, 1));
    }
}