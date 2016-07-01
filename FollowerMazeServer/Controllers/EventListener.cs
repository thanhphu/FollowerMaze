using FollowerMazeServer.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace FollowerMazeServer
{
    class EventListener : IDisposable
    {
        #region Data
        // Listens for events from event source
        private BackgroundWorker EventListenerWorker = new BackgroundWorker();

        // Process and send events to client
        private BackgroundWorker EventDispatchWorker = new BackgroundWorker();

        // Handle connections from client
        private BackgroundWorker ClientHandlingWorker = new BackgroundWorker();

        // Contains unhandled messages to be sent later
        private Dictionary<int, Payload> Unhandled = new Dictionary<int, Payload>();

        // List of clients [client ID, client instance]
        private Dictionary<int, AbstractClient> Clients = new Dictionary<int, AbstractClient>();

        // Clients connected but didn't sent their ID yet
        private List<ConnectedClient> PendingClients = new List<ConnectedClient>();

        // ID of the next message
        private int ProcessedCount = 1;
        #endregion

        public EventListener()
        {
            EventListenerWorker.WorkerSupportsCancellation = true;
            EventListenerWorker.DoWork += EventListenerWorker_DoWork;

            ClientHandlingWorker.WorkerSupportsCancellation = true;
            ClientHandlingWorker.DoWork += ClientHandlingWorker_DoWork;

            EventDispatchWorker.WorkerSupportsCancellation = true;
            EventDispatchWorker.DoWork += EventDispatchWorker_DoWork;
        }

        #region EventDispatchWorker
        private void EventDispatchWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!EventDispatchWorker.CancellationPending)
            {
                int Start = ProcessedCount;
                while (Unhandled.ContainsKey(ProcessedCount))
                {
                    if (PayloadHandled(Unhandled[ProcessedCount]))
                    {
                        ProcessedCount++;
                    }
                    // Empty the dictionary once in a while
                    if (ProcessedCount - Start > Constants.ProcessedEventLimit)
                        break;
                }
                lock (Unhandled)
                {
                    for (int i = Start; i < ProcessedCount; i++)
                        Unhandled.Remove(i);
                }
                UpdateStatus();
            }
            Utils.StatusLine("EventHandlerWorker stopped");
        }

        private void CheckAndCreateDummyClient(int ID)
        {
            // Adds a "dummy" client if it doesn't exist
            if (!Clients.ContainsKey(ID))
            {
                lock (Clients)
                {
                    Clients[ID] = new DummyClient(ID);
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
                    CheckAndCreateDummyClient(P.To);
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
                    CheckAndCreateDummyClient(P.To);
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
        #endregion

        #region EventListenerWorker
        private void EventListenerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(Constants.IP, Constants.EventSourcePort);
            Listener.Start();

            while (!EventListenerWorker.CancellationPending)
            {
                Utils.StatusLine($"Event source listener started: {Constants.IP.ToString()}:{Constants.EventSourcePort}");
                TcpClient Connection = Listener.AcceptTcpClient();
                Utils.Log("Event source connected");

                // System.Threading.Thread.Sleep(1000);
                using (StreamReader Reader = new StreamReader(Connection.GetStream(), Encoding.UTF8))
                {
                    List<Payload> ToAdd = new List<Payload>();
                    while (Reader.Peek() >= 0 || Connection.Connected)
                    {
                        string EventData = "";
                        try
                        {
                            EventData = Reader.ReadLine();
                        }
                        catch (Exception E)
                        {
                            Utils.StatusLine($"Exception {E.Message}");
                            // Ignore IO errors
                        }
                        Utils.Log($"Received event={EventData}");
                        Payload P = Payload.Create(EventData);
                        if (P == null) continue;
                        ToAdd.Add(P);
                        if (ToAdd.Count > Constants.MessageHoldingLimit)
                        {
                            AddToUnhandled(ToAdd);
                        }
                    }
                }
                Connection.Close();
                Utils.StatusLine("Event source disconnected");
            }
            Utils.StatusLine("Event source worker terminated");
        }

        private void AddToUnhandled(List<Payload> ToAdd)
        {
            lock (Unhandled)
            {
                foreach (Payload iP in ToAdd)
                    Unhandled[iP.ID] = iP;
            }
            ToAdd.Clear();
        }
        #endregion

        #region ClientHandlingWorker
        private void ClientHandlingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(Constants.IP, Constants.ClientConnectionPort);
            Listener.Start();
            Utils.StatusLine($"Client listener started: {Constants.IP.ToString()}:{Constants.ClientConnectionPort}");
            while (!ClientHandlingWorker.CancellationPending)
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

        private void Instance_IDAvailable(object sender, IDEventArgs e)
        {
            ConnectedClient Instance = (ConnectedClient)sender;
            if (Clients.ContainsKey(e.ID))
            {
                Instance.TakeOverFrom(Clients[e.ID]);
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
        #endregion

        #region Pattern
        private void UpdateStatus()
        {
            if (ProcessedCount == 0)
                return;
            Utils.Status($"Clients: Pending={PendingClients.Count} Connected={Clients.Count} " +
                    $"Messages: Pending={Unhandled.Count} Processed={ProcessedCount - 1}");
        }

        // Implements dispose pattern
        public void Dispose()
        {
            EventListenerWorker.Dispose();
            ClientHandlingWorker.Dispose();
            EventDispatchWorker.Dispose();
        }
        #endregion

        #region Behavior
        public void Start()
        {
            Utils.Log("Event listener starting...");
            EventListenerWorker.RunWorkerAsync();
            ClientHandlingWorker.RunWorkerAsync();
            EventDispatchWorker.RunWorkerAsync();
        }

        public void Stop()
        {
            Utils.Log("Event listener stopping...");
            EventListenerWorker.CancelAsync();
            ClientHandlingWorker.CancelAsync();
            EventDispatchWorker.CancelAsync();

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
        #endregion
    }
}
