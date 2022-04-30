using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CSharpCompilerTests")]

namespace CSharpCompiler;

internal class Program
{
    public static void Main(string[] arguments)
    {
        try
        {
            if(arguments.Length == 0)
                throw new Exception("arguments.Length"); // todo text

            // todo прибраться в выводах файлов на консоль
            var result = CompilerArgumentsParser.Parse(arguments);
            var files = result.FilesPath;
            var unexistFiles = GetUnexistFiles(files);
            if(unexistFiles.Count > 0)
                throw new FileNotFoundException($"One of more files not found: {string.Join(Environment.NewLine, unexistFiles)}");

            var filesSyntaxTree = new FilesSyntaxTree(files);

            var nugetPackages = NugetPackagesParser.Parse(filesSyntaxTree.GetAllCommentRows());
            if(nugetPackages.Count != 0)
                throw new NotImplementedException();
            else
                Console.WriteLine("No included nuget packages, skip downloading packages...");

            var dllDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString());
            Directory.CreateDirectory(dllDirectory);

            var compilationResult = RoslynGames.Compile(dllDirectory, filesSyntaxTree.Trees);
            if(!compilationResult.Success)
                throw new Exception(compilationResult.ToString());
            Console.WriteLine(compilationResult.ToString());

            new InProcessCodeRunner().Run(compilationResult.DllPath, result.ProgramArguments.Split(" "));
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

    public static List<string> GetUnexistFiles(IReadOnlyList<string> filesPath)
        => filesPath
           .Select(filePath => Path.IsPathRooted(filePath) ? filePath : Path.GetFullPath(filePath))
           .Where(filePath => !File.Exists(filePath))
           .ToList();
}