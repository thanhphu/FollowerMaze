using FollowerMazeServer.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace FollowerMazeServer
{
    class EventListener : IDisposable
    {
        private BackgroundWorker EventListenerWorker = new BackgroundWorker();
        private BackgroundWorker EventHandlerWorker = new BackgroundWorker();
        private BackgroundWorker ClientWorker = new BackgroundWorker();

        // Contains unhandled messages to be sent later
        private SortedList<int, Payload> Unhandled;

        // List of clients [client ID, client instance]
        private Dictionary<int, AbstractClient> Clients;

        // Clients connected but didn't sent their ID yet
        private List<ConnectedClient> PendingClients;

        // Index of current message
        private int ProcessedCount = 1;

        public EventListener()
        {
            Clients = new Dictionary<int, AbstractClient>();
            Unhandled = new SortedList<int, Payload>();
            PendingClients = new List<ConnectedClient>();

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
                var UnhandledList = Unhandled.Values;
                for (int i = 0; i < UnhandledList.Count; i++)
                {
                    var Next = UnhandledList[i];
                    if (Next.ID == ProcessedCount && PayloadHandled(Next))
                    {
                        ProcessedCount++;
                        continue;
                    }
                    // Wait for the correct message to come or the client to connect
                    break;
                }
                lock (Unhandled)
                {
                    while (Unhandled.Count > 0 && Unhandled.Values[0].ID < ProcessedCount)
                        Unhandled.RemoveAt(0);
                }
                UpdateStatus();
            }
            Utils.StatusLine("EventHandlerWorker stopped");
        }

        private bool CheckAndCreateDummyClient(int ID)
        {
            // Adds a "dummy" client if it doesn't exist
            if (!Clients.ContainsKey(ID))
            {
                lock (Clients)
                {
                    Clients[ID] = new DummyClient(ID);
                }
            }
            return true;
        }

        // Handle a payload, returns true if it can be processed now, false otherwise
        private bool PayloadHandled(Payload P)
        {
            Utils.Log($"Sending event {P.ToString()}");
            switch (P.Type)
            {
                case PayloadType.Follow:
                    if (!CheckAndCreateDummyClient(P.To))
                        return false;
                    Clients[P.To].AddFollower(P.From);
                    Clients[P.To].QueueMessage(P);
                    return true;
                case PayloadType.Unfollow:
                    if (Clients.ContainsKey(P.To))
                    {
                        Clients[P.To].RemoveFollower(P.From);
                    }
                    return true;
                case PayloadType.Broadcast:
                    var Copy = Clients.Values.ToList();
                    foreach (var Entry in Copy)
                    {
                        Entry.QueueMessage(P);
                    }
                    return true;
                case PayloadType.Private:
                    if (!CheckAndCreateDummyClient(P.To))
                        return false;
                    Clients[P.To].QueueMessage(P);
                    return true;
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
            return true;
        }

        private void EventListenerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(Constants.IP, Constants.EventSourcePort);
            Listener.Start();

            while (!EventListenerWorker.CancellationPending)
            {
                Utils.StatusLine($"Event source listener started: {Constants.IP.ToString()}:{Constants.EventSourcePort}");
                TcpClient Connection = Listener.AcceptTcpClient();
                Utils.Log("Event source connected");

                NetworkStream networkStream = Connection.GetStream();
                byte[] Buffer = new Byte[0];

                while (Connection.Connected)
                {
                    // Read new data
                    byte[] Incoming = new byte[Constants.BufferSize];
                    int ReadBytes;
                    try
                    {
                        ReadBytes = networkStream.Read(Incoming, 0, Constants.BufferSize);
                    }
                    catch
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
                Utils.StatusLine("Event source disconnected");
            }
            Utils.StatusLine("Event source worker terminated");
        }

        // Tries to extract events and return the remaining buffer 
        private byte[] ProcessBuffer(byte[] RawBuffer)
        {
            string Buffer = Encoding.UTF8.GetString(RawBuffer);
            while (Buffer.Contains("\n"))
            {
                int Index = Buffer.IndexOf("\n");
                // Trim() make it works with both \r\n and \n
                string EventData = Buffer.Substring(0, Index).Trim();
                Buffer = Buffer.Substring(Index + 1);

                Payload P = Payload.Create(EventData);
                if (P == null) continue;

                lock (Unhandled)
                {
                    Unhandled[P.ID] = P;
                }
            }
            return Encoding.UTF8.GetBytes(Buffer);
        }

        private void ClientWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(Constants.IP, Constants.ClientConnectionPort);
            Listener.Start();
            Utils.StatusLine($"Client listener started: {Constants.IP.ToString()}:{Constants.ClientConnectionPort}");
            while (!ClientWorker.CancellationPending)
            {
                TcpClient Connection = Listener.AcceptTcpClient();
                ConnectedClient Instance = new ConnectedClient(Connection);
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
            Utils.Status($"Clients: Pending={PendingClients.Count} Connected={Clients.Count} " +
                    $"Messages: Pending={Unhandled.Count} Processed={ProcessedCount - 1}");
        }

        private void Instance_IDAvailable(object sender, IDEventArgs e)
        {
            ConnectedClient Instance = (ConnectedClient)sender;
            if (Clients.ContainsKey(e.ID))
            {
                var DC = (DummyClient)Clients[e.ID];
                Instance.TakeOverFrom(DC);
            }
            lock (Clients)
            {
                Clients[e.ID] = Instance;
            }
            PendingClients.Remove(Instance);
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
