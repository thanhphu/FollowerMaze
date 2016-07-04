using System;
using System.Diagnostics.CodeAnalysis;

namespace FollowerMazeServer.DataObjects
{
    /// <summary>
    /// Data class used in events to pass message content around, currently only used in test
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class MessageEventArgs : EventArgs
    {
        public int ClientID { get; private set; }
        public string Message { get; private set; }

        public MessageEventArgs(int ClientID, string Message)
        {
            this.ClientID = ClientID;
            this.Message = Message;
        }
    }
}