using BDArmory.Core.Enum;
using BDArmory.Core.Services;

namespace BDArmory.Core.Extension
{
    /// <summary>
    /// Please only call this extensions from the BDArmory client code, this publish notification events to other clients
    /// </summary>
    public static class PartExtensions
    {
        public static  void AddDamage(this Part p, double damage)
        {
#if DEBUG
            DisplayDebugMessage("Adding " + damage + " damage to part " + p.name);
#endif
            Dependencies.Get<DamageService>().AddDamageToPart(p, damage);
            Dependencies.Get<DamageService>().PublishDamageEvent(p, damage, DamageOperation.Add);
        }

        public static void SetDamage(this Part p, double damage)
        {
#if DEBUG
            DisplayDebugMessage("Setting " + damage + " damage to part " + p.name);
#endif
            Dependencies.Get<DamageService>().SetDamageToPart(p, damage);
            Dependencies.Get<DamageService>().PublishDamageEvent(p, damage, DamageOperation.Add);
        }

        private static void DisplayDebugMessage(string message)
        {
           ScreenMessages.PostScreenMessage(new ScreenMessage(message, 5.0f,
               ScreenMessageStyle.UPPER_CENTER));
        }
    }
}