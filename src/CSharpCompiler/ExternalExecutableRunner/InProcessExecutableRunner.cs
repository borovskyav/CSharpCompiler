using System.Reflection;

using Microsoft.CodeAnalysis;

using Vostok.Logging.Abstractions;

namespace CSharpCompiler.ExternalExecutableRunner;

internal class InProcessExecutableRunner : IExternalExecutableRunner
{
    public InProcessExecutableRunner(ILog logger)
    {
        this.logger = logger.ForContext<InProcessExecutableRunner>();
    }

    public async Task<int> RunAsync(string dllPath, IReadOnlyList<string> arguments)
    {
        var assembly = Assembly.LoadFrom(dllPath);

        var mainList = new List<(Type type, MethodInfo methodInfo)>();
        foreach(var type in assembly.GetTypes())
            mainList.AddRange(TryGetMainMethodInfo(type).Select(x => (type, x)));

        if(mainList.Count == 0)
            throw new Exception("There is no method \"Main\" in executable dll");

        if(mainList.Count > 1)
            throw new Exception("There are two or more Main methods in executable dll");

        logger.Info("Start external application");
        var result = InvokeMethod(mainList[0].type, mainList[0].methodInfo, arguments);
        return await HandleResult(result);
    }

    private IEnumerable<MethodInfo> TryGetMainMethodInfo(Type type)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        return possibleMainNames
               .Select(possibleMainName => type.GetMethod(possibleMainName, bindingFlags))
               .Where(methodInfo => methodInfo != null)
               .Where(methodInfo => validReturnTypes.Contains(methodInfo!.ReturnType))!;
    }

    private object? InvokeMethod(Type type, MethodInfo methodInfo, IReadOnlyList<string> arguments)
    {
        try
        {
            return methodInfo.Invoke(type, new object?[] { arguments.ToArray() });
        }
        catch(TargetInvocationException exception) when(exception.InnerException != null)
        {
            throw exception.InnerException;
        }
    }

    private async Task<int> HandleResult(object? result)
    {
        if(result == null)
            return 0;

        switch(result)
        {
        case Task<int> res:
            return await res;
        case Task res:
            await res;
            return 0;
        case int res:
            return res;
        default:
            return 0;
        }
    }

    private readonly ILog logger;

    private readonly HashSet<Type> validReturnTypes = new() { typeof(Task<int>), typeof(Task), typeof(int), typeof(void) };
    private readonly string[] possibleMainNames = { WellKnownMemberNames.EntryPointMethodName, WellKnownMemberNames.TopLevelStatementsEntryPointMethodName };
}