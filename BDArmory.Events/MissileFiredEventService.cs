namespace BDArmory.Events
{
    public class MissileFiredEventService : NotificableService<MissileFireEventArgs>
    {
        public void PublishMissileFired(Vessel missileVessel, bool team)
        {
            PublishEvent(
                new MissileFireEventArgs()
                {
                    VesselId = missileVessel.id,
                    Team = team
                }
            );
        }
        
    }
}