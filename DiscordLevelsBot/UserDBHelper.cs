using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace DiscordLevelsBot
{
    /// <summary>Entry point helper for user database.</summary>
    public class UserDBHelper
    {
        /// <summary>Helper to get the correct current timestamp.</summary>
        public static long CurrentTimeStamp => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Map of guild IDs to database instances.</summary>
        public static Dictionary<ulong, UserDBHelper> DatabaseByGuild = new();

        /// <summary>Shuts down the DB helper.</summary>
        public static void Shutdown()
        {
            foreach (UserDBHelper helper in DatabaseByGuild.Values)
            {
                try
                {
                    helper.ShutdownInstance();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Database shutdown error: {ex}");
                }
            }
            DatabaseByGuild.Clear();
        }

        /// <summary>Gets the database object for a given guild ID, or creates one if needed.</summary>
        public static UserDBHelper GetDBForGuild(ulong guild)
        {
#warning TODO
        }

        /// <summary>The raw database instance.</summary>
        public LiteDatabase Database;
        
        /// <summary>The guild ID this database is for.</summary>
        public ulong Guild;

        /// <summary>Initializes the database.</summary>
        public void Init()
        {
#warning TODO
        }

        /// <summary>Shuts down this one instance.</summary>
        public void ShutdownInstance()
        {
#warning TODO
        }

        /// <summary>Gets the user object for a given user ID, or creates one if needed.</summary>
        public UserData GetUser(ulong id)
        {
#warning TODO
        }

        /// <summary>Updates the given user object in the database.</summary>
        public void UpdateUser(UserData user)
        {
#warning TODO
        }

        /// <summary>Grants the given amount of XP to the given user.</summary>
        public void GrantXP(UserData user, int xp)
        {
            if (xp <= 0)
            {
                throw new ArgumentException($"XP {xp} is invalid: must be > 0", "xp");
            }
            user.XP += xp;
            user.LastUpdatedTime = CurrentTimeStamp;
            UpdateUser(user);
        }

        /// <summary>If the user has earned XP, grants it and updates.</summary>
        /// <returns>True if XP was granted, false if not.</returns>
        public bool GrantXPIfNeeded(UserData data)
        {
#warning TODO
        }
    }
}
