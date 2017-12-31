using System;

namespace BDArmory.Events
{
    [Serializable]
    public class ArmorEventArgs : EventArgs
    {
        public Guid VesselId { get; set; }
        public uint PartFlightId { get; set; }
        public uint PartCraftId { get; set; }
        public float ArmorMassToReduce { get; set; }
    }
}