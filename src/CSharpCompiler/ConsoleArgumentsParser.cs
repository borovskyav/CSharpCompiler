namespace CSharpCompiler;

internal static class ConsoleArgumentsParser
{
    public static CSharpSourceCodeRunnerData Parse(string[] arguments)
    {
        var compilerArgumentsHash = new HashSet<string>();
        var filesList = new List<string>();
        var programArgumentsList = new List<string>();

        var filesMeet = false;
        var delimiterMeet = false;
        
        foreach(var argument in arguments)
        {
            if(!filesMeet && argument.StartsWith(argumentsSuffix) && argument != delimiter)
            {
                compilerArgumentsHash.Add(argument);
                continue;
            }

            filesMeet = true;
            if(!delimiterMeet)
            {
                if(argument != delimiter)
                    filesList.Add(argument);
                else
                    delimiterMeet = true;
                continue;
            }

            programArgumentsList.Add(argument);
        }

        return new CSharpSourceCodeRunnerData(
            filesList,
            programArgumentsList,
            compilerArgumentsHash.Contains(allowUnsafeArgumentName)
        );
    }

    private const string allowUnsafeArgumentName = "-allowUnsafe";

    private const string delimiter = "--";
    private const string argumentsSuffix = "-";
}