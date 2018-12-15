using System;

namespace BDArmory.Events
{
    public class VesselTeamChangeService : NotificableService
    {
        public void PublishVesselTeamEvent(Guid vesselId, string team)
        {
            PublishEvent(new VesselTeamChangeEventArgs
            {
                VesselId = vesselId,
                Team = team
            });
        }
    }
}