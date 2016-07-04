using FollowerMazeServer;
using FollowerMazeServer.Controllers;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FollowerMazeTest.Controllers
{
    [TestFixture]
    public class ConnectedClientTest
    {
        const int TestPort = 4567;
        TcpListener Server = new TcpListener(IPAddress.Loopback, TestPort);
        // Connection on client side
        TcpClient Client = new TcpClient();
        // Connection on server side
        TcpClient ClientConnection;

        [TestFixtureSetUp]
        public void Init()
        {
            // Prepare server
            Server.Start();
            new Thread(new ThreadStart(() =>
            {
                ClientConnection = Server.AcceptTcpClient();
            })).Start();

            // Prepare client
            Thread.Sleep(100);
            Client.Connect(IPAddress.Loopback, TestPort);
        }

        [TestFixtureTearDown]
        public void Dispose()
        {
            Client.Close();
            Server.Stop();
        }

        [Test]
        public void TestConnectedClientCreation()
        {
            ConnectedClient CC = new ConnectedClient(ClientConnection);
            Assert.That(CC.GetMessages().Count == 0, "ConnectedClient's constructor error!");
        }

        [Test]
        public void TestConnectedClientFollowers()
        {
            DummyClientTest.TestFollowers(new ConnectedClient(ClientConnection));
        }

        [Test]
        public void TestConnectedClientMessages()
        {
            DummyClientTest.TestMessages(new ConnectedClient(ClientConnection));
        }
    }
}
