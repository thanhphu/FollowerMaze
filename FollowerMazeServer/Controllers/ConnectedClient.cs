using FollowerMazeServer.Controllers;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace FollowerMazeServer
{
    /// <summary>
    /// Repersents a client that actually connect to the server, as opposed to a dummy client
    /// </summary>
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

        /// <summary>
        /// Take data from another client, the other client should be discarded shortly after so this
        /// instance can take oker
        /// </summary>
        /// <param name="Other">Client to take followers and messages from</param>
        public void TakeOverFrom(AbstractClient Other)
        {
            this.Followers = new List<int>(Other.GetCurrentFollowers());
            this.Messages = new Queue<Payload>(Other.GetMessages());
        }

        // Buffer to read client ID, shared between ProcessClientID and ClientMessageHandling
        byte[] Incoming = new byte[Constants.BufferSize];

        /// <summary>
        /// Asynchronous callback method called when a client return its ID
        /// </summary>
        /// <param name="AR"></param>
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
            Logger.Log($"Received ID from client ID={ClientID}");
            InvokeIDEvent();
        }

        /// <summary>
        /// Handle messages to and from clients, started in a thread and keep looping until shutdown
        /// or client disconnects
        /// </summary>
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
                        Logger.Log($"Sending from ClientID={ClientID} message=${Next}");
                        byte[] ToSend = System.Text.Encoding.UTF8.GetBytes(Next.ToString());
                        networkStream.Write(ToSend, 0, ToSend.Length);
                    }
                    Thread.Sleep(Constants.WorkerDelay);
                }
            }
            catch (System.IO.IOException E)
            {
                Logger.Log($"Client ID={ClientID} shutdown! Message={E.Message}");
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
            try
            {
                // Close stream to flush pending data, if any. Listener doesn't need to be closed like this because it's not sending out data
                Connection.GetStream().Close();
                Connection.Close();
            } catch (Exception E)
            {
                Logger.Log("Error closing connection: " + E.Message);
            }
            base.Stop();
        }
    }
}
