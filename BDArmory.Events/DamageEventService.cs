﻿namespace BDArmory.Events
{
    public class DamageEventService : NotificableService <DamageEventArgs>
    {
        public void PublishDamageEvent(Part p, float damage, DamageOperation operation)
        {
            PublishEvent(new DamageEventArgs
            {
                VesselId = p.vessel.id,
                PartFlightId = p.flightID,
                PartCraftId = p.craftID,
                Damage = damage,
                Operation = operation
            });
        }
    }
}