using FollowerMazeServer;
using FollowerMazeServer.Controllers;
using NUnit.Framework;

namespace FollowerMazeTest.Controllers
{
    [TestFixture]
    // Test for dummy client (and abstract client)
    public class DummyClientTest
    {
        Payload P1 = Payload.Create("196296|U|270|927");
        Payload P2 = Payload.Create("196297|U|271|928");
        Payload P3 = Payload.Create("196238|U|221|921");

        [Test]
        public void TestDummyCreation()
        {
            AbstractClient C = new DummyClient(1);
            Assert.That(C.GetMessages().Count == 0, "Dummy client's constructor error!");
        }
        
        // Shared with Connected client too
        internal void TestFollowers(AbstractClient C)
        {
            
            C.AddFollower(1);
            C.AddFollower(2);
            // Double add
            C.AddFollower(2);
            C.AddFollower(3);
            var Followers = C.GetCurrentFollowers();
            Assert.That(Followers.Count == 3, "Dummy client's follower adding error! A follower with the same ID can only be added once!");
            Assert.That(Followers.Contains(2), "Dummy client's follower adding error!");
            Assert.False(Followers.Contains(4), "Dummy client's follower checking error!");

            // External modification must not affect internal operation
            Followers.Add(5);
            Assert.False(C.GetCurrentFollowers().Contains(5), "Dummy client's GetCurrentFollowers should not return the internal follower list!");
        }

        [Test]
        public void TestDummyFollowers()
        {
            TestFollowers(new DummyClient(1));
        }


        internal void TestMessages(AbstractClient C)
        {
            C.QueueMessage(P1);
            C.QueueMessage(P2);
            C.QueueMessage(P2);

            var Messages = C.GetMessages();
            Assert.That(Messages.Count == 2, "Dummy client's message adding error! A message with the same ID can only be added once!");
            C.QueueMessage(null);
            Assert.That(Messages.Count == 2, "Dummy client's message adding error! Null message should not be queued");
            Assert.That(Messages.Contains(P2), "Dummy client's message adding error!");
            Assert.False(Messages.Contains(P3), "Dummy client's message checking error!");

            // External modification must not affect internal operation
            Messages.Enqueue(P3);
            Assert.False(C.GetMessages().Contains(P3), "Dummy client's GetMessages should not return the internal list!");
        }

        [Test]
        public void TestDummyMessages()
        {
            TestMessages(new DummyClient(1));
        }

        // TODO test concurrency and message ordering
    }
}
