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
        EventListener L;

        [TestFixtureSetUp]
        public void Init()
        {
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
        public void EventListenerCreation()
        {
            Assert.NotNull(L, "Cannot create an instance of EventListener");
        }
    }
}
