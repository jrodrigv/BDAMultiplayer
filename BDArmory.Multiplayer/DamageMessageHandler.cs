using System.Linq;
using BDArmory.Core;
using BDArmory.Core.Enum;
using BDArmory.Core.Events;
using BDArmory.Core.Extension;
using BDArmory.Core.Services;
using UnityEngine;

namespace BDArmory.Multiplayer
{
    internal class DamageMessageHandler : IBdaMessageHandler<DamageEventArgs>
    {
        public void ProcessMessage(DamageEventArgs message)
        {
            if (message == null)
            {
                Debug.LogError("[BDArmory]: DamageMessageHandler message is null");

            }
            else
            {
                var vessel = FlightGlobals.VesselsLoaded.FirstOrDefault(v => v.id == message.VesselId);

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
}