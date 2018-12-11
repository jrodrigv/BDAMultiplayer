using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BDArmory.Core;
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

            if (BDArmorySettings.MULTIPLAYER_VESSELS_OWNED.Contains(message.VesselId))
            {
                return;
            }

            var moduleTurret =
                PartUtils.GetModuleFromPart<ModuleTurret>(message.VesselId, message.PartFlightId, message.PartCraftId);

            if(moduleTurret == null) return;;

            moduleTurret.pitchTransform.localRotation = new Quaternion(message.PitchRotationX,message.PitchRotationY, message.PitchRotationZ, message.PitchRotationW);
            moduleTurret.yawTransform.localRotation = new Quaternion(message.YawRotationX, message.YawRotationY, message.YawRotationZ, message.YawRotationW);
        }
    }
}
