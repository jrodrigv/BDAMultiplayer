﻿using System.Linq;
using BDArmory.Core;
using BDArmory.Events;
using BDArmory.Misc;
using BDArmory.Modules;
using BDArmory.Multiplayer.Interface;
using LmpClient;
using UnityEngine;

namespace BDArmory.Multiplayer.Handler
{
    internal class MissileFireMessageHandler : IBdaMessageHandler<MissileFireEventArgs>
    {
        public bool ProcessMessage(MissileFireEventArgs message)
        {
            if (message == null) return false;

            Vessel vessel = FlightGlobals.VesselsLoaded.FirstOrDefault(v => v.id == message.VesselId);
          
            if (vessel == null || vessel.packed)
            {
                if (!LunaMultiplayerSystem.missileMessagePending.Contains(message))
                {
                    LunaMultiplayerSystem.missileMessagePending.Enqueue(message);
                }
              
                return false;
            }
            else
            {
                if (!vessel.loaded)
                {
                    vessel.Load();
                }
            }

            MissileBase missile = vessel.FindPartModuleImplementing<MissileBase>();

            if (missile == null)
            {
                return false;
            }

            if (BDArmorySettings.MULTIPLAYER_VESSELS_OWNED.Contains(message.VesselId))
            {
                return true;
            }

            if (missile.SourceVessel!= null && BDArmorySettings.MULTIPLAYER_VESSELS_OWNED.Contains(missile.SourceVessel.id))
            {
                return true;
            }

            missile.Team = BDTeam.Get(message.TeamName);
            missile.ActivateMissileMultiplayer();
            
            return true;
        }
    }
}