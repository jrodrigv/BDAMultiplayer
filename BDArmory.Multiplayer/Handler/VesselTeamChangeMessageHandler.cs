using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using BDArmory.Events;
using BDArmory.Misc;
using BDArmory.Modules;
using BDArmory.Multiplayer.Interface;
using BDArmory.UI;
using Debug = UnityEngine.Debug;

namespace BDArmory.Multiplayer.Handler
{
    internal class VesselTeamChangeMessageHandler : IBdaMessageHandler<VesselTeamChangeEventArgs>
    {
        public bool ProcessMessage(VesselTeamChangeEventArgs message)
        {
            if (message == null) return false;

            Vessel vessel = FlightGlobals.VesselsLoaded.FirstOrDefault(v => v.id == message.VesselId);

            if (vessel == null || vessel.packed) return false; 

            List<MissileFire> weaponManagerList = vessel.FindPartModulesImplementing<MissileFire>();

            foreach (var weaponManager in weaponManagerList)
            {
                Debug.Log($"VesselTeamChangeMessageHandler.ProcessMessage for {message.VesselId}");
                weaponManager.SetTeam(BDTeam.Get(message.Team),false);
            }

            return true;
        }
    }
}
