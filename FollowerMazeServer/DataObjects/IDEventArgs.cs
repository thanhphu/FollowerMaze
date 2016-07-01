using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FollowerMazeServer
{
    /// <summary>
    /// Data class used in events to pass client ID around
    /// </summary>
    class IDEventArgs : EventArgs
    {
        public int ID { get; private set; }

        public IDEventArgs(int ID)
        {
            this.ID = ID;
        }
    }
}
