using System;
using System.Diagnostics.CodeAnalysis;

namespace FollowerMazeServer
{
    /// <summary>
    /// Data class used in events to pass client ID around
    /// </summary>
    [ExcludeFromCodeCoverage]
    class IDEventArgs : EventArgs
    {
        public int ID { get; private set; }

        public IDEventArgs(int ID)
        {
            this.ID = ID;
        }
    }
}
