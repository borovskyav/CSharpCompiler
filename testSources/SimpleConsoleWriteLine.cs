using System;

namespace Program;

public class Program
{
    public static void Main(string[] args)
    {
        foreach (var s in args)
	    {
            // cw
            Console.WriteLine(s);
            if (false)
            {
                // Another WriteLine
                Console.WriteLine(s); 
            }
	    }
    }
}