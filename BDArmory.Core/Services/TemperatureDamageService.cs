namespace BDArmory.Core.Services
{
    internal class TemperatureDamageService : DamageService
    {
        public override void SetDamageToPart(Part p, double damage)
        {
            p.temperature = damage;           
        }

        public override void AddDamageToPart(Part p, double damage)
        {
            p.temperature += damage;
        }
    }
}
