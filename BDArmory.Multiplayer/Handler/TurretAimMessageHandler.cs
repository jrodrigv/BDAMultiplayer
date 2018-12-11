using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BDArmory.Events;
using BDArmory.Modules;
using BDArmory.Multiplayer.Interface;
using BDArmory.Multiplayer.Utils;
using UnityEngine;

namespace BDArmory.Multiplayer.Handler
{
    class TurretAimMessageHandler : IBdaMessageHandler<TurretAimEventArgs>
    {
        public void ProcessMessage(TurretAimEventArgs message)
        {
            if (message == null) return;

            var moduleTurret =
                PartUtils.GetModuleFromPart<ModuleTurret>(message.VesselId, message.PartFlightId, message.PartCraftId);


            moduleTurret?.AimInDirection(new Vector3(message.DirectionX, message.DirectionY, message.DirectionZ), false);
        }
    }
}
