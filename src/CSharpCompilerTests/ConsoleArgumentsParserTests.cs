namespace CSharpCompilerTests;

public class CompilerArgumentsParserTests
{
    [Test]
    [TestCase("", "", "", false)]
    [TestCase("-allowUnsafe", "", "", true)]
    [TestCase("-allowUnsafe 1.cs 2.cs 3.cs qwe.cs -- 1 2 exe", "1.cs 2.cs 3.cs qwe.cs", "1 2 exe", true)]
    [TestCase("-- 1 2 exe", "", "1 2 exe", false)]
    [TestCase("-allowUnsafe -- 1 2 exe", "", "1 2 exe", true)]
    [TestCase("1.cs 2.cs 3.cs qwe.cs -- ", "1.cs 2.cs 3.cs qwe.cs", "", false)]
    [TestCase("1.cs 2.cs 3.cs qwe.cs", "1.cs 2.cs 3.cs qwe.cs", "", false)]
    [TestCase("-allowUnsafe 1.cs 2.cs 3.cs qwe.cs", "1.cs 2.cs 3.cs qwe.cs", "", true)]
    public void Test(string arguments, string expectedFiles, string expectedArguments, bool expectedAllowUnsafe)
    {
        var result = ConsoleArgumentsParser.Parse(arguments.Split(" ", StringSplitOptions.RemoveEmptyEntries));
        result.FilesPath.Should().BeEquivalentTo(expectedFiles.Split(" ", StringSplitOptions.RemoveEmptyEntries));
        result.ProcessArguments.Should().BeEquivalentTo(expectedArguments.Split(" ", StringSplitOptions.RemoveEmptyEntries));
        result.AllowUnsafe.Should().Be(expectedAllowUnsafe);
    }
}