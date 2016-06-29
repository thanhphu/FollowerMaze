using System;

namespace FollowerMazeServer
{
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
