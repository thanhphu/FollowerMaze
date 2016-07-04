namespace FollowerMazeServer.Controllers
{
    /// <summary>
    /// "Dummy" client is a client that doesn't connect but is referenced in the events,
    /// we should keep a list of followers and messages intended for it, should it connects later.
    /// If it connects, it will hand over the collect data to Connected Client
    /// </summary>
    internal class DummyClient : AbstractClient
    {
        public DummyClient(int ID)
        {
            this.ClientID = ID;
            InvokeIDEvent();
        }
    }
}