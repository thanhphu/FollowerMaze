using System;
using System.Diagnostics.CodeAnalysis;

namespace FollowerMazeServer
{
    [ExcludeFromCodeCoverage]
    class Program
    {
        static void Main(string[] args)
        {
            EventListener L = new EventListener();
            L.Start();
            Console.WriteLine("Press ENTER to stop listening");
            Console.ReadLine();
            L.Stop();
        }
    }
}
