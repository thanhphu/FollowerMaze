using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FollowerMazeServer
{
    class Utils
    {
        public static void Log(string Message)
        {
#if DEBUG
            Console.WriteLine(Message);
#else
#endif
        }

        public static void Log(byte[] Array)
        {
#if DEBUG
            Console.WriteLine(System.Text.Encoding.UTF8.GetString(Array));
#else
#endif
        }
    }
}
