using System.Linq;
using BDArmory.Core;
using BDArmory.Core.Interface;
using BDArmory.Events;
using BDArmory.Multiplayer.Interface;
using BDArmory.Multiplayer.Utils;

namespace BDArmory.Multiplayer.Handler
{
    internal class ArmorMessageHandler : IBdaMessageHandler<ArmorEventArgs>
    {
        public bool ProcessMessage(ArmorEventArgs message)
        {
            if (message == null) return false;

            Part part = PartUtils.GetPart(message.VesselId, message.PartFlightId, message.PartCraftId);

            if (part == null) return false;

            Dependencies.Get<IDamageService>().ReduceArmor(part , message.ArmorMassToReduce);

            return true;
        }
    }
}