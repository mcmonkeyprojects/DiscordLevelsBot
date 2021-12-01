using System;
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
using FreneticUtilities.FreneticExtensions;

namespace DiscordLevelsBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AssemblyLoadContext.Default.Unloading += (context) =>
            {
                UserDBHelper.Shutdown();
            };
            AppDomain.CurrentDomain.ProcessExit += (obj, e) =>
            {
                UserDBHelper.Shutdown();
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
                OnShutdown = () =>
                {
                    ConsoleCancelToken.Cancel();
                    UserDBHelper.Shutdown();
                }
            };
            Task consoleThread = Task.Run(ConsoleLoop, ConsoleCancelToken.Token);
            DiscordBotBaseHelper.StartBotHandler(args, config);
        }

        public static CancellationTokenSource ConsoleCancelToken = new();

        public static void Initialize(DiscordBot bot)
        {
            bot.Client.MessageReceived += Client_MessageReceived;
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
                    default:
                        Console.WriteLine("Unknown command.");
                        break;
                }
            }
        }

        public static async Task Client_MessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot || message.Author.IsWebhook)
            {
                return;
            }
            if (message is not IUserMessage userMessage)
            {
                return;
            }
            if (message.Channel is not SocketTextChannel channel)
            {
                return;
            }
            if (message.Author is not SocketGuildUser author)
            {
                return;
            }
            UserDBHelper database = UserDBHelper.GetDBForGuild(channel.Guild.Id);
            if (database.Config.RestrictedChannels.Contains(channel.Id))
            {
                return;
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
                    didGrantAny = database.GrantXPIfNeeded(user);
                }
                if (didGrantAny && user.Level > origLevel)
                {
                    IReadOnlyCollection<SocketRole> roles = author.Roles;
                    foreach (GuildConfig.LevelUpReward reward in database.Config.LevelRewards)
                    {
                        if (user.Level >= reward.Level)
                        {
                            if (!roles.Any(r => r.Id == reward.Role))
                            {
                                try
                                {
                                    await author.AddRoleAsync(reward.Role);
                                }
                                catch (Exception ex)
                                {
                                    Console.Error.WriteLine($"Failed to add role {reward.Role} to user {user.RawID} in guild {database.Guild}: {ex}");
                                }
                            }
                        }
                        if (user.Level >= database.Config.MinimumLevelForNotif)
                        {
                            UserCommands.SendReply(userMessage, new EmbedBuilder().WithTitle("Level up!").WithDescription($"Congratulations <@{user.RawID}>! You're now at level {user.Level}!")
                                .WithColor(0, 128, 255).WithFooter("[Discord Levels Bot](https://github.com/mcmonkeyprojects/DiscordLevelsBot)").Build());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error while doing XP grant check: {ex}");
            }
        }
    }
}
