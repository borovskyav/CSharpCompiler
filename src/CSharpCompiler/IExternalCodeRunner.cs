using System.Reflection;

namespace CSharpCompiler;

internal interface IExternalCodeRunner
{
    void Run(string dllPath, string[] arguments);
}

internal class InProcessCodeRunner : IExternalCodeRunner
{
    public void Run(string dllPath, string[] arguments)
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

        mainList[0].mi.Invoke(mainList[0].type, new object?[] { arguments });
    }
}