using Vostok.Logging.Console;

namespace CSharpCompilerTests;

public class FullCycleTests
{
    [SetUp]
    public void SetUp()
    {
        var stBuilder = new RoslynSyntaxTreeBuilder();
        var commentExtractor = new FileBasedCommentExtractor();
        var runner = new InProcessExecutableRunner();
        codeRunner = new CSharpSourceCodeRunner(new ConsoleLog(), stBuilder, commentExtractor, runner);
    }

    [Test]
    public async Task SimpleConsoleWriteLineTest()
    {
        var data = new CSharpSourceCodeRunnerData(TestHelpers.GetFilesPath("SimpleConsoleWriteLine.cs"), Array.Empty<string>());
        (await codeRunner!.RunAsync(data)).Should().Be(0);
    }

    [Test]
    public async Task NewTemplateHelloWorldTest()
    {
        var data1 = new CSharpSourceCodeRunnerData(TestHelpers.GetFilesPath("NewTemplateHelloWorld.cs"), Array.Empty<string>());
        (await codeRunner!.RunAsync(data1)).Should().Be(1);
    }

    [Test]
    public async Task ThrowExceptionTest()
    {
        var data2 = new CSharpSourceCodeRunnerData(TestHelpers.GetFilesPath("ThrowException.cs"), new[] { "Hello", "World!" });
        Func<Task<int>> action = async () => await codeRunner!.RunAsync(data2);
        (await action.Should().ThrowAsync<Exception>()).WithMessage("Hello, World! =(");
    }

    [Test]
    public async Task AsyncWorkTest()
    {
        var data = new CSharpSourceCodeRunnerData(TestHelpers.GetFilesPath("AsyncWork.cs"), Array.Empty<string>());
        (await codeRunner!.RunAsync(data)).Should().Be(0);
    }

    [Test]
    [Ignore("todo: починить транзитивные зависимости")]
    public async Task TransitiveDependencyTest()
    {
        var data = new CSharpSourceCodeRunnerData(TestHelpers.GetFilesPath("TransitiveDependency.cs"), Array.Empty<string>());
        (await codeRunner!.RunAsync(data)).Should().Be(143);
    }

    [Test]
    public async Task MultipleDependenciesTest()
    {
        var data = new CSharpSourceCodeRunnerData(TestHelpers.GetFilesPath("MultipleDependencies.cs"), Array.Empty<string>());
        (await codeRunner!.RunAsync(data)).Should().Be(122);
    }

    [Test]
    public async Task MultipleFilesLoggerTest()
    {
        var data = new CSharpSourceCodeRunnerData(TestHelpers.GetFilesInFolder("MultipleFilesLogger"), new[] { "print", "something" });
        (await codeRunner!.RunAsync(data)).Should().Be(0);
    }

    private CSharpSourceCodeRunner? codeRunner;
}