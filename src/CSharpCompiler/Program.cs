using System.Reflection;
using System.Runtime.CompilerServices;

using NuGet.Common;
using NuGet.Frameworks;

[assembly: InternalsVisibleTo("CSharpCompilerTests")]

namespace CSharpCompiler;

internal class Program
{
    public static async Task<int> Main(string[] arguments)
    {
        var token = new CancellationToken();
        
        try
        {
            if(arguments.Length == 0)
                throw new Exception("arguments.Length"); // todo text

            // todo прибраться в выводах файлов на консоль
            var parseArgumentsResult = CompilerArgumentsParser.Parse(arguments);
            var files = parseArgumentsResult.FilesPath;
            var unexistFiles = GetUnexistFiles(files);
            if(unexistFiles.Count > 0)
                throw new FileNotFoundException($"One of more files not found: {string.Join(Environment.NewLine, unexistFiles)}");

            var filesSyntaxTree = new FilesSyntaxTree(files);

            var dllDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString());
            Directory.CreateDirectory(dllDirectory);

            var packagesFiles = await GetExternalLibraries(filesSyntaxTree, dllDirectory, token);
            foreach(var packagesFile in packagesFiles)
                Assembly.LoadFrom(packagesFile);
            
            foreach(var packagesFile in packagesFiles)
                Console.WriteLine(packagesFile);

            var compilationResult = RoslynGames.Compile(dllDirectory, filesSyntaxTree.Trees, packagesFiles);
            if(!compilationResult.Success)
                throw new Exception(compilationResult.ToString());
            Console.WriteLine(compilationResult.ToString());

            return await new InProcessCodeRunner().Run(compilationResult.DllPath, parseArgumentsResult.ProgramArguments.Split(" "));
        }
        catch(Exception exception)
        {
#if DEBUG
            throw;
#endif
            if(string.IsNullOrEmpty(exception.Message))
                throw;

            Console.WriteLine(exception.Message);
            Environment.Exit(1);
        }
    }

    private static async Task<IReadOnlyList<string>> GetExternalLibraries(FilesSyntaxTree filesSyntaxTree, string dllDirectory, CancellationToken token)
    {
        var nugetPackages = NugetPackagesParser.Parse(filesSyntaxTree.GetAllCommentRows());
        if(nugetPackages.Count != 0)
        {
            var packages = await new NugetPackagesDownloader().DownloadAsync(nugetPackages, token);
            if(packages.Any(x => !x.Found))
            {
                LogNotFoundPackages(packages);
                Environment.Exit(1);
            }
            LogDownloadedPackages(packages);
            return (await GetPackagesFiles(packages, dllDirectory, token))
                   .Where(x => Path.GetExtension(x) == ".dll")
                   .ToArray();
        }

        Console.WriteLine("No included nuget packages, skip downloading packages...");
        return Array.Empty<string>();
    }

    private static async Task<IReadOnlyList<string>> GetPackagesFiles(IReadOnlyList<DownloadPackageResult> packages, string dllDirectory, CancellationToken token)
    {
        var myFramework = NuGetFramework.Parse("net6.0");
        var list = new List<string>();
        foreach(var package in packages)
        {
            var nugetResult = package.NugetResult!;
            var supportedFrameworks = await nugetResult.PackageReader.GetSupportedFrameworksAsync(token);
            var packageFramework = supportedFrameworks.FirstOrDefault(x => DefaultCompatibilityProvider.Instance.IsCompatible(myFramework, x));
            if(packageFramework == null)
                throw new Exception("packageFramework");
            Console.WriteLine(packageFramework);

            var targetFrameworkFiles = (await nugetResult
                                              .PackageReader
                                              .GetLibItemsAsync(token))
                .First(x => x.TargetFramework == packageFramework);

            if(targetFrameworkFiles == null)
                throw new Exception("targetFrameworkFiles");

            // todo caching (??)
            list.AddRange(await nugetResult.PackageReader.CopyFilesAsync(
                              dllDirectory,
                              targetFrameworkFiles.Items,
                              ExtractFile,
                              NullLogger.Instance,
                              token
                          ));
        }

        return list;
    }
    
    private static string ExtractFile(string sourcePath, string targetPath, Stream sourceStream)
    {
        using var targetStream = File.OpenWrite(targetPath);
        sourceStream.CopyTo(targetStream);

        return targetPath;
    }

    public static void LogDownloadedPackages(IReadOnlyList<DownloadPackageResult> packages)
    {
        Console.WriteLine($"Packages downloaded: {packages.Count}");
        var dict = packages
            .GroupBy(x => x.FromCache)
            .ToDictionary(x => x.Key);
        if (dict.ContainsKey(true))
            Console.WriteLine($"From cache:{Environment.NewLine}{string.Join(Environment.NewLine, dict[true].Select(x => $"{x.PackageId}: {x.Version}"))}");
        if (dict.ContainsKey(false))
            Console.WriteLine($"From remote:{Environment.NewLine}{string.Join(Environment.NewLine, dict[true].Select(x => $"{x.PackageId}: {x.Version}"))}");
    }

    public static void LogNotFoundPackages(IReadOnlyList<DownloadPackageResult> packages)
    {
        var notFound = packages.Where(x => !x.Found);
        Console.WriteLine($"Error when download packages, some packages not found:{Environment.NewLine}"
                          + $"{string.Join(Environment.NewLine, notFound.Select(x => $"{x.PackageId}: {x.Version}"))}");
    }

    public static List<string> GetUnexistFiles(IReadOnlyList<string> filesPath)
        => filesPath
           .Select(filePath => Path.IsPathRooted(filePath) ? filePath : Path.GetFullPath(filePath))
           .Where(filePath => !File.Exists(filePath))
           .ToList();
}