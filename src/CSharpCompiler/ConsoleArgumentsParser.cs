namespace CSharpCompiler;

internal static class ConsoleArgumentsParser
{
    public static CSharpSourceCodeRunnerData Parse(string[] arguments)
    {
        var filesList = new List<string>();
        var hashSet = new HashSet<string>();
        
        var index = 0;
        for(; index < arguments.Length && arguments[index].StartsWith(argumentsSuffix); index++)
            hashSet.Add(arguments[index]);
        
        for(; index < arguments.Length && arguments[index] != delimiter; index++)
            filesList.Add(arguments[index]);

        var processArguments = index >= arguments.Length - 1 || arguments.Length == 0
                                   ? Array.Empty<string>()
                                   : arguments[(index + 1)..];
        return new CSharpSourceCodeRunnerData(
            filesList,
            processArguments,
            hashSet.Contains(allowUnsafeArgumentName)
        );
    }

    private const string allowUnsafeArgumentName = "-allowUnsafe";

    private const string delimiter = "--";
    private const string argumentsSuffix = "-";
}