using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace DiscordLevelsBot
{
    /// <summary>Data for a single user.</summary>
    public class UserData
    {
        /// <summary>The internal raw user ID.</summary>
        public ulong RawID = 0;

        /// <summary>The ID reformatted for LiteDB.</summary>
        [BsonId]
        public long DB_ID_Signed
        {
            get => unchecked((long)RawID);
            set
            {
                RawID = unchecked((ulong)value);
            }
        }

        /// <summary>Total experience for this user.</summary>
        public long XP { get; set; } = 0;

        /// <summary>User's current level.</summary>
        public long Level { get; set; } = 0;

        /// <summary>Current XP towards the next level.</summary>
        public long PartialXP { get; set; } = 0;

        /// <summary>Calculates the XP needed to reach the next level.</summary>
        public long CalcTotalXPToNextLevel() => (5 * (Level * Level)) + (50 * Level) + 100;

        /// <summary>Unix 64-bit seconds timestamp of the last time the user received XP.</summary>
        public long LastUpdatedTime { get; set; } = 0;

        /// <summary>The next (higher) person on the leaderboard (or 0 if on top).</summary>
        public ulong LeaderboardNext { get; set; } = 0;

        /// <summary>The next (lower) person on the leaderboard (or 0 if on bottom).</summary>
        public ulong LeaderboardPrev { get; set; } = 0;
    }
}
