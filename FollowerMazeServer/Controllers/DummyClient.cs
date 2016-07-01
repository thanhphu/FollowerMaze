using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace FollowerMazeServer.Controllers
{
    // "Dummy" client, doesn't connect, only have a list of followers
    class DummyClient: AbstractClient
    {
        protected DummyClient()
        {
        }
        
        public DummyClient(int ID)
        {
            this.ClientID = ID;
            InvokeIDEvent();
        }
    }
}
