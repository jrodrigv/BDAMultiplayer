using System;

namespace BDArmory.Events
{
    [Serializable]
    public class DamageEventArgs : EventArgs
    {
        public Guid VesselId { get; set; }
        public double Damage { get; set; }
        public DamageOperation Operation { get; set; }
        public uint PartFlightId { get; set; }
        public uint PartCraftId { get; set; }
    }
}