using System;
using UnityEngine;

namespace BDArmory.Events
{
    public class ForceEventService: NotificableService <ForceEventArgs>
    {
        public void PublishForceEvent(Part part, Vector3 force, Vector3 position, ForceMode mode)
        {
            PublishEvent(new ForceEventArgs
            {
                VesselId = part.vessel.id,
                PartFlightId = part.flightID,
                PartCraftId = part.craftID,
                PositionX = position.x,
                PositionY = position.y,
                PositionZ = position.z,
                ForceX = force.x,
                ForceY = force.y,
                ForceZ = force.z,
                Mode = mode
            });
        }
    }

    [Serializable]
    public class ForceEventArgs : EventArgs
    {
        public Guid VesselId { get; set; }
        public uint PartFlightId { get; set; }
        public uint PartCraftId { get; set; }

        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }

        public float ForceX { get; set; }
        public float ForceY { get; set; }
        public float ForceZ { get; set; }

        public ForceMode Mode { get; set; }
    }
}