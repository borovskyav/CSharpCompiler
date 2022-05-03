namespace CSharpCompiler.ExternalExecutableRunner;

internal interface IExternalExecutableRunner
{
    Task<int> RunAsync(string dllPath, IReadOnlyList<string> arguments);
}