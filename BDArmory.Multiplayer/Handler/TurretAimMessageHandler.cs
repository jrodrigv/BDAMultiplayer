using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BDArmory.Events;
using BDArmory.Modules;
using BDArmory.Multiplayer.Interface;
using UnityEngine;

namespace BDArmory.Multiplayer.Handler
{
    class TurretAimMessageHandler : IBdaMessageHandler<TurretAimEventArgs>
    {
        public void ProcessMessage(TurretAimEventArgs message)
        {
            if (message == null) return;

            Vessel vessel = FlightGlobals.VesselsLoaded.FirstOrDefault(v => v.id == message.VesselId);

            if (vessel == null || vessel.packed) return;

            Part part = vessel.Parts.FirstOrDefault(p => p.flightID == message.PartFlightId) ??
                        vessel.Parts.FirstOrDefault(p => p.craftID == message.PartCraftId);

            if (part == null) return;

           
            part.FindModuleImplementing<ModuleTurret>()?.AimInDirection(new Vector3(message.DirectionX, message.DirectionY, message.DirectionZ), false);
        }
    }
}
