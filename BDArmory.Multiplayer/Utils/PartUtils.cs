using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSPAchievements;

namespace BDArmory.Multiplayer.Utils
{
    public static class PartUtils
    {
        public static Part GetPart (Guid? vesselGuid, uint partFlightId, uint partCraftId)
        {
            var vessel = FlightGlobals.VesselsLoaded.FirstOrDefault(v => v.id == vesselGuid);

            if (vessel == null || vessel.packed) return null;

            var part = vessel.Parts.FirstOrDefault(p => p.flightID == partFlightId) ??
                        vessel.Parts.FirstOrDefault(p => p.craftID == partCraftId);

            return part;
        }
        public static T GetModuleFromPart<T>(Guid? vesselGuid, uint partFlightId, uint partCraftId) where  T: PartModule
        {
            var part = GetPart(vesselGuid, partFlightId, partCraftId);

            return part == null ? null : part.FindModuleImplementing<T>();
        }
    }
}
