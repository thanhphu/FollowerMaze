using FollowerMazeServer;
using FollowerMazeServer.DataObjects;
using System;
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
        bool Started = false;
        Thread Worker;
        TcpClient Client = new TcpClient();
        public event EventHandler<MessageEventArgs> OnMessage;
        public event EventHandler<IDEventArgs> OnConnect;

        public TestClient(int ID)
        {
            this.ID = ID;
        }

        public void Start()
        {
            // Only start once
            if (Started) return;
            Started = true;
            Client.Connect(IPAddress.Loopback, Constants.ClientConnectionPort);
            OnConnect?.Invoke(this, new IDEventArgs(ID));

            byte[] IDData = Encoding.UTF8.GetBytes(ID.ToString() + Environment.NewLine);
            Client.GetStream().Write(IDData, 0, IDData.Length);
            
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
            if (Started)
            {
                Client.Close();
            }
        }
    }
}
