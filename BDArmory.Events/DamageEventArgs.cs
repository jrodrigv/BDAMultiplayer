using System;

namespace BDArmory.Events
{
    [Serializable]
    public class DamageEventArgs : EventArgs
    {
        //public int VesselId { get; set; }
        //public int PartId { get; set; }
        public float Damage { get; set; }
        public float Armor { get; set; }
        public Guid VesselId { get; set; }
        public double Damage { get; set; }
        public DamageOperation Operation { get; set; }
        public uint PartFlightId { get; set; }
        public uint PartCraftId { get; set; }
    }
}