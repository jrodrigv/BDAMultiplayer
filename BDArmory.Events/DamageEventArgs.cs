using System;

namespace BDArmory.Events
{
    [Serializable]
    public class DamageEventArgs : PartEventArgs
    {
        public float Damage { get; set; }
        public float Armor { get; set; }
        public DamageOperation Operation { get; set; }     
    }
}