using FollowerMazeServer.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace FollowerMazeServer
{
    class ConnectedClient: AbstractClient
    {
        Thread Worker = null;
        TcpClient Connection = null;

        public ConnectedClient(TcpClient Connection)
        {
            Messages = new Queue<Payload>();
            Connection.ReceiveTimeout = -1;
            Connection.SendTimeout = -1;
            this.Connection = Connection;
            Worker = new Thread(new ThreadStart(ClientMessageHandling));
        }

        public void TakeOverFrom(AbstractClient Other)
        {
            this.Followers = new List<int>(Other.GetCurrentFollowers());
            this.Messages = new Queue<Payload>(Other.GetMessages());
        }

        // Buffer to read client ID, shared between ProcessClientID and ClientMessageHandling
        byte[] Incoming = new byte[Constants.BufferSize];

        private void ProcessClientID(IAsyncResult AR)
        {
            NetworkStream networkStream = (NetworkStream)AR.AsyncState;
            int ReadBytes = networkStream.EndRead(AR);

            // Read client ID            
            string ID = System.Text.Encoding.UTF8.GetString(Incoming, 0, ReadBytes);

            // Invalid client ID? Close this connection
            if (!int.TryParse(ID, out ClientID))
            {
                Stop();
            }
            Utils.Log($"Received ID from client ID={ClientID}");
            InvokeIDEvent();
        }

        private void ClientMessageHandling()
        {
            NetworkStream networkStream;
            try
            {
                networkStream = Connection.GetStream();
                networkStream.BeginRead(
                    Incoming,
                    0,
                    Constants.BufferSize,
                    this.ProcessClientID,
                    networkStream);

                // All writing should be done in this thread, since the overhead of starting a thread is large and the send operation
                // is blocking, we can just keep the thread alive and occasionally check for messages
                while (Worker.ThreadState != ThreadState.AbortRequested)
                {
                    while (Messages.Count > 0)
                    {
                        Payload Next = Messages.Dequeue();
                        Utils.Log($"Sending from ClientID={ClientID} message=${Next}");
                        byte[] ToSend = System.Text.Encoding.UTF8.GetBytes(Next.ToString());
                        networkStream.Write(ToSend, 0, ToSend.Length);
                    }
                    Thread.Sleep(Constants.WorkerDelay);
                }
            }
            catch
            {
                Utils.Log($"Client ID={ClientID}shutdown!");
            }
            Stop();
        }

        public override void Start()
        {
            Worker.Start();
            base.Start();
        }

        public override void Stop()
        {
            Worker.Abort();
            base.Stop();
        }
    }
}
