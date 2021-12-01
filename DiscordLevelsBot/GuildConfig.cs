using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordLevelsBot
{
    /// <summary>Holder of configuration related to a single guild.</summary>
    public class GuildConfig
    {
        /// <summary>Minimum amount of XP per event of receiving XP.</summary>
        public int MinXPPerTick { get; set; } = 15;

        /// <summary>Maximum amount of XP per event of receiving XP.</summary>
        public int MaxXPPerTick { get; set; } = 25;

        /// <summary>Minimum amount of time between messages before a user receives more XP.</summary>
        public int SecondsBetweenXPTick { get; set; } = 60;
    }
}
