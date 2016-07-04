using FollowerMazeServer;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FollowerMazeTest.Controllers
{
    [TestFixture]
    public sealed class EventListenerTest: IDisposable
    {
        const int ClientsCount = 100;
        TestClient[] Clients = new TestClient[ClientsCount];
        TestEventSource EventSource = new TestEventSource(ClientsCount);
        EventListener L;

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
            L.Stop();
            L.Dispose();
        }

        [Test]
        public void EventListener1Creation()
        {
            Assert.NotNull(L, "Cannot create an instance of EventListener");
        }

        [Test]
        public void EventListener2Connection()
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
    }
}
