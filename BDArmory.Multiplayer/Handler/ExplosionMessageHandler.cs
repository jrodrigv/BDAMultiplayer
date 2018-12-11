using System;
using System.Linq;
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
            if (message == null) return;

            Vector3 offsetCorrection = Vector3.zero;

            if (message.TargetVesselId != Guid.Empty)
            {
                Vector3 vesselPositionInClient = new Vector3(message.TargetVesselComX, message.TargetVesselComY,
                    message.TargetVesselComZ);

                Vessel vessel = FlightGlobals.VesselsLoaded.FirstOrDefault(v => v.id == message.TargetVesselId);

                if (vessel != null && !vessel.packed)
                {
                    // calculating offset
                    offsetCorrection = vessel.CoM - vesselPositionInClient;
                }
            }
       
            ExplosionFx.CreateVisualExplosion(new Vector3(message.PositionX , message.PositionY , message.PositionZ),message.TntMassEquivalent, message.ExplosionModelPath, message.SoundPath, new Vector3(message.DirectionX, message.DirectionY, message.DirectionZ));
        }
    }
}