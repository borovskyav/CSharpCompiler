using System;
using System.Threading.Tasks;

namespace ConsoleApp;

/*
    This file just
    throws exception!
*/

class Program 
{
    static void Main(string[] args)
    {
        throw new Exception(string.Join(", ", args) + " =(");
    }
}