using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FollowerMazeServer
{
    class Client
    {
        public Queue<string> Messages { get; private set; }

        BackgroundWorker Worker;
        TcpClient Connection;

        // Triggered when the client sends its ID
        public event EventHandler<IDEventArgs> OnIDAvailable;

        // Triggered when the client disconnects
        public event EventHandler OnDisconnect;

        public Client(TcpClient _Connection)
        {
            Messages = new Queue<string>();

            this.Connection = _Connection;            
            this.Worker = new BackgroundWorker();
            this.Worker.DoWork += ClientMessageHandling;
            this.Worker.RunWorkerAsync();
        }
        
        private void ClientMessageHandling(object sender, DoWorkEventArgs e)
        {
            const int BufferSize = Constants.BufferSize;
            NetworkStream networkStream = Connection.GetStream();
            byte[] Buffer = new Byte[0];

            while (this.Connection.Connected && !Worker.CancellationPending)
            {
                // Read client ID
                byte[] Incoming = new byte[BufferSize];
                int ReadBytes = networkStream.Read(Incoming, 0, BufferSize);
                string ID = System.Text.Encoding.UTF8.GetString(Buffer, 0, ReadBytes);
                int ClientID;

                // Invalid client ID? Close this connection
                if (!int.TryParse(ID, out ClientID))
                    Shutdown();

                OnIDAvailable?.Invoke(this, new IDEventArgs(ClientID));

                while (Messages.Count > 0)
                {
                    string Next = Messages.Dequeue() + "\r\n";
                    byte[] ToSend = System.Text.Encoding.UTF8.GetBytes(Next);
                    networkStream.Write(ToSend, 0, ToSend.Length);
                }

                Thread.Sleep(Constants.WorkerDelay);
            }
            // Process the remaining buffer before quitting
            Shutdown();
        }

        public void Shutdown()
        {
            Worker.CancelAsync();
            OnDisconnect?.Invoke(this, null);
        }
    }
}
