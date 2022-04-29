namespace CSharpCompiler;

internal static class ExceptionExtensions
{
    public static void Throw(this Exception exception)
    {
#if DEBUG
        throw exception;
#endif
        Console.WriteLine(exception.Message ?? throw exception);
        Environment.Exit(1);
    }
}