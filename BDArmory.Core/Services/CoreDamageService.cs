using BDArmory.Core.Interface;
using BDArmory.Core.Module;
using BDArmory.Events;

namespace BDArmory.Core.Services
{
    internal class CoreDamageService : IDamageService
    {
        public void ReduceArmor(Part p, float armorMass)
        {
            var damageModule = p.Modules.GetModule<HitpointTracker>();
            damageModule.ReduceArmor(armorMass);
        }
        
        public void SetDamageToPart(Part p, float partDamage)
        {
            var damageModule = p.Modules.GetModule<HitpointTracker>();

            damageModule.SetDamage(partDamage);
        }              

        public void AddDamageToPart(Part p, float partDamage)
        {
            var damageModule = p.Modules.GetModule<HitpointTracker>();

            damageModule.AddDamage(partDamage);
        }

        public void AddDamageToKerbal(KerbalEVA kerbal, float damage)
        {
            var damageModule = kerbal.part.Modules.GetModule<HitpointTracker>();

            damageModule.AddDamageToKerbal(kerbal,damage);
        }
    }
}
