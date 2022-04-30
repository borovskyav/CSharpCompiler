using System;
using System.Threading.Tasks;

namespace ConsoleApp;

class Program 
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Top");
        await Task.Delay(100);
        Console.WriteLine("Middle");
        await Task.Delay(100);
        Console.WriteLine("Bottom");
    }
}