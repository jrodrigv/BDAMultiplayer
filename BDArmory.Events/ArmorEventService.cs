using System;

namespace BDArmory.Events
{
    public class ArmorEventService : NotificableService <ArmorEventArgs>
    {
        public void PublishDamageEvent(Part p, float armorMassToReduce)
        {
            PublishEvent(new ArmorEventArgs
            {
                VesselId = p.vessel.id,
                PartFlightId = p.flightID,
                PartCraftId = p.craftID,
                ArmorMassToReduce = armorMassToReduce,
            });
        }
    }

}