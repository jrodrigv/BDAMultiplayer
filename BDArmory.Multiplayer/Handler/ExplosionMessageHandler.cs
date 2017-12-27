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
            ExplosionFx.CreateVisualExplosion(new Vector3(message.PositionX, message.PositionY, message.PositionZ),message.TntMassEquivalent, message.ExplosionModelPath, message.SoundPath, new Vector3(message.DirectionX, message.DirectionY, message.DirectionZ));
        }
    }
}