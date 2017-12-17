
namespace BDArmory.Core.Interface
{
    public interface IDamageService
    {
        void ReduceArmor(Part p, float armorMass);

        void SetDamageToPart(Part p, float damage);

        void AddDamageToPart(Part p, float damage);

        void AddDamageToKerbal(KerbalEVA kerbal, float damage);
    }
}