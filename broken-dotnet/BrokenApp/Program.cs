using System;

namespace BrokenApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting BrokenApp...");
            // Intentional compile-time errors: calling a non-existent method and missing semicolon
            var result = Calculator.AddStrings("one", "two")
            Console.WriteLine($"Result: {result}");
        }
    }
}
