using System.Reflection;

namespace CSharpCompiler;

internal interface IExternalLibraryRunner
{
    Task<int> Run(string dllPath, IReadOnlyList<string> arguments);
}

internal class InProcessLibraryRunner : IExternalLibraryRunner
{
    public async Task<int> Run(string dllPath, IReadOnlyList<string> arguments)
    {
        var assembly = Assembly.LoadFrom(dllPath);

        var mainList = new List<(Type type, MethodInfo mi)>();
        foreach(var type in assembly.GetTypes())
        {
            var mi = type.GetMethod("Main", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if(mi != null)
                mainList.Add((type, mi));
        }

        if(mainList.Count == 0)
            throw new Exception("0");

        if(mainList.Count > 1)
            throw new Exception(">1");

        var result = mainList[0].mi.Invoke(mainList[0].type, new object?[] { arguments });
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
}