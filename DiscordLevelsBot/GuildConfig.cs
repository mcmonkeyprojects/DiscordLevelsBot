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

        /// <summary>The ID of the top user.</summary>
        public ulong TopID { get; set; } = 0;

        /// <summary>The ID of the bottom user.</summary>
        public ulong BottomID { get; set; } = 0;

        /// <summary>The lowest level that is allowed to show level-up notifications.</summary>
        public int MinimumLevelForNotif { get; set; } = 5;

        /// <summary>A set of channel IDs forbidden from receiving XP.</summary>
        public HashSet<ulong> RestrictedChannels { get; set; }

        public List<LevelUpReward> LevelRewards { get; set; }

        /// <summary>Represents a single reward given for leveling up.</summary>
        public class LevelUpReward
        {
            /// <summary>Level that when reached grants the reward.</summary>
            public int Level { get; set; }

            /// <summary>Role ID to grant.</summary>
            public ulong Role { get; set; }
        }

        /// <summary>Ensures all lists are initialized.</summary>
        public void Ensure()
        {
            RestrictedChannels ??= [];
            LevelRewards ??= [];
        }
    }
}
