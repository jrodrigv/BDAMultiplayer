namespace BDArmory.Core
{
    public interface IDamageService
    {
        void SetDamageToPart(Part p, double damage);

        void AddDamageToPart(Part p, double damage);
    }
}