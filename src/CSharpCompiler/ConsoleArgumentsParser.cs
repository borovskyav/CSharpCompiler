namespace CSharpCompiler;

internal static class ConsoleArgumentsParser
{
    public static CSharpSourceCodeRunnerData Parse(string[] arguments)
    {
        var delimiter = "--";
        var filesList = new List<string>();

        var index = 0;
        for(; index < arguments.Length && arguments[index] != delimiter; index++)
            filesList.Add(arguments[index]);

        return new CSharpSourceCodeRunnerData(
            filesList,
            index >= arguments.Length - 1 || arguments.Length == 0
                ? Array.Empty<string>()
                : arguments[(index + 1)..]
        );
    }
}