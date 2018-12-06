using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BDArmory.Events
{
    public class TurretAimEventService : NotificableService
    {
        public virtual void PublishTurretAimEvent(Guid vesselId, uint partFlightId, uint partCraftId, Vector3 direction)
        {
            PublishEvent(new TurretAimEventArgs()
            {
                VesselId = vesselId,
                PartFlightId = partFlightId,
                PartCraftId = partCraftId,
                DirectionX = direction.x,
                DirectionY = direction.y,
                DirectionZ = direction.z
            });
        }
    }
}
