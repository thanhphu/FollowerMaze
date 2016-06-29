using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
