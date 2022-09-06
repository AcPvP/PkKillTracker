using System;

namespace PkKillTracker.Models
{
    public class Clan
    {
        public uint MonarchId { get; set; }
        
        public string ClanName { get; set; }

        public uint Kills { get; set; }

        public uint Deaths { get; set; }

        public float Ratio { get { return (float)Kills / (Deaths > 0 ? (float)Deaths : 1.0f); } }
    }
}
