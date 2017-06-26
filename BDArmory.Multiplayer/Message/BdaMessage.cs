
using System;

namespace BDArmory.Multiplayer.Message
{
    [Serializable]
    public class BdaMessage
    {
        public Type Type { get; set; }
        public EventArgs Content { get; set; }
    }
}
       