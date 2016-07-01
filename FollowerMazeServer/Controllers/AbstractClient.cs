using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FollowerMazeServer.Controllers
{
    abstract class AbstractClient
    {
        #region Data
        protected int ClientID;
        protected List<int> Followers = new List<int>();
        protected Queue<Payload> Messages = new Queue<Payload>();

        // Triggered when the client sends its ID
        public event EventHandler<IDEventArgs> OnIDAvailable;

        // Triggered when the client disconnects
        public event EventHandler<IDEventArgs> OnDisconnect;
        #endregion

        protected AbstractClient()
        {
        }

        protected void InvokeIDEvent()
        {
            OnIDAvailable?.Invoke(this, new IDEventArgs(ClientID));
        }

        protected void InvokeDisconnectEvent()
        {
            // Event handler not set in this class, better check if it is properly assigned
            OnDisconnect?.Invoke(this, new IDEventArgs(ClientID));
        }

        #region Follower handling
        public void AddFollower(int Target)
        {
            lock (Followers)
            {
                Followers.Add(Target);
            }
        }

        public bool RemoveFollower(int Target)
        {
            bool Result = false;
            lock (Followers)
            {
                Result = Followers.Remove(Target);
            }
            return Result;
        }

        public List<int> GetCurrentFollowers()
        {
            // Returns a copy
            return new List<int>(Followers);
        }
        #endregion

        #region Message handling
        public Queue<Payload> GetMessages()
        {
            return new Queue<Payload>(Messages);
        }

        public void QueueMessage(Payload Message)
        {
            lock (Messages)
            {
                Messages.Enqueue(Message);
            }
        }
        #endregion
        
        #region Behavior
        public virtual void Start()
        {
        }

        public virtual void Stop()
        {
            InvokeDisconnectEvent();
        }
        #endregion
    }
}
