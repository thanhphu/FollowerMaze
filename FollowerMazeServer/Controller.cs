﻿using System;
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
        private int InPort = 9090;
        private int OutPort = 9099;
        private BackgroundWorker Worker = new BackgroundWorker();

        public Controller()
        {
            Worker.WorkerSupportsCancellation = true;
            Worker.DoWork += ListenWorker;
            Worker.RunWorkerAsync();
            Debug.WriteLine($"Server started: {IP.ToString()}:{InPort}");
        }

        public void Stop()
        {
            Worker.CancelAsync();
        }

        private void ListenWorker(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = new TcpListener(IP, InPort);
            while (!Worker.CancellationPending)
            {
                Clients.Add(new Client(Listener.AcceptTcpClient()));
            }
        }

        private ArrayList Clients;
    }
}
