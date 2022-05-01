namespace CSharpCompilerTests;

public class CompilerArgumentsParserTests
{
    [Test]
    [TestCase("", "", "")]
    [TestCase("1.cs 2.cs 3.cs qwe.cs -- 1 2 exe", "1.cs 2.cs 3.cs qwe.cs", "1 2 exe")]
    [TestCase("-- 1 2 exe", "", "1 2 exe")]
    [TestCase("1.cs 2.cs 3.cs qwe.cs -- ", "1.cs 2.cs 3.cs qwe.cs", "")]
    [TestCase("1.cs 2.cs 3.cs qwe.cs", "1.cs 2.cs 3.cs qwe.cs", "")]
    public void Test(string arguments, string expectedFiles, string expectedArguments)
    {
        var result = ConsoleArgumentsParser.Parse(arguments.Split(" ", StringSplitOptions.RemoveEmptyEntries));
        result.FilesPath.Should().BeEquivalentTo(expectedFiles.Split(" ", StringSplitOptions.RemoveEmptyEntries));
        result.Arguments.Should().BeEquivalentTo(expectedArguments.Split(" ", StringSplitOptions.RemoveEmptyEntries));
    }
}