using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorIotaNode
{
    public class NodeInfo
    {
        public string Version { get; set; }
        public int NetworkVersion { get; set; }
        public string IdentityIdShort { get; set; }
        public int SolidMessageCount { get; set; }
        public int TotalMessageCount { get; set; }
        public TangleTime TangleTime { get; set; }
        public Mana Mana { get; set; }
    }

    public class TangleTime
    {
        public bool Synced { get; set; }
        public long Time { get; set; }

        [JsonIgnore]
        public DateTime DateTime => UnixTimeStampToDateTime(Time / 1_000_000_000);

        public override string ToString()
        {
            return $"TangleTime is {DateTime} and is{(Synced ? " " : " NOT ")}synced";
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }

    public class Mana
    {
        public decimal Access { get; set; }
        public decimal Consensus { get; set; }

        public static Mana operator +(Mana manaOne) => manaOne;

        public static Mana operator -(Mana manaOne) => new Mana(-manaOne.Access, -manaOne.Consensus);

        public static Mana operator +(Mana manaOne, Mana manaTwo) => new Mana(manaOne.Access + manaTwo.Access, manaOne.Consensus + manaTwo.Consensus);

        public static Mana operator -(Mana manaOne, Mana manaTwo) => manaOne + (-manaTwo);

        public static Mana operator *(Mana manaOne, Mana manaTwo) => new Mana(manaOne.Access * manaTwo.Access, manaOne.Consensus * manaTwo.Consensus);

        public static Mana operator *(Mana manaOne, int factor) => new Mana(manaOne.Access * factor, manaOne.Consensus * factor);

        public Mana(decimal access, decimal consensus)
        {
            Access = access;
            Consensus = consensus;
        }

        public override string ToString()
        {
            return $"Access Mana: {Access:0} - Consensus Mana: {Consensus}";
        }
    }
}
