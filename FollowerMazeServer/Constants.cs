using System.Net;

namespace FollowerMazeServer
{
    public static class Constants
    {
        public static readonly int BufferSize = 1024; // in bytes
        public static readonly int WorkerDelay = 50; // in ms
        public static readonly IPAddress IP = IPAddress.Any;
        public static readonly int EventSourcePort = 9090;
        public static readonly int ClientConnectionPort = 9099;
        public static readonly int GracePeriod = 2000; // Wait this amount of time before sending out messages (to wait for messages to accumulate in order)
        public static readonly int RetryLimit = 5; // Number of times a payload will be retried before it gets discarded
    }
}
