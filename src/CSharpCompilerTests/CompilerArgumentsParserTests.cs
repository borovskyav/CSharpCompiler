namespace CSharpCompilerTests;

public class CompilerArgumentsParserTests
{
    [Test]
    [TestCase("", "", "")]
    [TestCase("1.cs 2.cs 3.cs qwe.cs -- 1 2 exe", "1.cs 2.cs 3.cs qwe.cs", "1 2 exe")]
    [TestCase("-- 1 2 exe", "", "1 2 exe")]
    [TestCase("1.cs 2.cs 3.cs qwe.cs -- ", "1.cs 2.cs 3.cs qwe.cs", "")]
    [TestCase("1.cs 2.cs 3.cs qwe.cs", "1.cs 2.cs 3.cs qwe.cs", "")]
    public void Test1(string arguments, string expectedFiles, string expectedArguments)
    {
        var result = CompilerArgumentsParser.Parse(arguments.Split(" ", StringSplitOptions.RemoveEmptyEntries));
        result.FilesPath.Should().BeEquivalentTo(expectedFiles.Split(" ", StringSplitOptions.RemoveEmptyEntries));
        result.ProgramArguments.Should().Be(expectedArguments);
    }
}