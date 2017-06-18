using BDArmory.Core.Enum;
using BDArmory.Core.Events;

namespace BDArmory.Core.Services
{
    internal class TemperatureDamageService : DamageService
    {
        public override void SetDamageToPart(Part p, double damage)
        {
            p.temperature = damage;
            PublishDamageEvent(p, damage);
        }

        public override void AddDamageToPart(Part p, double damage)
        {
            p.temperature += damage;
            PublishDamageEvent(p, damage);
        }

        private void PublishDamageEvent(Part p, double damage)
        {
            PublishEvent(new DamageEventArgs()
            {
                VesselId = p.vessel.id,
                PartFlightId = p.flightID,
                PartCraftId = p.craftID,
                Damage = damage,
                Operation = DamageOperation.Add
            });
        }
    }
}
