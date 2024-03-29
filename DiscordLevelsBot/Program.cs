﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Loader;
using DiscordBotBase;
using Discord.WebSocket;
using DiscordBotBase.CommandHandlers;
using Discord;
using System.Threading;
using FreneticUtilities.FreneticDataSyntax;
using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;

namespace DiscordLevelsBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SpecialTools.Internationalize();
            AssemblyLoadContext.Default.Unloading += (context) =>
            {
                Shutdown();
            };
            AppDomain.CurrentDomain.ProcessExit += (obj, e) =>
            {
                Shutdown();
            };
            if (!Directory.Exists("./saves"))
            {
                Directory.CreateDirectory("./saves");
            }
            DiscordBotConfig config = new()
            {
                CacheSize = 0,
                EnsureCaching = false,
                Initialize = Initialize,
                CommandPrefix = null,
                ShouldPayAttentionToMessage = (message) => message is SocketUserMessage uMessage && uMessage.Channel is SocketGuildChannel,
                OnShutdown = () =>
                {
                    Shutdown();
                }
            };
            Task consoleThread = Task.Run(ConsoleLoop, ConsoleCancelToken.Token);
            DiscordBotBaseHelper.StartBotHandler(args, config);
        }

        public static void Shutdown()
        {
            ConsoleCancelToken.Cancel();
            UserDBHelper.Shutdown();
            LevelsWeb.WebHelper?.Cancel();
        }

        public static long XPToNextLevelFrom(long level) => (5 * (level * level)) + (50 * level) + 100;

        public static CancellationTokenSource ConsoleCancelToken = new();

        public static void Initialize(DiscordBot bot)
        {
            if (bot.ConfigFile is not null)
            {
                FDSSection webpage = bot.ConfigFile.GetSection("webpage");
                if (webpage is not null && webpage.GetBool("enable", false).Value)
                {
                    LevelsWeb.WebHelper = new(webpage.GetString("listen"), LevelsWeb.GetPage);
                    LevelsWeb.WebURL = webpage.GetString("address");
                    LevelsWeb.Load();
                }
            }
            bot.Client.MessageReceived += Client_MessageReceived;
            bot.Client.Ready += () =>
            {
                bot.Client.SetGameAsync("for new level ups to grant", type: ActivityType.Watching).Wait();
                foreach (SocketGuild guild in bot.Client.Guilds)
                {
                    UserDBHelper.GetDBForGuild(guild); // Preload
                }
                try
                {
                    const string commandVersionFile = "./config/command_registered_version.dat";
                    const int commandVersion = 1;
                    if (!File.Exists(commandVersionFile) || !int.TryParse(commandVersionFile, out int registered) || registered < commandVersion)
                    {
                        RegisterSlashCommands(bot);
                        File.WriteAllText(commandVersionFile, commandVersion.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to update slash commands: {ex}");
                }
                return Task.CompletedTask;
            };
            bot.Client.UserJoined += (discordUser) =>
            {
                UserDBHelper database = UserDBHelper.GetDBForGuild(discordUser.Guild);
                UserData user;
                lock (database.Lock)
                {
                    user = database.GetUser(discordUser.Id);
                }
                CheckRewards(database, user, discordUser);
                return Task.CompletedTask;
            };
            bot.RegisterCommand(LevelsBotCommands.Command_Help, "help", "halp", "hlp", "?");
            bot.RegisterCommand(LevelsBotCommands.Command_Rank, "rank", "level", "xp", "exp", "experience", "levelup");
            bot.RegisterCommand(LevelsBotCommands.Command_Leaderboard, "leaderboard", "board", "leaders", "top");
            bot.RegisterCommand(LevelsBotCommands.Command_AdminConfigure, "admin-configure", "adminconfigure");
            bot.RegisterSlashCommand(LevelsBotCommands.SlashCommand_Rank, "rank");
            bot.RegisterSlashCommand(LevelsBotCommands.SlashCommand_Leaderboard, "leaderboard");
        }

        public static void RegisterSlashCommands(DiscordBot bot)
        {
            SlashCommandBuilder rankCommand = new SlashCommandBuilder().WithName("rank").WithDescription("Shows your current leveling rank and XP, or somebody else's.")
                .AddOption("user", ApplicationCommandOptionType.User, "(Optional) A different user to show the leveling info of.", isRequired: false);
            SlashCommandBuilder leaderboardCommand = new SlashCommandBuilder().WithName("leaderboard").WithDescription("Shows the whole leveling leaderboard for this Discord group.")
                .AddOption("start", ApplicationCommandOptionType.Integer, "(Optional) alternate index to start the board at (eg start at 11 to show ranks 11-20).", isRequired: false);
            bot.Client.BulkOverwriteGlobalApplicationCommandsAsync([rankCommand.Build(), leaderboardCommand.Build()]);
        }

        public static async void ConsoleLoop()
        {
            while (true)
            {
                string line = await Console.In.ReadLineAsync();
                if (line == null)
                {
                    return;
                }
                string[] split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (split.IsEmpty())
                {
                    continue;
                }
                switch (split[0])
                {
                    case "stop":
                        {
                            Console.WriteLine("Clearing up...");
                            UserDBHelper.Shutdown();
                            Console.WriteLine("Shutting down...");
                            Environment.Exit(0);
                        }
                        break;
                    case "import":
                        if (split.Length == 2 && ulong.TryParse(split[1], out ulong importGuildId))
                        {
                            DataImport(importGuildId);
                            Console.WriteLine("Done");
                        }
                        else
                        {
                            Console.WriteLine("import (guild_id)");
                        }
                        break;
                    case "reset_all_seen_times":
                        if (split.Length == 2 && ulong.TryParse(split[1], out ulong resetGuildId))
                        {
                            UserDBHelper database = UserDBHelper.GetDBForGuild(resetGuildId, "");
                            foreach (UserData user in database.Users.FindAll())
                            {
                                user.LastUpdatedTime = 0;
                                database.DBStoreUser(user);
                            }
                            Console.WriteLine("Done");
                        }
                        else
                        {
                            Console.WriteLine("reset_all_seen_times (guild_id)");
                        }
                        break;
                    case "replace_user_id":
                        if (split.Length == 4 && ulong.TryParse(split[1], out ulong targetGuildId) && ulong.TryParse(split[2], out ulong incorrectUserId) && ulong.TryParse(split[3], out ulong correctUserId))
                        {
                            UserDBHelper database = UserDBHelper.GetDBForGuild(targetGuildId, "");
                            UserData usr = database.Users.FindById(unchecked((long)incorrectUserId));
                            UserData targetUsr = database.Users.FindById(unchecked((long)correctUserId));
                            if (usr is null)
                            {
                                Console.WriteLine("Invalid user ID");
                            }
                            else
                            {
                                // Nuke old user from database
                                if (targetUsr is not null)
                                {
                                    database.RemoveUser(targetUsr);
                                }
                                database.RemoveUser(usr);
                                // Reset user data for recalculating
                                usr.LeaderboardPrev = 0;
                                usr.LeaderboardNext = 0;
                                usr.RawID = correctUserId;
                                database.UpdateUser(usr, null);
                                Console.WriteLine("Done");
                            }
                        }
                        else
                        {
                            Console.WriteLine("replace_user_id (guild_id) (incorrect_user_id) (correct_user_id)");
                        }
                        break;
                    default:
                        Console.WriteLine("Unknown command. Use 'stop' to close the process, or consult internal code for secondary options.");
                        break;
                }
            }
        }

        /// <summary>
        /// Imports legacy data in line-separated list format, where each line is:
        /// `avatar`, `name`, `xp`, `level`, `percentage_to_level`
        /// </summary>
        public static void DataImport(ulong id)
        {
            try
            {
                if (!File.Exists("config/import_data.txt")) // Note: data this was made for, was generated by https://gist.github.com/mcmonkey4eva/dfebb2e479b7d448082a52f84f3b5ddd
                {
                    Console.WriteLine("Missing import data file.");
                    return;
                }
                Dictionary<string, ulong> altNameLookup = [];
                Dictionary<string, ulong> additionalNameLookup = [];
                if (File.Exists("config/users_lastknown.txt"))
                {
                    foreach (string seen in File.ReadAllLines("config/users_lastknown.txt"))
                    {
                        if (string.IsNullOrWhiteSpace(seen))
                        {
                            continue;
                        }
                        string[] parts = seen[1..^1].Split("`, `");
                        altNameLookup[parts[0]] = ulong.Parse(parts[1]);
                    }
                }
                if (File.Exists("config/users_seen.txt"))
                {
                    foreach (string seen in File.ReadAllLines("config/users_seen.txt"))
                    {
                        if (string.IsNullOrWhiteSpace(seen))
                        {
                            continue;
                        }
                        string[] parts = seen[1..^1].Split("`, `");
                        additionalNameLookup[parts[0]] = ulong.Parse(parts[1]);
                    }
                }
                SocketGuild guild = DiscordBotBaseHelper.CurrentBot.Client.GetGuild(id);
                if (guild is null)
                {
                    Console.WriteLine("Invalid guild.");
                    return;
                }
                guild.DownloadUsersAsync().Wait();
                foreach (SocketUser user in guild.Users)
                {
                    altNameLookup[LevelsBotCommands.NameSimplifier.TrimToMatches(user.Username.ToLowerFast())] = user.Id;
                }
                UserDBHelper database = UserDBHelper.GetDBForGuild(id, "imported");
                string[] lines = File.ReadAllLines("config/import_data.txt");
                ulong fakeID = 100;
                int real = 0, alt = 0, fake = 0, dups = 0;
                StringBuilder outputFail = new();
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    if (!line.StartsWithFast('`') || !line.EndsWithFast('`'))
                    {
                        Console.Error.WriteLine($"Invalid line {line} - bad start or end character");
                        continue;
                    }
                    string[] parts = line[1..^1].Split("`, `");
                    if (parts.Length != 5)
                    {
                        Console.Error.WriteLine($"Invalid line {line} - has {parts.Length}, expected 5");
                        continue;
                    }
                    ulong userID;
                    if (parts[0].StartsWith("https://cdn.discordapp.com/avatars/"))
                    {
                        string idPart = parts[0]["https://cdn.discordapp.com/avatars/".Length..].Before('/');
                        if (!ulong.TryParse(idPart, out userID))
                        {
                            Console.Error.WriteLine($"Invalid line {line} - user id part {idPart} is not a valid ID.");
                            continue;
                        }
                        real++;
                    }
                    else
                    {
                        string nameLow = LevelsBotCommands.NameSimplifier.TrimToMatches(parts[1].ToLowerFast());
                        if (altNameLookup.TryGetValue(nameLow, out ulong nameLowId))
                        {
                            userID = nameLowId;
                            alt++;
                        }
                        else if (additionalNameLookup.TryGetValue(nameLow, out ulong backupId))
                        {
                            userID = backupId;
                            alt++;
                        }
                        else
                        {
                            Console.WriteLine($"Can't identify {nameLow}");
                            outputFail.Append($"User {nameLow} failed and given ID {fakeID}\n");
                            userID = fakeID++;
                            fake++;
                        }
                    }
                    UserData user = database.GetUser(userID);
                    if (user.XP != 0)
                    {
                        dups++;
                        continue;
                    }
                    if (user.LeaderboardNext != 0)
                    {
                        UserData next = database.GetUser(user.LeaderboardNext);
                        next.LeaderboardPrev = user.LeaderboardPrev;
                        database.DBStoreUser(next);
                    }
                    if (user.LeaderboardPrev != 0)
                    {
                        UserData prev = database.GetUser(user.LeaderboardPrev);
                        prev.LeaderboardNext = user.LeaderboardNext;
                        database.DBStoreUser(prev);
                    }
                    user.LeaderboardPrev = 0;
                    user.LeaderboardNext = 0;
                    user.Level = long.Parse(parts[3]);
                    user.PartialXP = (long)((float.Parse(parts[4]) * 0.01) * user.CalcTotalXPToNextLevel());
                    user.XP = user.PartialXP;
                    user.LastUpdatedTime = UserDBHelper.CurrentTimeStamp;
                    user.LastKnownAvatar = parts[0];
                    user.LastKnownName = parts[1];
                    for (long i = 0; i < user.Level; i++)
                    {
                        user.XP += XPToNextLevelFrom(i);
                    }
                    database.UpdateUser(user, null);
                }
                File.WriteAllText("config/fail_log.txt", outputFail.ToString());
                Console.WriteLine($"Updated {real} users correctly, {dups} duplicates ignored, {alt} from alternate sourcing, and {fake} users wrongly");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Legacy data import failed: {ex}");
            }
        }

        public static Task Client_MessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot || message.Author.IsWebhook)
            {
                return Task.CompletedTask;
            }
            if (message is not IUserMessage userMessage)
            {
                return Task.CompletedTask;
            }
            if (message.Channel is not SocketTextChannel channel)
            {
                return Task.CompletedTask;
            }
            if (message.Author is not SocketGuildUser author)
            {
                return Task.CompletedTask;
            }
            UserDBHelper database = UserDBHelper.GetDBForGuild(channel.Guild);
            ulong chanId = channel.Id;
            if (channel is SocketThreadChannel threadChannel)
            {
                chanId = threadChannel.ParentChannel.Id;
            }
            if (database.Config.RestrictedChannels.Contains(chanId))
            {
                return Task.CompletedTask;
            }
            bool didGrantAny;
            long origLevel;
            UserData user;
            try
            {
                lock (database.Lock)
                {
                    user = database.GetUser(message.Author.Id);
                    origLevel = user.Level;
                    didGrantAny = database.GrantXPIfNeeded(user, author);
                }
                if (didGrantAny && (user.Level > origLevel || Random.Shared.Next(100) <= 10))
                {
                    CheckRewards(database, user, author);
                    if (user.Level > origLevel && user.Level >= database.Config.MinimumLevelForNotif)
                    {
                        UserCommands.SendReply(userMessage, new EmbedBuilder().WithTitle("Level up!").WithDescription($"Congratulations <@{user.RawID}>! You're now at **level {user.Level}**!")
                            .WithThumbnailUrl(DiscordBotBaseHelper.CurrentBot.Client.CurrentUser.GetAvatarUrl())
                            .WithColor(0, 128, 255).WithAuthor(new EmbedAuthorBuilder().WithName("Discord Levels Bot").WithUrl("https://github.com/mcmonkeyprojects/DiscordLevelsBot")).Build());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error while doing XP grant check: {ex}");
            }
            return Task.CompletedTask;
        }

        public static void CheckRewards(UserDBHelper database, UserData user, SocketGuildUser discordUser)
        {
            IReadOnlyCollection<SocketRole> roles = discordUser.Roles;
            List<ulong> rewards = [];
            foreach (GuildConfig.LevelUpReward reward in database.Config.LevelRewards)
            {
                if (user.Level >= reward.Level)
                {
                    if (!roles.Any(r => r.Id == reward.Role))
                    {
                        rewards.Add(reward.Role);
                    }
                }
            }
            if (rewards.Any())
            {
                try
                {
                    discordUser.AddRolesAsync(rewards).Wait();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to add role(s) {rewards.JoinString(", ")} to user {user.RawID} in guild {database.Guild}: {ex}");
                }
            }
        }
    }
}
