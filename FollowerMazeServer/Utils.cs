using System;

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
