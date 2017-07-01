using UnityEngine;

namespace BDArmory.Events
{
    public class ExplosionEventService : NotificableService
    {
        public virtual void PublishExplosionEvent(Vector3 position, float radius, float power, string explModelPath, string soundPath)
        {
            PublishEvent(new ExplosionEventArgs()
            {
                PositionX = position.x,
                PositionY = position.y,
                PositionZ = position.z,
                Radius = radius,
                Power = power,
                ExplosionModelPath = explModelPath,
                SoundPath = soundPath
            });
        }
    }
}