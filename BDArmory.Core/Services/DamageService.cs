using BDArmory.Core.Enum;
using BDArmory.Core.Events;

namespace BDArmory.Core.Services
{
    public abstract class DamageService : NotificableService<DamageEventArgs>
    {
        public abstract void SetDamageToPart(Part p, double damage);

        public abstract void AddDamageToPart(Part p, double damage);

        public virtual void PublishDamageEvent(Part p, double damage, DamageOperation operation)
        {
            PublishEvent(new DamageEventArgs()
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