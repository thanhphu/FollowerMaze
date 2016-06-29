using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FollowerMazeServer
{
    class IDEventArgs : EventArgs
    {
        public int ID { get; private set; }

        public IDEventArgs(int ID)
        {
            this.ID = ID;
        }
    }
}
