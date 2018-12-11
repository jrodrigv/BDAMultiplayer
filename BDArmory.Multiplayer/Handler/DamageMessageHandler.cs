using System.Linq;
using BDArmory.Core;
using BDArmory.Core.Interface;
using BDArmory.Events;
using BDArmory.Multiplayer.Interface;
using BDArmory.Multiplayer.Utils;

namespace BDArmory.Multiplayer.Handler
{
    internal class DamageMessageHandler : IBdaMessageHandler<DamageEventArgs>
    {
        public void ProcessMessage(DamageEventArgs message)
        {
            if(message == null) return;

            Part part = PartUtils.GetPart(message.VesselId, message.PartFlightId, message.PartCraftId);

            if (part == null) return;

            if (message.Operation == DamageOperation.Add)
            {
                Dependencies.Get<IDamageService>().AddDamageToPart(part, message.Damage);
            }
            else
            {
                Dependencies.Get<IDamageService>().SetDamageToPart(part, message.Damage);
            }
            
        }
    }
}