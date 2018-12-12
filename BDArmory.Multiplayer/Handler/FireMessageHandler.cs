using BDArmory.Core;
using BDArmory.Events;
using BDArmory.Modules;
using BDArmory.Multiplayer.Interface;
using BDArmory.Multiplayer.Utils;

namespace BDArmory.Multiplayer.Handler
{
    internal class FireMessageHandler : IBdaMessageHandler<FireEventArgs>
    {
        public void ProcessMessage(FireEventArgs message)
        {
            if (message == null) return;

            if (BDArmorySettings.MULTIPLAYER_VESSELS_OWNED.Contains(message.VesselId))
            {
                return;
            }

            var moduleWeapon =
                PartUtils.GetModuleFromPart<ModuleWeapon>(message.VesselId, message.PartFlightId, message.PartCraftId);

            if (moduleWeapon == null) return;

            moduleWeapon.UpdateVisualFire(message.Fire);
        }
    }
}