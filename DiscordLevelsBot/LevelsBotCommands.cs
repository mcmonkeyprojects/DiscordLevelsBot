using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBotBase;
using DiscordBotBase.CommandHandlers;
using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;

namespace DiscordLevelsBot
{
    /// <summary>Commands for the levels bots.</summary>
    public class LevelsBotCommands : UserCommands
    {
        /// <summary>A basic 'help' command.</summary>
        public static void Command_Help(CommandData command)
        {
            SendGenericPositiveMessageReply(command.Message, "Levels Bot - Help", "The levels bot automatically levels you up over time as you type messages."
                + "\nYou can type `/rank` at any time to see your current rank."
                + "\nor type `/rank (user)` to view somebody else's current rank."
                + "\nYou can type `/leaderboard` to see a leaderboard of users in this Discord group."
                + "\nAdmins can type `@LevelsBot admin-configure` to configure my settings."
                + "\n\nI'm [open source](https://github.com/mcmonkeyprojects/DiscordLevelsBot)!");
        }

        /// <summary>Characters that can be stripped from an '@' ping.</summary>
        public static AsciiMatcher PingIgnorableCharacters = new("<>!@");

        /// <summary>Very minimal character set to approximate names with, without escaping risk.</summary>
        public static AsciiMatcher NameSimplifier = new(AsciiMatcher.BothCaseLetters + AsciiMatcher.Digits + "_ ");

        /// <summary>Creates a rank information display embed for a given user.</summary>
        public static Embed BuildRankEmbedFor(UserData user)
        {
            string name = NameSimplifier.TrimToMatches(user.LastKnownName);
            DateTimeOffset lastSeen = DateTimeOffset.FromUnixTimeSeconds(user.LastUpdatedTime);
            return new EmbedBuilder().WithTitle($"Rank For {name}").WithThumbnailUrl(user.LastKnownAvatar)
                .AddField("User", $"<@{user.RawID}>", true)
                .AddField("Last Known Name", $"`{name}`", true)
                .AddField("Last Seen", $"{StringConversionHelper.DateTimeToString(lastSeen, false)} ... {(lastSeen - DateTimeOffset.UtcNow).SimpleFormat(true)}", true)
                .AddField("Total XP", $"{user.XP}", true)
                .AddField("Current Level", $"{user.Level}", true)
                .AddField("XP To Next Level", $"{user.PartialXP} / {user.CalcTotalXPToNextLevel()}", true)
                .WithColor(0, 255, 128).Build();
        }

        /// <summary>A command for users to show their own current rank, or somebody else's.</summary>
        public static void Command_Rank(CommandData command)
        {
            if (command.Message is not SocketUserMessage message || message.Channel is not SocketGuildChannel channel)
            {
                return;
            }
            ulong userId;
            if (command.RawArguments.IsEmpty())
            {
                userId = command.Message.Author.Id;
            }
            else if (command.RawArguments.Length != 1 || !ulong.TryParse(PingIgnorableCharacters.TrimToNonMatches(command.RawArguments[0]), out userId))
            {
                SendGenericNegativeMessageReply(command.Message, "Invalid Input", "Give no input, or a user ID or @ mention. Any other input won't work.");
                return;
            }
            UserDBHelper database = UserDBHelper.GetDBForGuild(channel.Guild.Id);
            UserData user = database.GetUser(userId);
            if (user is null || user.XP == 0)
            {
                SendGenericNegativeMessageReply(command.Message, "Unknown User", "User is unknown, or has never received any XP.");
                return;
            }
            SendReply(command.Message, BuildRankEmbedFor(user));
        }
        
        /// <summary>A command to show a guild-wide leaderboard.</summary>
        public static void Command_Leaderboard(CommandData command)
        {
            if (command.Message is not SocketUserMessage message || message.Channel is not SocketGuildChannel channel)
            {
                return;
            }
#warning TODO
        }

        /// <summary>A command to configure guild settings.</summary>
        public static void Command_AdminConfigure(CommandData command)
        {
            if (command.Message is not SocketUserMessage message || message.Channel is not SocketGuildChannel channel)
            {
                return;
            }
#warning TODO
        }
    }
}
