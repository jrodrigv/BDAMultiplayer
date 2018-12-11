using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BDArmory.Events
{
    [Serializable]
    public class TurretAimEventArgs : PartEventArgs
    {
        public float PitchRotationX { get; set; }
        public float PitchRotationY { get; set; }
        public float PitchRotationZ { get; set; }
        public float PitchRotationW { get; set; }

        public float YawRotationX { get; set; }
        public float YawRotationY { get; set; }
        public float YawRotationZ { get; set; }
        public float YawRotationW { get; set; }


    }
}
