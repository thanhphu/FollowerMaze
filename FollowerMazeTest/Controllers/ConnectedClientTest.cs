using FollowerMazeServer;
using FollowerMazeServer.Controllers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        TcpClient ClientConnection = new TcpClient();
        // Connection on server side
        TcpClient ServerConnection;
        ConnectedClient ClientInstance = null;

        [TestFixtureSetUp]
        public void Init()
        {
            // Prepare server
            Server.Start();
            new Thread(new ThreadStart(() =>
            {
                ServerConnection = Server.AcceptTcpClient();
                ClientInstance = new ConnectedClient(ServerConnection);
            })).Start();

            // Prepare client
            Thread.Sleep(100);
            ClientConnection.Connect(IPAddress.Loopback, TestPort);

            Thread.Sleep(100);
            if (ClientInstance == null)
                Assert.Fail("Cannot create client for test");
            ClientInstance.Start();
        }

        [TestFixtureTearDown]
        public void Dispose()
        {
            ClientInstance.Stop();
            ClientConnection.Close();
            Server.Stop();
        }

        [Test]
        public void ConnectedClientCreation()
        {
            
            Assert.That(ClientInstance.GetMessages().Count == 0, "ConnectedClient's constructor error!");
        }

        [Test]
        public void ConnectedClientFollowers()
        {
            DummyClientTest.TestFollowers(new ConnectedClient(ServerConnection));
        }

        [Test]
        public void ConnectedClientMessages()
        {
            DummyClientTest.TestMessages(new ConnectedClient(ServerConnection));
        }

        [Test]
        public void ConnectedFollowerConcurrency()
        {
            DummyClientTest.TestFollowerConcurrency(new ConnectedClient(ServerConnection));
        }

        [Test]
        public void ConnectedMessageConcurrency()
        {
            DummyClientTest.TestMessageConcurrency(new ConnectedClient(ServerConnection));
        }

        [Test]
        // Test message ordering for client, also test ID receiving
        // StreamReader automatically closes the connection, which affect other tests,
        // So this test should be run last, hence the "Z" in the name
        public void ConnectedZClientMessageOrdering()
        {
            const int Iterations = 10000;
            var T = new Thread(new ThreadStart(() =>
            {
                int i = 0;
                using (StreamReader Reader = new StreamReader(ServerConnection.GetStream(), Encoding.UTF8))
                {
                    while (i < Iterations)
                    {
                        var Line = Reader.ReadLine();
                        var P = Payload.Create(Line);
                        Assert.AreEqual(P.ID, i, "Messages did not get sent in order");
                        i++;
                    }
                }
            }));
            T.Start();

            const int FakeClientID = 1234;
            ClientInstance.OnIDAvailable += (object sender, IDEventArgs args) =>
            {
                // Verify client ID
                Assert.AreEqual(FakeClientID, args.ID);
            };

            using (StreamWriter Writer = new StreamWriter(ClientConnection.GetStream(), Encoding.UTF8))
            {
                // Client ID
                Writer.WriteLine(FakeClientID.ToString());
            }

            for (int i = 0; i < Iterations; i++)
            {
                Payload P = Payload.Create(i.ToString() + "|U|271|928");
                ClientInstance.QueueMessage(P);
            }

            Assert.True(T.Join(5000), "Messsages were not received in time");
        }
    }
}
