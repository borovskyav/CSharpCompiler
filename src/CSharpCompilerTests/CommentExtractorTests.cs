using CSharpCompiler.CSharpCommentExtractor;
using CSharpCompiler.SyntaxTreeBuilder;

using Vostok.Logging.Console;

namespace CSharpCompilerTests;

internal class CommentExtractorTests
{
    public CommentExtractorTests()
    {
        extractor = new RoslynSyntaxTreeCommentExtractor();
        treeBuilder = new RoslynSyntaxTreeBuilder(new RoslynDiagnosticResultAnalyzer(new ConsoleLog()));
    }

    [Test]
    [TestCaseSource(nameof(simpleCommentsTestFixtures))]
    public async Task SimpleCommentsTest(string fileName, string[] expectedComments)
    {
        var files = TestHelpers.GetFilesPath(fileName);
        var tree = treeBuilder.BuildAndAnalyzeTreeAsync(files.Select(File.ReadAllText).ToArray());
        (await extractor.ExtractAsync(tree)).Should().BeEquivalentTo(expectedComments);
    }

    [Test]
    [TestCaseSource(nameof(multilineTestFixtures))]
    public async Task MultilineTest(string fileName, string[] expectedComments)
    {
        var filesPath = TestHelpers.GetFilesPath(fileName);
        var files = filesPath.Select(File.ReadAllText).Select(x => x.Replace("\r\n", Environment.NewLine)).ToArray();
        var tree = treeBuilder.BuildAndAnalyzeTreeAsync(files);
        (await extractor.ExtractAsync(tree)).Should().BeEquivalentTo(expectedComments);
    }

    private readonly ISyntaxTreeBuilder treeBuilder;
    private readonly ICSharpCommentExtractor extractor;

    public static object[] multilineTestFixtures =
        {
            new object[]
                {
                    "ThrowException.cs", new[]
                        {
                            "/*"
                            + Environment.NewLine
                            + "    This file just"
                            + Environment.NewLine
                            + "    throws exception!"
                            + Environment.NewLine
                            + "*/",
                        },
                },
            new object[]
                {
                    "MultipleFilesLogger/2.cs", new[]
                        {
                            "// Package: Vostok.Logging.Console 1.0.3", "/* "
                                                                        + Environment.NewLine
                                                                        + "    Package: Vostok.Logging.Abstractions 1.0.23"
                                                                        + Environment.NewLine
                                                                        + "    Package: Vostok.Logging.Formatting 1.0.8"
                                                                        + Environment.NewLine
                                                                        + "*/",
                        },
                },
            new object[]
                {
                    "JetbrainsExampleAsync.cs", new[]
                        {
                            "// Package: RestSharp 107.3.0", "/*"
                                                             + Environment.NewLine
                                                             + "                This example is same as"
                                                             + Environment.NewLine
                                                             + "                original jetbrains example"
                                                             + Environment.NewLine
                                                             + "                but library was updated"
                                                             + Environment.NewLine
                                                             + "                and async was used"
                                                             + Environment.NewLine
                                                             + "            */",
                        },
                },
        };

    public static object[] simpleCommentsTestFixtures =
        {
            new object[] { "JetbrainsExample.cs", new[] { "// Package: RestSharp 106.6.7" } },
            new object[]
                {
                    "MultipleDependencies.cs",
                    new[] { "// Package: Vostok.Logging.Abstractions 1.0.23", "// Package: Vostok.Logging.Formatting 1.0.8", "// Package: Vostok.Logging.Console 1.0.8" },
                },
            new object[]
                {
                    "MultipleFilesLogger/1.cs",
                    new[] { "// Package: Vostok.Logging.Console 1.0.8", "// Package: Moq 4.17.2", "// Package: Vostok.Logging.Abstractions 1.0.1", }
                },
            new object[] { "SimpleConsoleWriteLine.cs", new[] { "// cw", "// Another WriteLine" } },
        };
}