using System;

namespace BDArmory.Events
{
    public class RequestTeamEventService : NotificableService<VesselEventArgs>
    {
        public void PublishRequest(Guid vesselId)
        {
            PublishEvent(new VesselEventArgs
            {
                VesselId = vesselId,
            });
        }
    }
}