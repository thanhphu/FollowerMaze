using System.Net;

namespace FollowerMazeServer
{
    /// <summary>
    /// Constants used in the app, can be put into configuration file if needed
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// On screen tatus update interval
        /// </summary>
        public static readonly int StatusInterval = 1000; // in ms

        /// <summary>
        /// Buffer size for reading from network
        /// </summary>
        public static readonly int BufferSize = 1024; // in bytes

        /// <summary>
        /// Time between each iteration inside client's thread. Messages will only be sent out once during this perioud
        /// </summary>
        public static readonly int WorkerDelay = 100; // in ms. 

        /// <summary>
        /// Listening IP Address
        /// </summary>
        public static readonly IPAddress IP = IPAddress.Any;

        /// <summary>
        /// Port to listen to event source
        /// </summary>
        public static readonly int EventSourcePort = 9090;

        /// <summary>
        /// Port to listen for client connections
        /// </summary>
        public static readonly int ClientConnectionPort = 9099;
    }
}
