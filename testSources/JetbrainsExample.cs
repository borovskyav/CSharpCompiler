using System;
using RestSharp;

namespace ConsoleApp;

class Program 
{
    static void Main(string[] args) 
    {
        var client = new RestClient("https://jsonplaceholder.typicode.com");
        foreach (var arg in args) {
            // Package: RestSharp 106.6.7
            var request = new RestRequest("todos/{id}");
            request.AddUrlSegment("id", Int32.Parse(arg));
            Console.WriteLine(arg);
            Console.WriteLine(client.Get(request).Content);
        }
    }
}