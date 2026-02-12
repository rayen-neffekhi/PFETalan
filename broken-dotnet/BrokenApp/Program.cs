using System;

namespace BrokenApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting BrokenApp...");
            // Fixed: added semicolon and now calls existing method
            var result = Calculator.AddStrings("one", "two");
            Console.WriteLine($"Result: {result}");
        }
    }
}
