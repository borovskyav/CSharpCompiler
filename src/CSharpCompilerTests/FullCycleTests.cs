using Vostok.Logging.Console;

namespace CSharpCompilerTests;

public class FullCycleTests
{
    private CSharpSourceCodeRunner codeRunner;

    [SetUp]
    public void SetUp()
    {
        codeRunner = new CSharpSourceCodeRunner(new ConsoleLog(), new InProcessLibraryRunner());
    }
    
    [Test]
    public async Task SimpleConsoleWriteLineTest()
    {
        var data = new CSharpSourceCodeRunnerData(GetFilesPath("SimpleConsoleWriteLine.cs"), Array.Empty<string>());
        (await codeRunner.RunAsync(data)).Should().Be(0);
    }

    [Test]
    public async Task NewTemplateHelloWorldTest()
    {
        var data1 = new CSharpSourceCodeRunnerData(GetFilesPath("NewTemplateHelloWorld.cs"), Array.Empty<string>());
        (await codeRunner.RunAsync(data1)).Should().Be(1);
    }

    [Test]
    public async Task ThrowExceptionTest()
    {
        var data2 = new CSharpSourceCodeRunnerData(GetFilesPath("ThrowException.cs"), new []{"Hello", "World!"});
        Func<Task<int>> action = async () => await codeRunner.RunAsync(data2);
        (await action.Should().ThrowAsync<Exception>()).WithMessage("Hello, World! =(");
    }

    [Test]
    public async Task AsyncWorkTest()
    {
        var data = new CSharpSourceCodeRunnerData(GetFilesPath("AsyncWork.cs"), Array.Empty<string>());
        (await codeRunner.RunAsync(data)).Should().Be(0);
    }

    [Test, Ignore("todo: починить транзитивные зависимости")]
    public async Task TransitiveDependencyTest()
    {
        var data = new CSharpSourceCodeRunnerData(GetFilesPath("TransitiveDependency.cs"), Array.Empty<string>());
        (await codeRunner.RunAsync(data)).Should().Be(143);
    }

    [Test]
    public async Task MultipleDependenciesTest()
    {
        var data = new CSharpSourceCodeRunnerData(GetFilesPath("MultipleDependencies.cs"), Array.Empty<string>());
        (await codeRunner.RunAsync(data)).Should().Be(122);
    }

    [Test]
    public async Task MultipleFilesLoggerTest()
    {
        var data = new CSharpSourceCodeRunnerData(GetFilesInFolder("MultipleFilesLogger"), Array.Empty<string>());
        (await codeRunner.RunAsync(data)).Should().Be(37);
    }

    private IReadOnlyList<string> GetFilesInFolder(string folderName)
    {
        var gitDirectory = FindDirectoryUpRecursive(".git", AppDomain.CurrentDomain.BaseDirectory);
        if(gitDirectory == null)
            throw new Exception();

        var sourcesDirectory = Path.Combine(gitDirectory, "testSources", folderName);
        if (!Directory.Exists(sourcesDirectory))
            throw new Exception();

        return Directory.GetFiles(sourcesDirectory);
    }

    private IReadOnlyList<string> GetFilesPath(params string[] fileNames)
    {
        var gitDirectory = FindDirectoryUpRecursive(".git", AppDomain.CurrentDomain.BaseDirectory);
        if(gitDirectory == null)
            throw new Exception();

        return fileNames.Select(x => Path.Combine(gitDirectory, "testSources", x)).ToArray();
    }
    
    public static string? FindDirectoryUpRecursive(string directoryName, string fromDirName)
    {
        try
        {
            var currentDir = fromDirName;
            while(!string.IsNullOrWhiteSpace(currentDir))
            {
                if(Directory.Exists(Path.Combine(currentDir, directoryName)))
                    return currentDir;
                currentDir = Path.GetDirectoryName(currentDir);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}