using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace FollowerMazeServer
{
    class EventListener: IDisposable
    {
        
        private BackgroundWorker EventSourceWorker = new BackgroundWorker();
        private BackgroundWorker ClientWorker = new BackgroundWorker();

        // Contains unhandled messages to be sent later
        private List<Payload> Unhandled;

        // Triggered when a new event is received
        private event EventHandler<ServerEventArgs> OnEventAvailable;
        
        // List of clients [client ID, client instance]
        private Dictionary<int, Client> Clients;

        public EventListener()
        {
            Clients = new Dictionary<int, Client>();
            Unhandled =  new List<Payload>();

            EventSourceWorker.WorkerSupportsCancellation = true;
            EventSourceWorker.DoWork += EventSourceHandling;            

            ClientWorker.WorkerSupportsCancellation = true;
            ClientWorker.DoWork += ClientConnectionHandling;

            OnEventAvailable += EventHandling;
        }

        // Handler called when event arrives
        private void EventHandling(object sender, ServerEventArgs e)
        {
            Payload P = Payload.Create(e.ServerEvent);
            if (P == null) return;

            lock (Unhandled)
            {
                Unhandled.Add(P);
            }

            foreach (var UnhandledPayload in Unhandled.ToList())
            {
                if (PayloadHandling(UnhandledPayload))
                    Unhandled.Remove(UnhandledPayload);
            }
        }

        // Handle a payload, returns true if it can be processed now, false otherwise
        private bool PayloadHandling(Payload P)
        {
            switch (P.Type)
            {
                case PayloadType.Follow:
                    if (Clients.ContainsKey(P.From) && Clients.ContainsKey(P.To))
                    {
                        Clients[P.From].AddFollower(P.To);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case PayloadType.Unfollow:
                    if (Clients.ContainsKey(P.From) && Clients.ContainsKey(P.To))
                    {
                        return Clients[P.From].RemoveFollower(P.To);
                    }
                    else
                    {
                        return false;
                    }
                case PayloadType.Broadcast:
                    foreach (var Entry in Clients)
                    {
                        Entry.Value.Messages.Enqueue(P);
                    }
                    return true;
                case PayloadType.Private:
                    if (Clients.ContainsKey(P.From) && Clients.ContainsKey(P.To))
                    {
                        Clients[P.To].Messages.Enqueue(P);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case PayloadType.Status:
                    if (Clients.ContainsKey(P.From))
                    {
                        foreach (int C in Clients[P.From].GetCurrentFollowers())
                        {
                            Clients[C].Messages.Enqueue(P);
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
            }
            return false;
        }

        private void EventSourceHandling(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(Constants.IP, Constants.EventSourcePort);
            Debug.WriteLine($"Event source listener started: {Constants.IP.ToString()}:{Constants.EventSourcePort}");
            TcpClient Connection = Listener.AcceptTcpClient();

            NetworkStream networkStream = Connection.GetStream();
            byte[] Buffer = new Byte[0];

            // Stop when event source disconnects
            while (Connection.Connected)
            {
                // Read new data
                byte[] Incoming = new byte[Constants.BufferSize];
                int ReadBytes = networkStream.Read(Incoming, 0, Constants.BufferSize);

                // Append the previous data to the new data
                {
                    int newLength = ReadBytes + Buffer.Length;
                    byte[] NewBuffer = new byte[newLength];
                    Array.Copy(Buffer, NewBuffer, Buffer.Length);
                    Array.Copy(Incoming, 0, NewBuffer, Buffer.Length, ReadBytes);
                    Buffer = NewBuffer;
                }
                Buffer = ProcessBuffer(Buffer);                
            }
            // Process the remaining buffer before quitting
            Buffer = ProcessBuffer(Buffer);

            this.EventSourceWorker.CancelAsync();
        }

        // Tries to extract events and return the remaining buffer 
        private byte[] ProcessBuffer(byte[] Buffer)
        {
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

                Debug.WriteLine("Remaining buffer");
                Debug.WriteLine(Encoding.UTF8.GetString(Buffer));
            }
            return Buffer;
        }
        
        private void ClientConnectionHandling(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(Constants.IP, Constants.ClientConnectionPort);
            Debug.WriteLine($"Client listener started: {Constants.IP.ToString()}:{Constants.ClientConnectionPort}");
            while (!EventSourceWorker.CancellationPending)
            {
                TcpClient Connection = Listener.AcceptTcpClient();
                Client Instance = new Client(Connection);
                Instance.OnIDAvailable += Instance_IDAvailable;
            }
        }

        public void Start()
        {
            EventSourceWorker.RunWorkerAsync();
            ClientWorker.RunWorkerAsync();            
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

        // Implements dispose pattern
        public void Dispose()
        {
            ((IDisposable)EventSourceWorker).Dispose();
            ((IDisposable)ClientWorker).Dispose();
        }
    }
}
