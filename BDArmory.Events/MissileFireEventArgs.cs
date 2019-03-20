using System;

namespace BDArmory.Events
{
    [Serializable]
    public class MissileFireEventArgs : VesselEventArgs
    {
        public bool Team { get; set; }
    }
}