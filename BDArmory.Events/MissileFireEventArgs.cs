using System;

namespace BDArmory.Events
{
    [Serializable]
    public class MissileFireEventArgs : VesselEventArgs
    {
        public string TeamName { get; set; }
    }
}