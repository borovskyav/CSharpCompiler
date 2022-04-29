namespace CSharpCompiler;

internal static class CompilerArgumentsParser
{
    public static ParseArgumentsResult Parse(string[] arguments)
    {
        var delimiter = "--";
        var filesList = new List<string>();

        var index = 0;
        for(; index < arguments.Length && arguments[index] != delimiter; index++)
            filesList.Add(arguments[index]);

        return new ParseArgumentsResult(
            filesList,
            index >= arguments.Length - 1 || arguments.Length == 0
                ? ""
                : string.Join(" ", arguments[(index + 1)..])
        );
    }
}