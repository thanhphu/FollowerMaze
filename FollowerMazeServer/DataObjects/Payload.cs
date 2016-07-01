namespace FollowerMazeServer
{
    public enum PayloadType
    {
        Follow,
        Unfollow,
        Broadcast,
        Private,
        Status
    }

    /// <summary>
    /// Represent a parsed event sent from event source, support parsing via factory method Payload.Create
    /// </summary>
    class Payload
    {
        public int ID { get; private set; }
        public PayloadType Type { get; private set; }
        public int From { get; private set; }
        public int To { get; private set; }
        public string Raw { get; private set; }

        /// <summary>
        /// Hidden constructor. Error may happen during initialization, so it's best to use factory pattern
        /// </summary>
        private Payload()
        {
        }

        /// <summary>
        /// Factory method to create a payload instance from event's string representation
        /// </summary>
        /// <param name="raw">raw payload data</param>
        /// <returns>instance if data is valid, null otherwise</returns>
        public static Payload Create(string raw)
        {
            if (raw == null)
                return null;

            Payload Instance = new Payload();
            int Test = -1;
            string[] components = raw.Split('|');

            if (int.TryParse(components[0], out Test))
            {
                Instance.ID = Test;
            }
            else
            {
                return null;
            }

            // If there's no type, or type is empty
            if (components.Length < 2 || string.IsNullOrEmpty(components[1]))
                return null;

            switch (components[1][0])
            {
                case 'F': Instance.Type = PayloadType.Follow; break;
                case 'U': Instance.Type = PayloadType.Unfollow; break;
                case 'B': Instance.Type = PayloadType.Broadcast; break;
                case 'P': Instance.Type = PayloadType.Private; break;
                case 'S': Instance.Type = PayloadType.Status; break;
                default: return null;
            }

            // All types of packet but broadcast have >= 3 fields
            if (Instance.Type != PayloadType.Broadcast)
            {
                if (components.Length < 3)
                    return null;
                if (int.TryParse(components[2], out Test))
                {
                    Instance.From = Test;
                }
                else
                {
                    return null;
                }

                // All types of packet but status have 4 fields
                if (Instance.Type != PayloadType.Status)
                {
                    if (components.Length < 4)
                        return null;
                    if (int.TryParse(components[3], out Test))
                    {
                        Instance.To = Test;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            Instance.Raw = raw;

            return Instance;
        }

        /// <summary>
        /// Returns the original string unmodified to client
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // Append back the line break cut from the protocol
            return Raw + "\r\n";
        }
    }
}
