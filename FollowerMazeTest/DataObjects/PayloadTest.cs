using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FollowerMazeServer;

namespace FollowerMazeTest
{
    [TestClass]
    public class PayloadTest
    {
        [TestMethod]
        public void NormalCase()
        {
            var ToParse = "196296|U|270|927";
            var P = Payload.Create(ToParse);
            Assert.AreEqual(P.ID, 196296);
            Assert.AreEqual(P.Type, PayloadType.Unfollow);
            Assert.AreEqual(P.From, 270);
            Assert.AreEqual(P.To, 927);
        }

        [TestMethod]
        public void FailPayloadType()
        {
            var ToParse = "196296|X|270|927";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [TestMethod]
        public void FailID()
        {
            var ToParse = "ABC|U|270|927";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [TestMethod]
        public void FailTo()
        {
            var ToParse = "123|U|A|927";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }

        [TestMethod]
        public void FailFrom()
        {
            var ToParse = "ABC|U|270|B";
            var P = Payload.Create(ToParse);
            Assert.IsNull(P);
        }
    }
}
