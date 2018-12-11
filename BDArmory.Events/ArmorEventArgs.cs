using System;

namespace BDArmory.Events
{
    [Serializable]
    public class ArmorEventArgs : PartEventArgs
    {
        public float ArmorMassToReduce { get; set; }
    }
}