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

    class Payload
    {
        public int ID { get; private set; }
        public PayloadType Type { get; private set; }
        public int From { get; private set; }
        public int To { get; private set; }
        public string Raw { get; private set; }

        // Error may happen during initialization, so it's best to use factory pattern
        private Payload()
        {            
        }

        /// <summary>
        /// Create a payload instance from raw data
        /// </summary>
        /// <param name="raw">raw payload data</param>
        /// <returns>instance if data is valid, null otherwise</returns>
        public static Payload Create(string raw)
        {
            Payload Instance = new Payload();
            int Test = -1;
            string[] components = raw.Split('|');

            if (int.TryParse(components[0], out Test))
            {
                Instance.ID = Test;
            } else
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

        public override string ToString()
        {
            // Append back the line break cut from the protocol
            return Raw + "\r\n";
        }

        private int RetryCount = 0;

        public bool ShouldRetry()
        {
            RetryCount++;
            if (RetryCount > Constants.RetryLimit)
                return false;
            return true;
        }
    }
}
