using System;

namespace BDArmory.Events
{
    [Serializable]
    public class ExplosionEventArgs : EventArgs
    {
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }

        public float TntMassEquivalent { get; set; }

        public string ExplosionModelPath { get; set; }
        public string SoundPath { get; set; }

        public float DirectionX { get; set; }
        public float DirectionY { get; set; }
        public float DirectionZ { get; set; }

        public float TargetVesselComX { get; set; }
        public float TargetVesselComY { get; set; }
        public float TargetVesselComZ { get; set; }

        public Guid? TargetVesselId { get; set; }
    }

}