using System;

namespace BDArmory.Events
{
    [Serializable]
    public class ExplosionEventArgs : EventArgs
    {
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }

        public float Radius { get; set; }
        public float Power { get; set; }
        public string ExplosionModelPath { get; set; }
        public string SoundPath { get; set; }
    }
}