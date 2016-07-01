using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace FollowerMazeServer
{
    class EventListener: IDisposable
    {   
        private BackgroundWorker EventListenerWorker = new BackgroundWorker();
        private BackgroundWorker EventHandlerWorker = new BackgroundWorker();
        private BackgroundWorker ClientWorker = new BackgroundWorker();

        // Contains unhandled messages to be sent later
        private SortedList<int, Payload> Unhandled;
        
        // List of clients [client ID, client instance]
        private Dictionary<int, Client> Clients;

        // Clients connected but didn't sent their ID yet
        private List<Client> PendingClients;

        // Timer to wait before sending out messages
        Timer GraceTimer;

        // Number of messages processed, for statistics
        private int ProcessedCount = 0;        

        public EventListener()
        {
            Clients = new Dictionary<int, Client>();
            Unhandled = new SortedList<int, Payload>();
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
                    if (PayloadHandled(UnhandledPayload))
                    {
                        ProcessedCount++;
                        lock (Unhandled)
                        {
                            Unhandled.Remove(UnhandledPayload.ID);
                        }
                    }
                }
                UpdateStatus();
            }
            Utils.StatusLine("EventHandlerWorker stopped");
        }

        private bool IsAllClientsConnected()
        {
            return (Clients.Count > 0) && (PendingClients.Count == 0) && (GraceTimer.Enabled == false);
        }

        // Handle a payload, returns true if it can be processed now, false otherwise
        private bool PayloadHandled(Payload P)
        {
            Utils.Log($"Sending event {P.ToString()}");
            switch (P.Type)
            {
                case PayloadType.Follow:
                    // Adds a "dummy" client if it doesn't exist
                    if (!Clients.ContainsKey(P.To))
                    {
                        lock (Clients)
                        {
                            Clients[P.To] = new Client(P.To);
                        }
                    }                    
                    Clients[P.To].AddFollower(P.From);
                    Clients[P.To].QueueMessage(P);
                    return true;
                case PayloadType.Unfollow:
                    if (Clients.ContainsKey(P.To))
                    {
                        return Clients[P.To].RemoveFollower(P.From);
                    }
                    break;
                case PayloadType.Broadcast:
                    foreach (var Entry in Clients.Values)
                    {
                        Entry.QueueMessage(P);
                    }
                    return true;
                case PayloadType.Private:
                    if (Clients.ContainsKey(P.To))
                    {
                        Clients[P.To].QueueMessage(P);
                        return true;
                    }
                    break;
                case PayloadType.Status:
                    if (Clients.ContainsKey(P.From))
                    {
                        List<int> Followers = Clients[P.From].GetCurrentFollowers();
                        foreach (int C in Followers)
                        {
                            if (Clients.ContainsKey(C))
                                Clients[C].QueueMessage(P);
                        }
                        return true;
                    }
                    break;
            }

            // If all clients connected and the packet is invalid, dispose it
            // If packet has been retried too many times, dispose it
            bool ShouldRetry = !IsAllClientsConnected() && P.ShouldRetry();
            return true;
        }

        private void EventListenerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(Constants.IP, Constants.EventSourcePort);
            Listener.Start();
            Utils.StatusLine($"Event source listener started: {Constants.IP.ToString()}:{Constants.EventSourcePort}");
            TcpClient Connection = Listener.AcceptTcpClient();
            Utils.Log("Event source connected");

            // Start the timer
            GraceTimer.Enabled = true;

            NetworkStream networkStream = Connection.GetStream();
            byte[] Buffer = new Byte[0];

            // Stop when event source disconnects
            while (Connection.Connected && !EventListenerWorker.CancellationPending)
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
            Utils.StatusLine("Event source stopped");
            // Stop();
        }

        // Tries to extract events and return the remaining buffer 
        private byte[] ProcessBuffer(byte[] RawBuffer)
        {
            string Buffer = Encoding.UTF8.GetString(RawBuffer);
            string UnhandledBuffer = "";
            string[] Events = Buffer.Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string EventData in Events)
            {
                Utils.Log($"Received event={EventData}");
                // Try to parse, the event may be partially received (invalid), this will skip it
                Payload P = Payload.Create(EventData);
                if (P == null)
                {
                    UnhandledBuffer = "\r\n" + EventData;
                    continue;
                }

                if (!Unhandled.ContainsKey(P.ID))
                    lock (Unhandled)
                    {
                        Unhandled[P.ID] = P;
                    }
            }
            return Encoding.UTF8.GetBytes(UnhandledBuffer);
        }
        
        private void ClientWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(Constants.IP, Constants.ClientConnectionPort);
            Listener.Start();
            Utils.StatusLine($"Client listener started: {Constants.IP.ToString()}:{Constants.ClientConnectionPort}");
            while (!ClientWorker.CancellationPending)
            {
                TcpClient Connection = Listener.AcceptTcpClient();
                Client Instance = new Client(Connection);
                Utils.Log("Client connected");
                Instance.OnIDAvailable += Instance_IDAvailable;
                Instance.OnDisconnect += Instance_OnDisconnect;
                PendingClients.Add(Instance);

                Instance.Start();
                UpdateStatus();
            }
            Utils.StatusLine("ClientWorker stopped");
        }

        private void UpdateStatus()
        {
            if (ProcessedCount == 0)
                return;
            Utils.Status($"{DateTime.Now.ToLongTimeString()} Clients: Pending={PendingClients.Count} Connected={Clients.Count} " +
                    $"Messages: Pending={Unhandled.Count} Processed={ProcessedCount}  ");
        }

        private void Instance_IDAvailable(object sender, IDEventArgs e)
        {
            Client Instance = (Client)sender;
            lock (Clients)
            {
                Clients[e.ID] = Instance;
                PendingClients.Remove(Instance);
            }
        }

        private void Instance_OnDisconnect(object sender, IDEventArgs e)
        {
            lock (Clients)
            {
                Clients.Remove(e.ID);
            }
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
            Utils.Log("Event listener starting...");
            EventListenerWorker.RunWorkerAsync();
            ClientWorker.RunWorkerAsync();

            GraceTimer = new Timer();
            GraceTimer.Interval = Constants.GracePeriod;
            GraceTimer.Elapsed += GraceTimer_Elapsed;

            UpdateStatus();
        }

        private void GraceTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            GraceTimer.Enabled = false;
            EventHandlerWorker.RunWorkerAsync();
        }

        public void Stop()
        {
            Utils.Log("Event listener stopping...");
            EventListenerWorker.CancelAsync();
            ClientWorker.CancelAsync();
            EventHandlerWorker.CancelAsync();

            // Copy clients list on shutdown to avoid concurrency problems
            foreach (var C in Clients.Values.ToList())
            {
                C.Stop();
            }

            foreach (var C in PendingClients)
            {
                C.Stop();
            }
        }
    }
}
