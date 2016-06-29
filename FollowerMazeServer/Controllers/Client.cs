using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FollowerMazeServer
{
    class Client: IDisposable
    {
        private int ClientID;
        private List<int> Followers;
        public ConcurrentQueue<Payload> Messages { get; private set; }        

        BackgroundWorker Worker;
        TcpClient Connection;

        // Triggered when the client sends its ID
        public event EventHandler<IDEventArgs> OnIDAvailable;

        // Triggered when the client disconnects
        public event EventHandler<IDEventArgs> OnDisconnect;

        public Client(TcpClient Connection)
        {
            Messages = new ConcurrentQueue<Payload>();
            Followers = new List<int>();

            this.Connection = Connection;            
            this.Worker = new BackgroundWorker();
            this.Worker.DoWork += ClientMessageHandling;
            this.Worker.RunWorkerAsync();
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

        public List<int> GetCurrentFollowers()
        {
            // Returns a copy
            return Followers.ToList();
        }


        private void ClientMessageHandling(object sender, DoWorkEventArgs e)
        {
            NetworkStream networkStream = Connection.GetStream();
            while (Connection.Connected && !Worker.CancellationPending)
            {
                // Read client ID
                byte[] Incoming = new byte[Constants.BufferSize];
                int ReadBytes = networkStream.Read(Incoming, 0, Constants.BufferSize);
                string ID = System.Text.Encoding.UTF8.GetString(Incoming, 0, ReadBytes);

                // Invalid client ID? Close this connection
                if (!int.TryParse(ID, out ClientID))
                {
                    break;
                }

                OnIDAvailable?.Invoke(this, new IDEventArgs(ClientID));

                while (Messages.Count > 0)
                {
                    Payload Next;
                    if (Messages.TryDequeue(out Next))
                    {
                        byte[] ToSend = System.Text.Encoding.UTF8.GetBytes(Next.ToString());
                        networkStream.Write(ToSend, 0, ToSend.Length);
                    }
                }

                Thread.Sleep(Constants.WorkerDelay);
            }
            // Process the remaining buffer before quitting
            Shutdown();
        }

        public void Shutdown()
        {
            if (Connection.Connected)
                Connection.Close();
            Worker.CancelAsync();
            // Event handler not set in this class, better check if it is properly assigned
            OnDisconnect?.Invoke(this, new IDEventArgs(ClientID));
        }

        void IDisposable.Dispose()
        {
            Worker.Dispose();
        }
    }
}
