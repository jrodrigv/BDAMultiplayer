using BDArmory.Events;
using BDArmory.FX;
using BDArmory.Multiplayer.Interface;
using UnityEngine;

namespace BDArmory.Multiplayer.Handler
{
    internal class ExplosionMessageHandler : IBdaMessageHandler<ExplosionEventArgs>
    {
        public void ProcessMessage(ExplosionEventArgs message)
        {
            ExplosionFx.CreateExplosionAnimation(new Vector3(message.PositionX, message.PositionY, message.PositionZ),
                message.Radius, message.Power, message.ExplosionModelPath, message.SoundPath, false);
        }
    }
}