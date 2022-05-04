using System;

namespace Timus;

class UnsafeTest
{
    static unsafe void SquarePtrParam(int* p)
    {
        *p *= *p;
    }

    static unsafe int Main(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException();
        var i = int.Parse(args[0]);
        SquarePtrParam(&i);
        Console.WriteLine(i);
        return i;
    }
}