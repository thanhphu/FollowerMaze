using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FollowerMazeServer
{
    /// <summary>
    /// Logging support, could've used Log4Net, but we should minimize external framework usage
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class Logger
    {
#if DEBUG
        private static List<string> Buffer = new List<string>();
        private const string LocalLog = "E:\\Log.txt";
        private static string LogFilePath = "Log.txt";
        private static bool First = true;
#else
#endif

        /// <summary>
        /// Write log to file, with buffer
        /// </summary>
        /// <param name="Message">string to write</param>
        public static void Log(string Message)
        {
#if DEBUG
            if (First)
            {
                // Dev machine?
                if (System.IO.File.Exists(LocalLog))
                    LogFilePath = LocalLog;
                First = false;
                System.IO.File.Delete(LogFilePath);
            }
            lock (Buffer)
            {
                Buffer.Add(Message.TrimEnd());
            }
            if (Buffer.Count > 1000)
            {
                FlushLog();
            }

#else
#endif
        }

        /// <summary>
        /// Write log to file
        /// </summary>
        /// <param name="Array">array to write</param>
        public static void Log(byte[] Array)
        {
#if DEBUG
            Buffer.Add(System.Text.Encoding.UTF8.GetString(Array));
#else
#endif
        }

        /// <summary>
        /// Write all log entries in buffer to file
        /// </summary>
        public static void FlushLog()
        {
#if DEBUG
            lock (Buffer)
            {
                System.IO.File.AppendAllLines(LogFilePath, Buffer);
                Buffer.Clear();
            }
#else
#endif
        }

        /// <summary>
        /// Write a status message on screen, can be updated inline
        /// </summary>
        /// <param name="Message"></param>
        public static void Status(string Message)
        {
            Console.Write("\r" + DateTime.Now.ToLongTimeString() + " " + Message.PadRight(100));
        }

        /// <summary>
        /// Write a status message on screen, then a new line
        /// </summary>
        /// <param name="Message"></param>
        public static void StatusLine(string Message)
        {
            Status(Message);
            Console.WriteLine();
        }
    }
}