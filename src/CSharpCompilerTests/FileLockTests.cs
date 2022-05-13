using CSharpCompiler.CompileDirectoryManagement;

namespace CSharpCompilerTests;

public class FileLockTests
{
    [Test]
    public async Task FileLock_StressTest()
    {
        const int tasksCount = 50;
        const int iterations = 10;
        var lockFileDirectory = AppDomain.CurrentDomain.BaseDirectory;
        
        var verificationSemaphore = new SemaphoreSlim(1);

        var tasks = Enumerable.Range(0, tasksCount).Select(LoopAsync);
        var results = (await Task.WhenAll(tasks)).SelectMany(r => r).ToArray();

        results.Length.Should().Be(tasksCount * iterations);
        results.Should().NotContain(false);

        async Task<List<bool>> LoopAsync(int taskNumber)
        {
            var loopResults = new List<bool>();
            foreach(var iteration in Enumerable.Range(0, iterations))
            {
                Console.WriteLine($"Task number: {taskNumber}, iteration: {iteration}");
                using(await FileLock.CreateAsync(lockFileDirectory))
                {
                    var acquired = await verificationSemaphore.WaitAsync(0);
                    if(!acquired)
                    {
                        loopResults.Add(false);
                        continue;
                    }
                    
                    await Task.Delay(TimeSpan.FromMilliseconds(1));
                    verificationSemaphore.Release();
                    loopResults.Add(true);
                }
            }

            return loopResults;
        }
    }
}