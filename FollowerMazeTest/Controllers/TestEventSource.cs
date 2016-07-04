using FollowerMazeServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FollowerMazeTest.Controllers
{
    class TestEventSource: IDisposable
    {
        int NumberOfClients;
        int MessagesCount = 0;
        TcpClient Connection;
        StreamWriter Writer;
        Random R = new Random();

        public TestEventSource(int NumberOfClients)
        {
            this.NumberOfClients = Math.Max(NumberOfClients, 1);
        }

        public void Start()
        {
            Connection = new TcpClient();
            Connection.Connect(IPAddress.Loopback, Constants.EventSourcePort);
            Writer = new StreamWriter(Connection.GetStream());
        }

        public void SendMessages(int NumberOfMessages)
        {
            for (int i = 0; i < NumberOfMessages; i++)
            {
                Writer.WriteLine($"{MessagesCount}|P|{R.Next(NumberOfClients)}|{R.Next(NumberOfClients)}");
                MessagesCount++;
            }
            Writer.Flush();
        }

        public void Stop()
        {
            // Must have called start first
            if (Connection != null)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            Writer.Close();
            Connection.Close();
        }
    }
}
