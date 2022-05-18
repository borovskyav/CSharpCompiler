using CSharpCompiler.CompileDirectoryManagement;

using Vostok.Logging.Console;

namespace CSharpCompilerTests;

public class CompileDirectoryManagerTests
{
    [SetUp]
    public void SetUp()
    {
        manager = new CompileDirectoryManager(new SynchronousConsoleLog(),
                                              "CompileDirectoryManagerTests",
                                              ApplicationConstants.OutputFileName);
    }

    [Test]
    public async Task Should_AcquireNewDirectory_WhenDoesNotExists()
    {
        var guids = Enumerable.Range(0, 5)
                              .Select(x => Guid.NewGuid().ToString())
                              .ToArray();

        var result = await manager.AcquireCompileDirectoryAsync(guids, true, CancellationToken.None);
        var tempDirPath = Path.Combine(Path.GetTempPath(), "CompileDirectoryManagerTests");
        result.DirectoryPath.Should().StartWith(tempDirPath);
        result.DllPath.Should().BeEquivalentTo(Path.Combine(result.DirectoryPath, ApplicationConstants.OutputFileName));
        result.DllExists.Should().BeFalse();
        result.FileLock.Should().NotBeNull();
    }

    [Test]
    public async Task Should_CreateManyFolders_WhenGuidSetsDoesNotEqual()
    {
        var guids = new List<string>();
        foreach(var i in Enumerable.Range(0, 10))
        {
            guids.Add(Guid.NewGuid().ToString());
            var result = await manager.AcquireCompileDirectoryAsync(guids.ToArray(), true, CancellationToken.None);
            var tempDirPath = Path.Combine(Path.GetTempPath(), "CompileDirectoryManagerTests");
            result.DirectoryPath.Should().StartWith(tempDirPath);
            result.DllPath.Should().BeEquivalentTo(Path.Combine(result.DirectoryPath, ApplicationConstants.OutputFileName));
            result.DllExists.Should().BeFalse();
            result.FileLock.Should().NotBeNull();
        }
    }

    [Test]
    public async Task Should_ReuseCurrentDll_WhenItExists()
    {
        var guids = Enumerable.Range(0, 5)
                              .Select(x => Guid.NewGuid().ToString())
                              .ToArray();

        var result1 = await manager.AcquireCompileDirectoryAsync(guids, true, CancellationToken.None);
        result1.FileLock.Should().NotBeNull();
        File.Create(result1.DllPath);

        var result2 = await manager.AcquireCompileDirectoryAsync(guids, true, CancellationToken.None);
        result2.Should().BeEquivalentTo(new CompileDirectoryAcquireResult(result1.DirectoryPath, result1.DllPath, null, true));
    }

    [Test]
    public async Task Should_CleanupFilesInDirectory_WhenItExists()
    {
        var files = Enumerable.Range(0, 5).Select(x => Guid.NewGuid() + ".txt").ToArray();
        var guids = Enumerable.Range(0, 5).Select(x => Guid.NewGuid().ToString()).ToArray();

        var result1 = await manager.AcquireCompileDirectoryAsync(guids, true, CancellationToken.None);
        result1.FileLock.Should().NotBeNull();
        foreach(var file in files)
            await File.Create(Path.Combine(result1.DirectoryPath, file)).DisposeAsync();

        result1.FileLock!.Dispose();
        var _ = await manager.AcquireCompileDirectoryAsync(guids, true, CancellationToken.None);
        foreach(var file in files)
            File.Exists(Path.Combine(result1.DirectoryPath, file)).Should().BeFalse();
    }

    [Test]
    public async Task Should_AcquireDll_AfterWaitForLockFile()
    {
        var guids = Enumerable.Range(0, 5).Select(x => Guid.NewGuid().ToString()).ToArray();
        var result1 = await manager.AcquireCompileDirectoryAsync(guids, true, CancellationToken.None);
        result1.FileLock.Should().NotBeNull();

        Task.Run(async () =>
            {
                await Task.Delay(5000);
                File.Create(result1.DllPath);
                result1.FileLock!.Dispose();
            });

        var result2 = await manager.AcquireCompileDirectoryAsync(guids, true, CancellationToken.None);
        result2.FileLock.Should().NotBeNull();
    }

    private CompileDirectoryManager manager;
}