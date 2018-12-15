using System;

namespace BDArmory.Events
{
    public class FireEventService : NotificableService<FireEventArgs>
    {
        public virtual void PublishFireEvent(Guid vesselId, uint partFlightId, uint partCraftId, bool fire)
        {
            PublishEvent(new FireEventArgs()
            {
                VesselId = vesselId,
                PartFlightId = partFlightId,
                PartCraftId = partCraftId,
                Fire = fire
            });
        }
    }
}