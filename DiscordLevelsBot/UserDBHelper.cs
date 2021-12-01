using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using FreneticUtilities.FreneticToolkit;

namespace DiscordLevelsBot
{
    /// <summary>Entry point helper for user database.</summary>
    public class UserDBHelper
    {
        /// <summary>Helper to get the correct current timestamp.</summary>
        public static long CurrentTimeStamp => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Map of guild IDs to database instances.</summary>
        public static ConcurrentDictionary<ulong, UserDBHelper> DatabaseByGuild = new();

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
            return DatabaseByGuild.GetOrAdd(guild, (id) =>
            {
                UserDBHelper created = new()
                {
                    Guild = id,
                };
                created.Init();
                return created;
            });
        }

        /// <summary>The raw database instance.</summary>
        public LiteDatabase Database;

        /// <summary>The raw underlying user collection.</summary>
        public ILiteCollection<UserData> Users;

        /// <summary>DB collection for guild configuration data.</summary>
        public ILiteCollection<GuildConfig> ConfigCollection;
        
        /// <summary>The guild ID this database is for.</summary>
        public ulong Guild;

        /// <summary>Configuration for this guild.</summary>
        public GuildConfig Config;

        /// <summary>A lock for this DB.</summary>
        public LockObject Lock = new();

        /// <summary>Random number generator.</summary>
        public Random Random = new();

        /// <summary>Initializes the database.</summary>
        public void Init()
        {
            Database = new LiteDatabase($"./saves/guild_{Guild}.ldb", null);
            Users = Database.GetCollection<UserData>("user_data");
            ConfigCollection = Database.GetCollection<GuildConfig>("guild_config");
            Config = ConfigCollection.FindById(0);
            if (Config == null)
            {
                Config = new GuildConfig();
                ConfigCollection.Insert(0, Config);
            }
        }

        /// <summary>Shuts down this one instance.</summary>
        public void ShutdownInstance()
        {
            lock (Lock)
            {
                Database.Dispose();
            }
        }

        /// <summary>Gets the user object for a given user ID, or creates one if needed.</summary>
        public UserData GetUser(ulong id)
        {
            lock (Lock)
            {
                long dbId = unchecked((long)id);
                UserData user = Users.FindById(dbId);
                if (user is not null)
                {
                    return user;
                }
                return new UserData()
                {
                    RawID = id,
                    XP = 0,
                    LastUpdatedTime = 0,
                    LeaderboardNext = 0,
                    LeaderboardPrev = 0
                };
            }
        }

        /// <summary>Updates the given user object in the database.</summary>
        public void UpdateUser(UserData user)
        {
            lock (Lock)
            {
            }
#warning TODO
        }

        /// <summary>Grants the given amount of XP to the given user.</summary>
        public void GrantXP(UserData user, int xp)
        {
            lock (Lock)
            {
                if (xp <= 0)
                {
                    throw new ArgumentException($"XP {xp} is invalid: must be > 0", nameof(xp));
                }
                user.XP += xp;
                user.LastUpdatedTime = CurrentTimeStamp;
                UpdateUser(user);
            }
        }

        /// <summary>If the user has earned XP, grants it and updates.</summary>
        /// <returns>True if XP was granted, false if not.</returns>
        public bool GrantXPIfNeeded(UserData data)
        {
            lock (Lock)
            {
                if (data.LastUpdatedTime + Config.SecondsBetweenXPTick <= CurrentTimeStamp)
                {
                    GrantXP(data, Random.Next(Config.MinXPPerTick, Config.MaxXPPerTick));
                    return true;
                }
                return false;
            }
        }
    }
}
