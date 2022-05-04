using CSharpCompiler.CSharpCommentExtractor;
using CSharpCompiler.SyntaxTreeBuilder;

using Vostok.Logging.Console;

namespace CSharpCompilerTests;

// ReSharper disable once StaticMemberInGenericType
[TestFixture(typeof(FileBasedCommentExtractor))]
[TestFixture(typeof(RoslynSyntaxTreeCommentExtractor))]
internal class CommentExtractorTests<T> where T : ICSharpCommentExtractor, new()
{
    public CommentExtractorTests()
    {
        extractor = new T();
        treeBuilder = new RoslynSyntaxTreeBuilder(new RoslynDiagnosticResultAnalyzer(new ConsoleLog( )));
    }

    [Test]
    [TestCaseSource(nameof(simpleCommentsTestFixtures))]
    public async Task SimpleCommentsTest(string fileName, string[] expectedComments)
    {
        var files = TestHelpers.GetFilesPath(fileName);
        var tree = await treeBuilder.BuildAndAnalyzeAsync(files);
        (await extractor.ExtractAsync(tree)).Should().BeEquivalentTo(expectedComments);
    }

    [Test]
    [TestCaseSource(nameof(multilineTestFixtures))]
    public async Task MultilineTest(string fileName, string[] expectedComments)
    {
        if(extractor is FileBasedCommentExtractor)
            Assert.Ignore("Can not realize this logic...");

        var files = TestHelpers.GetFilesPath(fileName);
        var tree = await treeBuilder.BuildAndAnalyzeAsync(files);
        (await extractor.ExtractAsync(tree)).Should().BeEquivalentTo(expectedComments);
    }

    private readonly ISyntaxTreeBuilder treeBuilder;
    private T extractor;

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