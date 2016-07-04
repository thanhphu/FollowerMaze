using FollowerMazeServer;
using FollowerMazeServer.DataObjects;
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
        public int ID { get; private set; }
        bool ShouldStop = false;
        Thread Worker;
        TcpClient Client = new TcpClient();
        public event EventHandler<MessageEventArgs> OnMessage;

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
                        OnMessage?.Invoke(this, new MessageEventArgs(ID, Line));
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
