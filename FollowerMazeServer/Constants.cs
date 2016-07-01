using System.Net;

namespace FollowerMazeServer
{
    public static class Constants
    {
        public static readonly int BufferSize = 1024; // in bytes
        // Time between each iteration inside client's thread. Messages will only be sent out once during this perioud
        public static readonly int WorkerDelay = 100; // in ms. 
        public static readonly IPAddress IP = IPAddress.Any;
        public static readonly int EventSourcePort = 9090;
        public static readonly int ClientConnectionPort = 9099;
        // Number of processed events to store in memory before purging, higher means better performance but worse memory usage
        public static readonly int ProcessedEventLimit = 100000;
    }
}
