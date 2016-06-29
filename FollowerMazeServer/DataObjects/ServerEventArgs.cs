using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FollowerMazeServer
{
    class ServerEventArgs: EventArgs
    {
        public string ServerEvent { get; private set; }

        public ServerEventArgs(string Command)
        {
            this.ServerEvent = Command;
        }
    }
}
