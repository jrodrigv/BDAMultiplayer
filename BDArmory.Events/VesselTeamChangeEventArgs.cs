using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BDArmory.Events
{

    /// <summary>
    /// Message payload when a player changes a vessel team.
    /// </summary>
    [Serializable]
    public class VesselTeamChangeEventArgs : EventArgs
    {
        public string Team { get; set; }
        public Guid VesselId { get; set; }

    }
}
