using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordLevelsBot
{
    /// <summary>Data for a single user.</summary>
    public class UserData
    {
        /// <summary>The internal raw user ID.</summary>
        public ulong RawID;

        /// <summary>The ID reformatted for LiteDB.</summary>
        public long _ID
        {
            get => unchecked((long)RawID);
            set
            {
                RawID = unchecked((ulong)value);
            }
        }

        /// <summary>Total experience for this user.</summary>
        public long XP { get; set; }

        /// <summary>Unix 64-bit seconds timestamp of the last time the user received XP.</summary>
        public long LastUpdatedTime { get; set; }

        /// <summary>The next (higher) person on the leaderboard (or 0 if on top).</summary>
        public ulong LeaderboardNext { get; set; }

        /// <summary>The next (lower) person on the leaderboard (or 0 if on bottom).</summary>
        public ulong LeaderboardPrev { get; set; }
    }
}
