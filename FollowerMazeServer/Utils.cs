using System;

namespace FollowerMazeServer
{
    class Utils
    {
        public static void Log(string Message)
        {
#if DEBUG
            if (
                // !Message.Contains("Send") &&
                !Message.Contains("Received event") &&
                !Message.Contains("Remaining") &&
                // !Message.Contains("connected") &&
                !Message.Contains("Buffer"))
            {
                // Console.WriteLine(Message.TrimEnd());
            }
#else
#endif
        }

        public static void Log(byte[] Array)
        {
#if DEBUG
            // Console.WriteLine(System.Text.Encoding.UTF8.GetString(Array));
#else
#endif
        }
    }
}
