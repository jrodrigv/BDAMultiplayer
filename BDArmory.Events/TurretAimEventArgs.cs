using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BDArmory.Events
{
    [Serializable]
    public class TurretAimEventArgs : EventArgs
    {
        public Guid? VesselId { get; set; }
        public uint PartFlightId { get; set; }
        public uint PartCraftId { get; set; }

        public float DirectionX { get; set; }
        public float DirectionY { get; set; }
        public float DirectionZ { get; set; }
    }
}
