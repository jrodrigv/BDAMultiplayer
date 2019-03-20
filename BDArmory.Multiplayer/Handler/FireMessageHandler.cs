using BDArmory.Core;
using BDArmory.Events;
using BDArmory.Modules;
using BDArmory.Multiplayer.Interface;
using BDArmory.Multiplayer.Utils;

namespace BDArmory.Multiplayer.Handler
{
    internal class FireMessageHandler : IBdaMessageHandler<FireEventArgs>
    {
        public bool ProcessMessage(FireEventArgs message)
        {
            if (message == null) return false;

            if (BDArmorySettings.MULTIPLAYER_VESSELS_OWNED.Contains(message.VesselId))
            {
                return false;
            }

            var moduleWeapon =
                PartUtils.GetModuleFromPart<ModuleWeapon>(message.VesselId, message.PartFlightId, message.PartCraftId);

            if (moduleWeapon == null) return false;

            moduleWeapon.UpdateVisualFire(message.Fire);
            return true;
        }
    }
}