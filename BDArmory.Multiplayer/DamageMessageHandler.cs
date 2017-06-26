using System.Linq;
using BDArmory.Core;
using BDArmory.Core.Enum;
using BDArmory.Core.Events;
using BDArmory.Core.Services;

namespace BDArmory.Multiplayer
{
    internal class DamageMessageHandler : IBdaMessageHandler<DamageEventArgs>
    {
        public void ProcessMessage(DamageEventArgs message)
        {
            if(message == null) return;
        
            Vessel vessel = FlightGlobals.VesselsLoaded.FirstOrDefault(v => v.id == message.VesselId);

            if (vessel == null || vessel.packed) return;

            Part part = vessel.Parts.FirstOrDefault(p => p.flightID == message.PartFlightId) ??
                        vessel.Parts.FirstOrDefault(p => p.craftID == message.PartCraftId);

            if (part == null) return;

            if (message.Operation == DamageOperation.Add)
            {
                Dependencies.Get<DamageService>().AddDamageToPart(part, message.Damage);
            }
            else
            {
                Dependencies.Get<DamageService>().SetDamageToPart(part, message.Damage);
            }
            
        }
    }
}