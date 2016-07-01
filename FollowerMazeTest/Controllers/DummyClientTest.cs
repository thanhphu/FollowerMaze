using FollowerMazeServer;
using FollowerMazeServer.Controllers;
using NUnit.Framework;

namespace FollowerMazeTest.Controllers
{
    [TestFixture]
    public class DummyClientTest
    {
        Payload P1 = Payload.Create("196296|U|270|927");
        Payload P2 = Payload.Create("196297|U|271|928");
        Payload P3 = Payload.Create("196238|U|221|921");

        [Test]
        public void TestDummyCreation()
        {
            AbstractClient C = new DummyClient(1);
            Assert.That(C.GetMessages().Count == 0);
        }

        [Test]
        public void TestDummyFollowers()
        {
            AbstractClient C = new DummyClient(1);
            C.AddFollower(1);
            C.AddFollower(2);
            C.AddFollower(3);
            var Followers = C.GetCurrentFollowers();
            Assert.That(Followers.Contains(2));
            Assert.False(Followers.Contains(4));

            // External modification must not affect internal operation
            Followers.Add(5);
            Assert.False(C.GetCurrentFollowers().Contains(5));
        }

        [Test]
        public void TestDummyMessages()
        {
            AbstractClient C = new DummyClient(1);
            C.QueueMessage(P1);
            C.QueueMessage(P2);
            
            var Messages = C.GetMessages();
            Assert.That(Messages.Contains(P2));
            Assert.False(Messages.Contains(P3));

            // External modification must not affect internal operation
            Messages.Enqueue(P3);
            Assert.False(C.GetMessages().Contains(P3));
        }
    }
}
