using System;
using BDArmory.Core.Enum;

namespace BDArmory.Core.Events
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