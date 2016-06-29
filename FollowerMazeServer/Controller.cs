using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FollowerMazeServer
{
    class Controller
    {
        private IPAddress IP = IPAddress.Any;
        private int EventSourcePort = 9090;
        private int ClientConnectionPort = 9099;
        private BackgroundWorker EventSourceWorker = new BackgroundWorker();
        private BackgroundWorker ClientWorker = new BackgroundWorker();
        // Triggered when a new event is received
        public event EventHandler<ServerEventArgs> OnEventAvailable;
        
        // List of clients [client ID, client instance]
        private Dictionary<int, Client> Clients;

        public Controller()
        {
            Clients = new Dictionary<int, Client>();

            EventSourceWorker.WorkerSupportsCancellation = true;
            EventSourceWorker.DoWork += EventSourceHandling;
            EventSourceWorker.RunWorkerAsync();
            Debug.WriteLine($"Event source listener started: {IP.ToString()}:{EventSourcePort}");

            ClientWorker.WorkerSupportsCancellation = true;
            ClientWorker.DoWork += ClientConnectionHandling;
            ClientWorker.RunWorkerAsync();
            Debug.WriteLine($"Client listener started: {IP.ToString()}:{ClientConnectionPort}");

            OnEventAvailable += EventHandling;
        }

        private void EventHandling(object sender, ServerEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void EventSourceHandling(object sender, DoWorkEventArgs e)
        {
            const int BufferSize = Constants.BufferSize;
            TcpListener Listener = new TcpListener(IP, EventSourcePort);
            TcpClient Connection = Listener.AcceptTcpClient();

            NetworkStream networkStream = Connection.GetStream();
            byte[] Buffer = new Byte[0];

            while (Connection.Connected)
            {
                // Read new data
                byte[] Incoming = new byte[BufferSize];
                int ReadBytes = networkStream.Read(Incoming, 0, BufferSize);

                // Append the previous data to the new data
                {
                    int newLength = ReadBytes + Buffer.Length;
                    byte[] NewBuffer = new byte[newLength];
                    Array.Copy(Buffer, NewBuffer, Buffer.Length);
                    Array.Copy(Incoming, 0, NewBuffer, Buffer.Length, ReadBytes);
                    Buffer = NewBuffer;
                }

                // While there's a message to process
                // Finds the new line in appended data
                for (
                    int? Position = Utils.FindNewLine(Buffer);
                    Position.HasValue;
                    Position = Utils.FindNewLine(Buffer))
                {
                    // Extracts the command
                    string Command = System.Text.Encoding.UTF8.GetString(Buffer, 0, Position.Value);
                    OnEventAvailable(this, new ServerEventArgs(Command));

                    // Trim the processed command from the buffer
                    byte[] NewBuffer = new byte[Position.Value];
                    Array.Copy(Buffer, 0, NewBuffer, 0, Buffer.Length - Position.Value);
                    Buffer = NewBuffer;
                }
            }
            // TODO Process the remaining buffer before quitting
            this.EventSourceWorker.CancelAsync();
        }
        
        private void ClientConnectionHandling(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(IP, ClientConnectionPort);
            while (!EventSourceWorker.CancellationPending)
            {
                TcpClient Connection = Listener.AcceptTcpClient();
                Client Instance = new Client(Connection);
                Instance.OnIDAvailable += Instance_IDAvailable;
            }
        }

        public void Stop()
        {
            EventSourceWorker.CancelAsync();
            ClientWorker.CancelAsync();
        }

        private void Instance_IDAvailable(object sender, IDEventArgs e)
        {
            Client Instance = (Client)sender;
            lock (this)
            {
                Clients[e.ID] = Instance;
            }
        }
    }
}
