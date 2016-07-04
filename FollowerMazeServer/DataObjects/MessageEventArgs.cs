using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FollowerMazeServer.DataObjects
{    
    /// <summary>
    /// Data class used in events to pass message content around, currently only used in test
    /// </summary>
    [ExcludeFromCodeCoverage]
    class MessageEventArgs : EventArgs
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
