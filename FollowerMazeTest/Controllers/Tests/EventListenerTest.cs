using FollowerMazeServer;
using FollowerMazeServer.DataObjects;
using NUnit.Framework;
using System;
using System.Threading;

namespace FollowerMazeTest.Controllers
{
    // The CI doesn't go well with sockets, test passed on local
#if (!TRAVIS)

    [TestFixture]
    public sealed class EventListenerTest : IDisposable
    {        
        private const int ClientsCount = 5;
        private TestClient[] Clients = new TestClient[ClientsCount];
        private TestEventSource EventSource = new TestEventSource(ClientsCount);
        private EventListener L;

        [TestFixtureSetUp]
        public void Init()
        {
            for (int i = 0; i < ClientsCount; i++)
            {
                Clients[i] = new TestClient(i);
            }
            L = new EventListener();
            L.Start();
        }

        [TestFixtureTearDown]
        public void Dispose()
        {
            foreach (TestClient C in Clients)
            {
                C.Stop();
            }
            L.Stop();
            L.Dispose();
        }

        [Test]
        public void EventListener1Creation()
        {
            Assert.NotNull(L, "Cannot create an instance of EventListener");
        }

        [Test]
        public void EventListener2EventSourceConnection()
        {
            const int MessagesToSend = 5;
            EventSource.Start();
            Thread.Sleep(100);
            EventSource.SendMessages(MessagesToSend);
            Thread.Sleep(100);
            Assert.AreEqual(MessagesToSend,
                L.ProcessedMessagesCount + L.PendingMessagesCount,
                "Event source could not connect and send events");
        }

        [Test]
        public void EventListener3ClientConnection()
        {
            int ConnectedCount = 0;
            foreach (TestClient C in Clients)
            {
                C.OnConnect += (object Client, IDEventArgs E) =>
                {
                    ConnectedCount++;
                };
                C.Start();
            }
            // Wait until all clients connect, or 5s, whichever comes first
            SpinWait.SpinUntil(() => ConnectedCount == ClientsCount, 5000);
            Assert.AreEqual(ClientsCount, ConnectedCount, "All clients did not connect to event listener");
        }

        [Test]
        public void EventListener4MessageDispatching()
        {
            int MessageCount = 0;
            foreach (TestClient C in Clients)
            {
                C.OnMessage += (object Client, MessageEventArgs E) =>
                {
                    MessageCount++;
                };
                C.Start();
            }
            const int MessagesToSend = 50;
            EventSource.SendMessages(MessagesToSend);
            // Wait until all message are received by clients, or 5s, whichever comes first
            SpinWait.SpinUntil(() => MessagesToSend == MessageCount, 5000);
            Assert.AreEqual(MessagesToSend, MessageCount, "All messages did not get sent");
        }
#endif
    }
}