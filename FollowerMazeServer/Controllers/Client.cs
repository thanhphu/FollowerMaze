using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace FollowerMazeServer
{
    class Client
    {
        private int ClientID;
        private List<int> Followers = new List<int>();
        private Queue<Payload> Messages = new Queue<Payload>();

        Thread Worker = null;
        TcpClient Connection = null;

        // Triggered when the client sends its ID
        public event EventHandler<IDEventArgs> OnIDAvailable;

        // Triggered when the client disconnects
        public event EventHandler<IDEventArgs> OnDisconnect;

        // TODO refactor this and HandOverTo
        public Client(TcpClient Connection)
        {
            Messages = new Queue<Payload>();
            Connection.ReceiveTimeout = -1;
            Connection.SendTimeout = -1;            
            this.Connection = Connection;
            Worker = new Thread(new ThreadStart(ClientMessageHandling));
        }

        // "Dummy" client, doesn't connect, only have a list of followers
        public Client(int ID)
        {
            this.ClientID = ID;
            Worker = new Thread(new ThreadStart(ClientMessageHandling));
            // TODO Switch from dummy to real client? Better do it with inheritance!
        }

        public void Start()
        {            
            Worker.Start();
        }

        public void HandOverTo(Client Other)
        {
            Other.Followers = new List<int>(this.Followers);
            Other.Messages = new Queue<Payload>(this.Messages);
        }

        public void AddFollower(int Target)
        {
            lock (Followers)
            {
                Followers.Add(Target);
            }
        }

        public bool RemoveFollower(int Target)
        {
            bool Result = false;
            lock (Followers)
            {
                Result = Followers.Remove(Target);
            }
            return Result;
        }

        public void QueueMessage(Payload Message)
        {
            if (Messages.Count > 100 && Connection == null)
                Messages.Clear();
            lock (Messages)
            {
                Messages.Enqueue(Message);
            }
        }

        public List<int> GetCurrentFollowers()
        {
            // Returns a copy
            return Followers.ToList();
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
            OnIDAvailable?.Invoke(this, new IDEventArgs(ClientID));
        }

        private void ClientMessageHandling()
        {
            NetworkStream networkStream;
            // Dummy client
            if (Connection == null)
            {
                OnIDAvailable?.Invoke(this, new IDEventArgs(ClientID));
                return;
            }
            
            try
            {
                networkStream = Connection.GetStream();
                networkStream.BeginRead(
                    Incoming,
                    0,
                    Constants.BufferSize,
                    this.ProcessClientID,
                    networkStream);

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
            } catch {
                Utils.Log("Client shutdown!");                
            }
            Stop();
        }

        public void Stop()
        {
            Worker.Abort();
            // Event handler not set in this class, better check if it is properly assigned
            OnDisconnect?.Invoke(this, new IDEventArgs(ClientID));
        }
    }
}
