namespace CSharpCompilerTests;

// ReSharper disable once StaticMemberInGenericType
[TestFixture(typeof(FileBasedCommentExtractor))]
[TestFixture(typeof(RoslynSyntaxTreeCommentExtractor))]
internal class CommentExtractorTests<T> where T : ICSharpCommentExtractor, new()
{
    public CommentExtractorTests()
    {
        extractor = new T();
        treeBuilder = new RoslynSyntaxTreeBuilder();
    }

    [Test]
    [TestCaseSource(nameof(simpleCommentsTestFixtures))]
    public async Task SimpleCommentsTest(string fileName, string[] expectedComments)
    {
        var files = TestHelpers.GetFilesPath(fileName);
        var tree = await treeBuilder.BuildAsync(files);
        (await extractor.ExtractAsync(tree)).Should().BeEquivalentTo(expectedComments);
    }

    [Test]
    [TestCaseSource(nameof(multilineTestFixtures))]
    public async Task MultilineTest(string fileName, string[] expectedComments)
    {
        if(extractor is FileBasedCommentExtractor)
            Assert.Ignore("Can not realize this logic...");

        var files = TestHelpers.GetFilesPath(fileName);
        var tree = await treeBuilder.BuildAsync(files);
        (await extractor.ExtractAsync(tree)).Should().BeEquivalentTo(expectedComments);
    }

    private readonly ISyntaxTreeBuilder treeBuilder;
    private T extractor;

    public static object[] multilineTestFixtures =
        {
            new object[] { "ThrowException.cs", new[] { "/*\r\n    This file just\r\n    throws exception!\r\n*/" } },
            new object[] { "MultipleFilesLogger/2.cs",  new []
                {
                    "/* \r\n"
                    + "    Package: Vostok.Logging.Abstractions 1.0.23\r\n"
                    + "    Package: Vostok.Logging.Formatting 1.0.8\r\n"
                    + "*/",
                }},
            new object[]
                {
                    "JetbrainsExampleAsync.cs",
                    new[]
                        {
                            "// Package: RestSharp 107.3.0",
                            "/*\r\n"
                            + "                This example is same as\r\n"
                            + "                original jetbrains example\r\n"
                            + "                but library was updated\r\n"
                            + "                and async was used\r\n"
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
            new object[] { "MultipleFilesLogger/1.cs", new[] { "// Package: Vostok.Logging.Console 1.0.8", "// Package: Moq 4.17.2" } },
            new object[] { "SimpleConsoleWriteLine.cs", new[] { "// cw", "// Another WriteLine" } },
        };
}