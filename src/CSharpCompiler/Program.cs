// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CSharpCompilerTests")]

namespace CSharpCompiler;

internal class Program
{
    public static void Main(string[] arguments)
    {
        Console.WriteLine("Hello World!");
        
        if(arguments.Length == 0)
            new Exception().Throw(); // todo text
        
        var result = CompilerArgumentsParser.Parse(arguments);
    }
}