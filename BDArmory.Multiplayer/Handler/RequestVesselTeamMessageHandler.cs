using System.Collections.Generic;
using System.Linq;
using BDArmory.Core;
using BDArmory.Events;
using BDArmory.Modules;
using BDArmory.Multiplayer.Interface;

namespace BDArmory.Multiplayer.Handler
{
    public  class RequestVesselTeamMessageHandler : IBdaMessageHandler<RequestVesselTeamEventArgs>
    {
        public bool ProcessMessage(RequestVesselTeamEventArgs message)
        {
            if (message == null) return false;

            Vessel vessel = FlightGlobals.VesselsLoaded.FirstOrDefault(v => v.id == message.VesselId);

            if (vessel == null || vessel.packed) return false;

            if (BDArmorySettings.MULTIPLAYER_VESSELS_OWNED.Contains(vessel.id))
            {
                List<MissileFire> weaponManagerList = vessel.FindPartModulesImplementing<MissileFire>();

                foreach (var weaponManager in weaponManagerList)
                {
                    Dependencies.Get<VesselTeamChangeService>().PublishVesselTeamEvent(vessel.id, weaponManager.teamString);
                }
            }

            return true;
        }
    }
}