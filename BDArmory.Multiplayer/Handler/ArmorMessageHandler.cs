using System.Linq;
using BDArmory.Core;
using BDArmory.Core.Interface;
using BDArmory.Events;
using BDArmory.Multiplayer.Interface;

namespace BDArmory.Multiplayer.Handler
{
    internal class ArmorMessageHandler : IBdaMessageHandler<ArmorEventArgs>
    {
        public void ProcessMessage(ArmorEventArgs message)
        {
            if (message == null) return;

            Vessel vessel = FlightGlobals.VesselsLoaded.FirstOrDefault(v => v.id == message.VesselId);

            if (vessel == null || vessel.packed) return;

            Part part = vessel.Parts.FirstOrDefault(p => p.flightID == message.PartFlightId) ??
                        vessel.Parts.FirstOrDefault(p => p.craftID == message.PartCraftId);

            if (part == null) return;

            Dependencies.Get<IDamageService>().ReduceArmor(part , message.ArmorMassToReduce);
        }
    }
}