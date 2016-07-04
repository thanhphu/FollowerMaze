using FollowerMazeServer;
using FollowerMazeServer.Controllers;
using NUnit.Framework;
using System;
using System.Threading;

namespace FollowerMazeTest.Controllers
{
    [TestFixture]
    // Test for dummy client (and abstract client)
    public class DummyClientTest
    {
        static Payload P1 = Payload.Create("196296|U|270|927");
        static Payload P2 = Payload.Create("196297|U|271|928");
        static Payload P3 = Payload.Create("196238|U|221|921");
        static Random R = new Random();

        [Test]
        public void DummyCreation()
        {
            AbstractClient C = new DummyClient(1);
            Assert.That(C.GetMessages().Count == 0, "Dummy client's constructor error!");
        }
        
        // Shared with Connected client too
        internal static void TestFollowers(AbstractClient C)
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
        public void DummyFollowers()
        {
            TestFollowers(new DummyClient(1));
        }


        internal static void TestMessages(AbstractClient C)
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
        public void DummyMessages()
        {
            TestMessages(new DummyClient(1));
        }

        internal static Payload RandomPayload()
        {
            
            return Payload.Create(R.Next().ToString() + "|U|271|928");
        }

        internal static void TestFollowerConcurrency(AbstractClient C)
        {
            const int Iterations = 10000;
            // Two threads racing each other adding and removing followers
            new Thread(new ThreadStart(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    C.AddFollower(R.Next());
                }
            })).Start();
            new Thread(new ThreadStart(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    var List = C.GetCurrentFollowers();
                    if (List.Count > 0)
                    {
                        C.RemoveFollower(List[R.Next(List.Count)]);
                    } else
                    {
                        // Remove non-existant followers should work too!
                        C.RemoveFollower(R.Next());
                    }
                }
            })).Start();
            // No exception should be thrown
            Assert.Pass();
        }

        [Test]
        public void DummyFollowerConcurrency()
        {
            TestFollowerConcurrency(new DummyClient(1));
        }

        internal static void TestMessageConcurrency(AbstractClient C)
        {
            const int Iterations = 10000;
            new Thread(new ThreadStart(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    C.QueueMessage(RandomPayload());
                }
            })).Start();
            new Thread(new ThreadStart(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    C.QueueMessage(RandomPayload());
                }
            })).Start();
            // No exception should be thrown
            Assert.Pass();
        }

        [Test]
        public void DummyMessageConcurrency()
        {
            TestMessageConcurrency(new DummyClient(1));
        }
                
    }
}
