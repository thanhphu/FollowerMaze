using FollowerMazeServer;
using NUnit.Framework;

namespace FollowerMazeTest
{
    [TestFixture]
    public class PayloadTest
    {
        [Test]
        public void PayloadNormalCase()
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
        public void PayloadFailCase()
        {
            var ToParse = "196296|";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void PayloadFailPayloadType1()
        {
            var ToParse = "196296|X|270|927";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void PayloadFailPayloadType2()
        {
            // If we just check the first character, it may be null
            var ToParse = "196296||270|927";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void PayloadFailID()
        {
            var ToParse = "ABC|U|270|927";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void PayloadFailTo()
        {
            var ToParse = "123|U|A|927";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void PayloadFailFrom()
        {
            var ToParse = "ABC|U|270|B";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void PayloadPassBroadcast()
        {
            var ToParse = "123|B";
            var P = Payload.Create(ToParse);
            Assert.IsNotNull(P);
        }

        [Test]
        public void PayloadFailStatus()
        {
            var ToParse = "123|S";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void PayloadPassStatus()
        {
            var ToParse = "123|S|456";
            var P = Payload.Create(ToParse);
            Assert.IsNotNull(P);
        }

        [Test]
        public void PayloadFailFollow()
        {
            var ToParse = "123|F|456";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [Test]
        public void PayloadPassFollow()
        {
            var ToParse = "123|F|456|789";
            var P = Payload.Create(ToParse);
            Assert.IsNotNull(P);
        }
    }
}