using UnityEngine;

namespace BDArmory.Events
{
    public class ExplosionEventService : NotificableService
    {
        public virtual void PublishExplosionEvent(Vector3 position, float tntMassEquivalent, string explModelPath, string soundPath, Vector3 direction = default(Vector3))
        {
            PublishEvent(new ExplosionEventArgs()
            {
                PositionX = position.x,
                PositionY = position.y,
                PositionZ = position.z,
                TntMassEquivalent = tntMassEquivalent,
                ExplosionModelPath = explModelPath,
                SoundPath = soundPath,
                DirectionX = direction.x,
                DirectionY = direction.y,
                DirectionZ = direction.z
            });
        }
    }
}