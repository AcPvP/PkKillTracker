using System;

namespace PkKillTracker.Models
{
    public class PkKill
    {
        //public uint Id { get; set; }

        //public uint KillerId { get; set; }

        //public uint VictimId { get; set; }

        //public uint KillerMonarchId { get; set; }

        //public uint VictimMonarchId { get; set; }

        public string KillerName { get; set; }

        public string VictimName { get; set; }

        public string KillerMonarchName { get; set; }

        public string VictimMonarchName { get; set; }

        public DateTime? KillDateTime { get; set; }

        public string KillDateTimeString { get { return KillDateTime.HasValue ? KillDateTime.Value.ToString("yyyy-MM-dd hh:mm tt") : ""; } }

    }
}
