using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FollowerMazeServer
{
    class Utils
    {
        public static int? FindNewLine(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length - 1; i++)
            {
                // The new line
                if (buffer[i] == '\r' && buffer[i + 1] == '\n')
                    return i;
            }
            return null;
        }

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
