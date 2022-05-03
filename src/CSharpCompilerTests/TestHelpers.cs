namespace CSharpCompilerTests;

internal static class TestHelpers
{
    public static IReadOnlyList<string> GetFilesInFolder(string folderName)
    {
        var gitDirectory = FindDirectoryUpRecursive(".git", AppDomain.CurrentDomain.BaseDirectory);
        if(gitDirectory == null)
            throw new Exception();

        var sourcesDirectory = Path.Combine(gitDirectory, "testSources", folderName);
        if(!Directory.Exists(sourcesDirectory))
            throw new Exception();

        return Directory.GetFiles(sourcesDirectory);
    }

    public static IReadOnlyList<string> GetFilesPath(params string[] fileNames)
    {
        var gitDirectory = FindDirectoryUpRecursive(".git", AppDomain.CurrentDomain.BaseDirectory);
        if(gitDirectory == null)
            throw new Exception();

        return fileNames.Select(x => Path.Combine(gitDirectory, "testSources", x)).ToArray();
    }

    private static string? FindDirectoryUpRecursive(string directoryName, string fromDirName)
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