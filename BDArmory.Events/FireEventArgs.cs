using System;

namespace BDArmory.Events
{
    [Serializable]
    public class FireEventArgs : PartEventArgs
    {
        public bool Fire { get; set; }
    }
}