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
    /// <summary>
    /// Controller class, manage all listeners
    /// </summary>
    internal sealed class EventListener : IDisposable
    {
        #region Data

        private bool Started = false;

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

        #endregion Data

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
                while (Unhandled.ContainsKey(ProcessedCount))
                {
                    if (IsPayloadHandled(Unhandled[ProcessedCount]))
                    {
                        lock (Unhandled)
                            Unhandled.Remove(ProcessedCount);
                        ProcessedCount++;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a client already exists, if not, create a dummy client
        /// </summary>
        /// <param name="ID">ID of client to create</param>
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

        /// <summary>
        /// Handle a payload, returns true if it can be processed now, false otherwise
        /// </summary>
        /// <param name="P">payload to handle</param>
        /// <returns>true if payload has been handled, false if retry is needed</returns>
        private bool IsPayloadHandled(Payload P)
        {
            Logger.Log($"Sending event {P.ToString()}");
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

        #endregion EventDispatchWorker

        #region EventListenerWorker

        private void EventListenerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(Constants.IP, Constants.EventSourcePort);
            Listener.Start();

            while (!EventListenerWorker.CancellationPending)
            {
                TcpClient Connection = Listener.AcceptTcpClient();
                Logger.Log("Event source connected");

                using (StreamReader Reader = new StreamReader(Connection.GetStream(), Encoding.UTF8))
                {
                    int Peek = Reader.Peek();
                    while (Peek >= 0 || Connection.Connected)
                    {
                        string EventData = "";
                        try
                        {
                            EventData = Reader.ReadLine();
                            Peek = Reader.Peek();
                        }
                        catch (IOException E)
                        {
                            Logger.Log("Listener error! Message=" + E.Message);
                            continue;
                        }
                        if (!string.IsNullOrEmpty(EventData))
                        {
                            // Parse event data
                            Logger.Log($"Received event={EventData}");
                            Payload P = Payload.Create(EventData);
                            if (P == null) continue;

                            lock (Unhandled)
                            {
                                Unhandled[P.ID] = P;
                            }
                            if (EventListenerWorker.CancellationPending)
                                break;
                        }
                    }
                }
                Connection.Close();
            }
        }

        #endregion EventListenerWorker

        #region ClientHandlingWorker

        private void ClientHandlingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(Constants.IP, Constants.ClientConnectionPort);
            Listener.Start();
            while (!ClientHandlingWorker.CancellationPending)
            {
                TcpClient Connection = Listener.AcceptTcpClient();
                ConnectedClient Instance = new ConnectedClient(Connection);
                Logger.Log("Client connected");
                Instance.OnIDAvailable += Instance_IDAvailable;
                Instance.OnDisconnect += Instance_OnDisconnect;
                PendingClients.Add(Instance);

                Instance.Start();
            }
        }

        /// <summary>
        /// Called when a connected client sends it ID
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Called when a connected client disconnects
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Instance_OnDisconnect(object sender, IDEventArgs e)
        {
            lock (Clients)
            {
                Clients.Remove(e.ID);
            }
        }

        #endregion ClientHandlingWorker

        #region Statistics

        public int PendingClientsCount
        {
            get
            {
                return PendingClients.Count;
            }
        }

        public int ConnectedClientsCount
        {
            get
            {
                return Clients.Count;
            }
        }

        public int PendingMessagesCount
        {
            get
            {
                return Unhandled.Count;
            }
        }

        public int ProcessedMessagesCount
        {
            get
            {
                return ProcessedCount - 1;
            }
        }

        #endregion Statistics

        #region DisposePattern

        /// <summary>
        /// Implements dispose pattern for the worker objects
        /// </summary>
        public void Dispose()
        {
            EventListenerWorker.Dispose();
            ClientHandlingWorker.Dispose();
            EventDispatchWorker.Dispose();
        }

        #endregion DisposePattern

        #region Behavior

        public void Start()
        {
            if (!Started)
            {
                Logger.Log("Event listener starting...");
                EventListenerWorker.RunWorkerAsync();
                ClientHandlingWorker.RunWorkerAsync();
                EventDispatchWorker.RunWorkerAsync();
            }
            Started = true;
        }

        public void Stop()
        {
            if (Started)
            {
                Logger.Log("Event listener stopping...");
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

                Logger.FlushLog();
            }
        }

        #endregion Behavior
    }
}