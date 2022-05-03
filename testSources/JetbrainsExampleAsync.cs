// Package: RestSharp 107.3.0

using System;
using System.Threading.Tasks;
using RestSharp;

namespace ConsoleApp;

class Program 
{
    static async Task Main(string[] args) 
    {
        Console.WriteLine("Start fetching data...");
        var client = new RestClient("https://jsonplaceholder.typicode.com");
        foreach (var arg in args) {
            /*
                This example is same as
                original jetbrains example
                but library was updated
                and async was used
            */
            var request = new RestRequest("todos/{id}");
            request.AddUrlSegment("id", Int32.Parse(arg));
            Console.WriteLine(arg);
            Console.WriteLine((await client.GetAsync(request)).Content);
        }
    }
}