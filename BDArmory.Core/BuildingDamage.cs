using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BDArmory.Core
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class BuildingDamage : ScenarioDestructibles
    {
        public override void OnAwake()
        {
            Debug.Log("[BDArmory]: Modifying Buildings");

            if (!HighLogic.LoadedSceneIsFlight) return;

            foreach (KeyValuePair<string, ProtoDestructible> bldg in protoDestructibles)
            {
                if (bldg.Value == null) return;

                DestructibleBuilding building = bldg.Value.dBuildingRefs[0];
                building.damageDecay = 600f;
                building.impactMomentumThreshold *= 150;
            }

        }

    }
}
