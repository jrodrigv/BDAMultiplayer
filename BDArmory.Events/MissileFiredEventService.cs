namespace BDArmory.Events
{
    public class MissileFiredEventService : NotificableService<MissileFireEventArgs>
    {
        public void PublishMissileFired(Vessel missileVessel, string teamName)
        {
            PublishEvent(
                new MissileFireEventArgs()
                {
                    VesselId = missileVessel.id,
                    TeamName = teamName
                }
            );
        }
        
    }
}