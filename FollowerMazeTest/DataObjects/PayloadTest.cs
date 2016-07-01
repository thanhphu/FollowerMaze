using NUnit.Framework;
using FollowerMazeServer;

namespace FollowerMazeTest
{
    [TestFixture]
    public class PayloadTest
    {
        [Test]
        public void NormalCase()
        {
            var ToParse = "196296|U|270|927";
            var P = Payload.Create(ToParse);
            Assert.AreEqual(P.ID, 196296);
            Assert.AreEqual(P.Type, PayloadType.Unfollow);
            Assert.AreEqual(P.From, 270);
            Assert.AreEqual(P.To, 927);
            Assert.AreEqual(P.ToString(), ToParse + "\r\n");
        }

        [Test]
        public void FailCase()
        {
            var ToParse = "196296|";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void FailPayloadType1()
        {
            var ToParse = "196296|X|270|927";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void FailPayloadType2()
        {
            // If we just check the first character, it may be null
            var ToParse = "196296||270|927";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void FailID()
        {
            var ToParse = "ABC|U|270|927";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void FailTo()
        {
            var ToParse = "123|U|A|927";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void FailFrom()
        {
            var ToParse = "ABC|U|270|B";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void PassBroadcast()
        {
            var ToParse = "123|B";
            var P = Payload.Create(ToParse);
            Assert.IsNotNull(P);
        }

        [Test]
        public void FailStatus()
        {
            var ToParse = "123|S";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void PassStatus()
        {
            var ToParse = "123|S|456";
            var P = Payload.Create(ToParse);
            Assert.IsNotNull(P);
        }

        [Test]
        public void FailFollow()
        {
            var ToParse = "123|F|456";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void PassFollow()
        {
            var ToParse = "123|F|456|789";
            var P = Payload.Create(ToParse);
            Assert.IsNotNull(P);
        }
    }
}
