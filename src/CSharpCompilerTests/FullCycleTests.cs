using CSharpCompiler.CompileDirectoryManagement;
using CSharpCompiler.CSharpCommentExtractor;
using CSharpCompiler.CSharpCompiler;
using CSharpCompiler.ExternalExecutableRunner;
using CSharpCompiler.NugetPackageLibrariesExtracting;
using CSharpCompiler.NugetPackagesDownloader;
using CSharpCompiler.SyntaxTreeBuilder;

using Vostok.Logging.Console;

namespace CSharpCompilerTests;

public class FullCycleTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "CSharpCompilerTests");
        if(!Directory.Exists(tempPath))
            return;
        var di = new DirectoryInfo(tempPath);
        foreach(var dir in di.GetDirectories())
            dir.Delete(true);
    }

    [SetUp]
    public void SetUp()
    {
        ApplicationConstants.ApplicationName = "CSharpCompilerTests";
        ApplicationConstants.OutputFileName = $"Generated-{Guid.NewGuid().ToString()}.dll";

        var logger = new ConsoleLog();
        var roslynDiagnosticResultAnalyzer = new RoslynDiagnosticResultAnalyzer(logger);
        codeRunner = new CSharpSourceCodeRunner(
            new CompileDirectoryManager(logger, ApplicationConstants.ApplicationName, ApplicationConstants.OutputFileName),
            new RoslynSyntaxTreeBuilder(roslynDiagnosticResultAnalyzer),
            new RoslynSyntaxTreeCommentExtractor(),
            new NugetPackagesParser(logger),
            new NugetClientBasedPackagesDownloader(logger, ApplicationConstants.Framework, ApplicationConstants.Runtime),
            new NugetPackageLibrariesExtractor(logger, ApplicationConstants.Framework),
            new RoslynCSharpCompiler(roslynDiagnosticResultAnalyzer, logger),
            new InProcessExecutableRunner(logger));
    }

    [Test]
    public async Task SimpleConsoleWriteLineTest()
    {
        var data = new CSharpSourceCodeRunnerData(
            TestHelpers.GetFilesPath("SimpleConsoleWriteLine.cs"),
            Array.Empty<string>(),
            false
        );
        (await codeRunner!.RunAsync(data)).Should().Be(0);
    }

    [Test]
    public async Task NewTemplateHelloWorldTest()
    {
        var data1 = new CSharpSourceCodeRunnerData(
            TestHelpers.GetFilesPath("NewTemplateHelloWorld.cs"),
            Array.Empty<string>(),
            false
        );
        (await codeRunner!.RunAsync(data1)).Should().Be(1);
    }

    [Test]
    public async Task ThrowExceptionTest()
    {
        var data2 = new CSharpSourceCodeRunnerData(
            TestHelpers.GetFilesPath("ThrowException.cs"),
            new[] { "Hello", "World!" },
            false
        );
        Func<Task<int>> action = async () => await codeRunner!.RunAsync(data2);
        (await action.Should().ThrowAsync<Exception>()).WithMessage("Hello, World! =(");
    }

    [Test]
    public async Task AsyncWorkTest()
    {
        var data = new CSharpSourceCodeRunnerData(
            TestHelpers.GetFilesPath("AsyncWork.cs"),
            Array.Empty<string>(),
            false
        );
        (await codeRunner!.RunAsync(data)).Should().Be(0);
    }

    [Test]
    public async Task TransitiveDependencyTest()
    {
        var data = new CSharpSourceCodeRunnerData(
            TestHelpers.GetFilesPath("TransitiveDependency.cs"),
            Array.Empty<string>(),
            false
        );
        (await codeRunner!.RunAsync(data)).Should().Be(143);
    }

    [Test]
    public async Task MultipleDependenciesTest()
    {
        var data = new CSharpSourceCodeRunnerData(
            TestHelpers.GetFilesPath("MultipleDependencies.cs"),
            Array.Empty<string>(),
            false
        );
        (await codeRunner!.RunAsync(data)).Should().Be(122);
    }

    [Test]
    public async Task MultipleFilesLoggerTest()
    {
        var data = new CSharpSourceCodeRunnerData(
            TestHelpers.GetFilesInFolder("MultipleFilesLogger"),
            new[] { "print", "something" },
            false);
        (await codeRunner!.RunAsync(data)).Should().Be(0);
    }

    [Test]
    public async Task PackageFrameworkNotFoundTest()
    {
        var data = new CSharpSourceCodeRunnerData(
            TestHelpers.GetFilesPath("PackageFrameworkNotFound.cs"),
            new[] { "print", "something" },
            false);
        Func<Task<int>> act = async () => await codeRunner!.RunAsync(data);
        await act.Should().ThrowAsync<Exception>().WithMessage("Package Nancy.Hosting.Self found but package framework did not resolved");
    }

    [Test]
    public async Task UnsafeCodeTest()
    {
        var data = new CSharpSourceCodeRunnerData(
            TestHelpers.GetFilesPath("UnsafeCode.cs"),
            new[] { "10" },
            false);
        Func<Task<int>> act = async () => await codeRunner!.RunAsync(data);
        await act.Should().ThrowAsync<Exception>().WithMessage("Compilation failed");

        var data1 = new CSharpSourceCodeRunnerData(
            TestHelpers.GetFilesPath("UnsafeCode.cs"),
            new[] { "10" },
            true);
        (await codeRunner!.RunAsync(data1)).Should().Be(100);
    }

    private CSharpSourceCodeRunner? codeRunner;
}