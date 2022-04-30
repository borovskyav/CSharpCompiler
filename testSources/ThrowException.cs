using System;
using System.Threading.Tasks;

namespace ConsoleApp;

class Program 
{
    static void Main(string[] args)
    {
        throw new Exception(string.Join(", ", args) + " =(");
    }
}