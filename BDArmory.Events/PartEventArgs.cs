using System;

namespace BDArmory.Events
{
    [Serializable]
    public class PartEventArgs : VesselEventArgs
    {
        public uint PartFlightId { get; set; }
        public uint PartCraftId { get; set; }
    }
}