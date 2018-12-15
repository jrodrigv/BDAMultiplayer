using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BDArmory.Events
{
    public class TurretAimEventService : NotificableService <TurretAimEventArgs>
    {
        public virtual void PublishTurretAimEvent(Guid vesselId, uint partFlightId, uint partCraftId, Quaternion pitchRotation, Quaternion yawRotation)
        {
            PublishEvent(new TurretAimEventArgs()
            {
                VesselId = vesselId,
                PartFlightId = partFlightId,
                PartCraftId = partCraftId,
                PitchRotationW = pitchRotation.w ,
                PitchRotationX = pitchRotation.x,
                PitchRotationY = pitchRotation.y,
                PitchRotationZ = pitchRotation.z ,
                YawRotationW = yawRotation.w ,
                YawRotationX = yawRotation.x ,
                YawRotationY = yawRotation.y,
                YawRotationZ = yawRotation.z
            });
        }
    }
}
