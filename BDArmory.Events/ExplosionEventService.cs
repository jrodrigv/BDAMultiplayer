using System;
using UnityEngine;

namespace BDArmory.Events
{
    public class ExplosionEventService : NotificableService <ExplosionEventArgs>
    {
        public virtual void PublishExplosionEvent(Vector3 position, float tntMassEquivalent, string explModelPath, string soundPath, Vector3 direction = default(Vector3), Vessel targetVessel = null)
        {
            var explosionMessage = new ExplosionEventArgs()
            {
                PositionX = position.x,
                PositionY = position.y,
                PositionZ = position.z,
                TntMassEquivalent = tntMassEquivalent,
                ExplosionModelPath = explModelPath,
                SoundPath = soundPath,
                DirectionX = direction.x,
                DirectionY = direction.y,
                DirectionZ = direction.z,
                TargetVesselId = Guid.Empty
            };

            if (targetVessel != null)
            {
                explosionMessage.TargetVesselId = targetVessel.id;
                explosionMessage.TargetVesselComX = targetVessel.CoM.x;
                explosionMessage.TargetVesselComY = targetVessel.CoM.y;
                explosionMessage.TargetVesselComZ = targetVessel.CoM.z;
            }

            PublishEvent(explosionMessage);
        }
    }
}