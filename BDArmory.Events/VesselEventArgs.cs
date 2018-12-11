using System;

namespace BDArmory.Events
{
    [Serializable]
    public class VesselEventArgs : EventArgs
    {
        public Guid VesselId { get; set; }
    }
}