using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace FollowerMazeServer
{
    class EventListener: IDisposable
    {
        
        private BackgroundWorker EventListenerWorker = new BackgroundWorker();
        private BackgroundWorker EventHandlerWorker = new BackgroundWorker();
        private BackgroundWorker ClientWorker = new BackgroundWorker();

        // Contains unhandled messages to be sent later
        private Dictionary<int, Payload> Unhandled;
        
        // List of clients [client ID, client instance]
        private Dictionary<int, Client> Clients;

        // Clients connected but didn't sent their ID yet
        private List<Client> PendingClients;

        public EventListener()
        {
            Clients = new Dictionary<int, Client>();
            Unhandled = new Dictionary<int, Payload>();
            PendingClients = new List<Client>();

            EventListenerWorker.WorkerSupportsCancellation = true;
            EventListenerWorker.DoWork += EventListenerWorker_DoWork;            

            ClientWorker.WorkerSupportsCancellation = true;
            ClientWorker.DoWork += ClientWorker_DoWork;

            EventHandlerWorker.WorkerSupportsCancellation = true;
            EventHandlerWorker.DoWork += EventHandlerWorker_DoWork;
        }

        private void EventHandlerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!EventHandlerWorker.CancellationPending)
            {
                Payload[] UnhandledList;
                lock (Unhandled)
                {
                    UnhandledList = Unhandled.Values.ToArray();
                }
                foreach (var UnhandledPayload in UnhandledList)
                {
                    // TODO Restore after debug
                    // if (PayloadHandled(UnhandledPayload))
                    PayloadHandled(UnhandledPayload);
                    lock (Unhandled)
                    {
                        Unhandled.Remove(UnhandledPayload.ID);
                    }
                }
            }
        }

        // Handle a payload, returns true if it can be processed now, false otherwise
        private bool PayloadHandled(Payload P)
        {
            Utils.Log($"Sending event {P.ToString()}");
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
                    foreach (var Entry in Clients.Values)
                    {
                        Entry.QueueMessage(P);
                    }
                    return true;
                case PayloadType.Private:
                    if (Clients.ContainsKey(P.From) && Clients.ContainsKey(P.To))
                    {
                        Clients[P.To].QueueMessage(P);
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
                            Clients[C].QueueMessage(P);
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

        private void EventListenerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(Constants.IP, Constants.EventSourcePort);
            Listener.Start();
            Utils.Log($"Event source listener started: {Constants.IP.ToString()}:{Constants.EventSourcePort}");
            TcpClient Connection = Listener.AcceptTcpClient();
            Utils.Log("Event source connected");

            NetworkStream networkStream = Connection.GetStream();
            byte[] Buffer = new Byte[0];

            // Stop when event source disconnects
            while (Connection.Connected)
            {
                // Read new data
                byte[] Incoming = new byte[Constants.BufferSize];
                int ReadBytes;
                try
                {
                    ReadBytes = networkStream.Read(Incoming, 0, Constants.BufferSize);
                } catch
                {
                    break;
                }

                // Append the previous data to the new data
                int newLength = ReadBytes + Buffer.Length;
                byte[] NewBuffer = new byte[newLength];
                Array.Copy(Buffer, NewBuffer, Buffer.Length);
                Array.Copy(Incoming, 0, NewBuffer, Buffer.Length, ReadBytes);
                Buffer = NewBuffer;
                
                Buffer = ProcessBuffer(Buffer);
                Utils.Log("Remaining buffer");
                Utils.Log(Buffer);
            }
            Connection.Close();
            Stop();
        }

        // Tries to extract events and return the remaining buffer 
        private byte[] ProcessBuffer(byte[] RawBuffer)
        {
            string Buffer = Encoding.UTF8.GetString(RawBuffer);
            string UnhandledBuffer = "";
            string[] Events = Buffer.Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string EventData in Events)
            {
                Utils.Log($"Received event={EventData}");
                // Try to parse, the event may be partially received (invalid), this will skip it
                Payload P = Payload.Create(EventData);
                if (P == null)
                {
                    UnhandledBuffer += "\r\n" + EventData;
                    continue;
                }

                lock (Unhandled)
                {
                    Unhandled.Add(P.ID, P);
                }
            }
            return Encoding.UTF8.GetBytes(UnhandledBuffer);
        }
        
        private void ClientWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(Constants.IP, Constants.ClientConnectionPort);
            Listener.Start();
            Utils.Log($"Client listener started: {Constants.IP.ToString()}:{Constants.ClientConnectionPort}");
            while (!ClientWorker.CancellationPending)
            {
                TcpClient Connection = Listener.AcceptTcpClient();
                Client Instance = new Client(Connection);
                Utils.Log("Client connected");
                Instance.OnIDAvailable += Instance_IDAvailable;
                Instance.OnDisconnect += Instance_OnDisconnect;
                PendingClients.Add(Instance);
            }
        }

        private void Instance_IDAvailable(object sender, IDEventArgs e)
        {
            Client Instance = (Client)sender;
            lock (this)
            {
                Clients[e.ID] = Instance;
                PendingClients.Remove(Instance);
            }
        }

        private void Instance_OnDisconnect(object sender, IDEventArgs e)
        {
            Clients.Remove(e.ID);
        }

        // Implements dispose pattern
        public void Dispose()
        {
            EventListenerWorker.Dispose();
            ClientWorker.Dispose();
            EventHandlerWorker.Dispose();
        }

        public void Start()
        {
            EventListenerWorker.RunWorkerAsync();
            ClientWorker.RunWorkerAsync();
            EventHandlerWorker.RunWorkerAsync();
        }

        public void Stop()
        {
            // Copy clients list on shutdown to avoid concurrency problems
            foreach (var C in Clients.Values.ToList())
            {
                C.Shutdown();
            }

            foreach (var C in PendingClients)
            {
                C.Shutdown();
            }

            EventListenerWorker.CancelAsync();
            ClientWorker.CancelAsync();
            EventHandlerWorker.CancelAsync();
        }
    }
}
