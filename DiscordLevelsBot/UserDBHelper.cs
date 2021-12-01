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
                Config.Ensure();
                ConfigCollection.Insert(0, Config);
                UpdateConfig();
            }
        }

        /// <summary>Updates the config file in the database.</summary>
        public void UpdateConfig()
        {
            lock (Lock)
            {
                ConfigCollection.Upsert(0, Config);
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
                    Level = 0,
                    PartialXP = 0,
                    LastUpdatedTime = 0,
                    LeaderboardNext = 0,
                    LeaderboardPrev = 0
                };
            }
        }

        /// <summary>Automatically repositions a user in the leaderboard.</summary>
        public void Reposition(UserData user)
        {
            lock (Lock)
            {
                UserData next = user.LeaderboardNext == 0 ? null : GetUser(user.LeaderboardNext);
                if (next is null) // If we're already at the top, no position change needed
                {
                    return;
                }
                if (next.XP >= user.XP) // Only try to move if we can move at least one.
                {
                    return;
                }
                next.LeaderboardPrev = user.LeaderboardPrev;
                Users.Update(next);
                UserData prev = user.LeaderboardPrev == 0 ? null : GetUser(user.LeaderboardPrev);
                if (prev is not null)
                {
                    prev.LeaderboardNext = user.LeaderboardNext;
                    Users.Update(prev);
                }
                else // If prev is null, we were the bottom
                {
                    Config.BottomID = next.RawID;
                    UpdateConfig();
                }
                user.LeaderboardNext = 0;
                user.LeaderboardPrev = 0;
                while (next.XP < user.XP)
                {
                    if (next.LeaderboardNext == 0)
                    {
                        next.LeaderboardNext = user.RawID;
                        Users.Update(next);
                        user.LeaderboardPrev = next.RawID;
                        Users.Upsert(user);
                        Config.TopID = user.RawID;
                        UpdateConfig();
                        return;
                    }
                    next = GetUser(next.LeaderboardNext);
                }
                ulong origPrev = next.LeaderboardPrev;
                if (origPrev != 0)
                {
                    prev = GetUser(origPrev);
                    prev.LeaderboardNext = user.RawID;
                    Users.Update(prev);
                    user.LeaderboardPrev = origPrev;
                }
                next.LeaderboardPrev = user.RawID;
                Users.Update(next);
                user.LeaderboardNext = next.RawID;
                Users.Upsert(user);
            }
        }

        /// <summary>Updates the given user object in the database, calculating leaderboard reposition if needed.</summary>
        public void UpdateUser(UserData user)
        {
            lock (Lock)
            {
                if (user.LeaderboardNext == 0 && user.LeaderboardPrev == 0)
                {
                    if (Config.BottomID == 0)
                    {
                        Users.Upsert(user);
                        Config.BottomID = user.RawID;
                        Config.TopID = user.RawID;
                        UpdateConfig();
                    }
                    else
                    {
                        UserData bottom = GetUser(Config.BottomID);
                        bottom.LeaderboardPrev = user.RawID;
                        Users.Update(bottom);
                        user.LeaderboardNext = bottom.RawID;
                        Users.Upsert(user);
                        Config.BottomID = user.RawID;
                        UpdateConfig();
                    }
                }
                else
                {
                    Users.Upsert(user);
                }
                Reposition(user);
            }
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
                user.PartialXP += xp;
                user.LastUpdatedTime = CurrentTimeStamp;
                long toNext = user.CalcTotalXPToNextLevel();
                while (user.PartialXP >= toNext)
                {
                    user.Level++;
                    user.PartialXP -= toNext;
                    toNext = user.CalcTotalXPToNextLevel();
                }
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
