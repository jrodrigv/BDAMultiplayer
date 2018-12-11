using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BDArmory.Core;
using BDArmory.Core.Extension;
using BDArmory.Events;
using BDArmory.Multiplayer.Interface;
using BDArmory.Multiplayer.Utils;
using UnityEngine;

namespace BDArmory.Multiplayer.Handler
{
    internal class ForceMessageHandler : IBdaMessageHandler<ForceEventArgs>
    {
        public void ProcessMessage(ForceEventArgs message)
        {
            if (message == null) return;

            Part part = PartUtils.GetPart(message.VesselId, message.PartFlightId, message.PartCraftId);

            if (part == null) return;

            part.AddForceToPart(new Vector3(message.ForceX, message.ForceY, message.ForceZ),
                new Vector3(message.PositionX, message.PositionY, message.PositionZ), message.Mode, false);

        }
    }
}
