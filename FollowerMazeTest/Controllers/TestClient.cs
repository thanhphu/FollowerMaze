using FollowerMazeServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FollowerMazeTest.Controllers
{
    /// <summary>
    /// Simulates real client, actually a reimplementation in C#
    /// </summary>
    sealed class TestClient: IDisposable
    {
        int ID;
        bool ShouldStop = false;
        Thread Worker;
        TcpClient Client = new TcpClient();
        public readonly List<string> Received = new List<string>();

        public TestClient(int ID)
        {
            this.ID = ID;
        }

        public void Start()
        {
            Client.Connect(IPAddress.Loopback, Constants.ClientConnectionPort);
            using (StreamWriter Writer = new StreamWriter(Client.GetStream(), Encoding.UTF8))
            {
                Writer.WriteLine(ID);
            }
            Worker = new Thread(new ThreadStart(async () => {
                using (StreamReader Reader = new StreamReader(Client.GetStream(), Encoding.UTF8))
                {
                    do
                    {
                        if (ShouldStop)
                        {
                            break;
                        } else
                        {
                            Thread.Sleep(Constants.WorkerDelay);
                        }
                        string Line = await Reader.ReadLineAsync();
                        Received.Add(Line);
                    } while (!ShouldStop);
                }
            }));
            Worker.Start();
        }

        public void Stop()
        {
            Dispose();
        }

        public void Dispose()
        {
            Client.Close();
        }
    }
}
