// Package: Nancy.Hosting.Self 2.0.0
// Package: Nancy 1.4.1
// Package: Nancy.Authentication.Forms 1.4.1

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