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
                + "\nor type `/leaderboard #` to jump the leaderboard ahead to a certain rank number"
                + "\nAdmins can type `@LevelsBot admin-configure` to configure my settings."
                + "\n\nI'm [open source](https://github.com/mcmonkeyprojects/DiscordLevelsBot)!");
        }

        /// <summary>Characters that can be stripped from an '@' ping.</summary>
        public static AsciiMatcher PingIgnorableCharacters = new("<>!@");

        /// <summary>Very minimal character set to approximate names with, without escaping risk.</summary>
        public static AsciiMatcher NameSimplifier = new(AsciiMatcher.BothCaseLetters + AsciiMatcher.Digits + "_ -=+#!.,[]()'\"$%^&");

        /// <summary>Creates a rank information display embed for a given user.</summary>
        public static Embed BuildRankEmbedFor(UserDBHelper database, UserData user)
        {
            string name = NameSimplifier.TrimToMatches(user.LastKnownName);
            long ranking = 1;
            UserData nextUser = user;
            while (nextUser.LeaderboardNext != 0)
            {
                nextUser = database.GetUser(nextUser.LeaderboardNext);
                ranking++;
            }
            EmbedBuilder builder = new EmbedBuilder().WithTitle($"Rank For {name}").WithThumbnailUrl(user.LastKnownAvatar)
                .AddField("User", $"<@{user.RawID}>", true)
                .AddField("Last Known Name", $"`{(string.IsNullOrWhiteSpace(name) ? "N/A" : name)}`", true)
                .AddField("Rank", $"**{ranking}**", true);
            if (user.LastUpdatedTime != 0 && Math.Abs(UserDBHelper.CurrentTimeStamp - user.LastUpdatedTime) > (2 * 60))
            {
                builder.AddField("Last Seen", $"<t:{user.LastUpdatedTime}> ... <t:{user.LastUpdatedTime}:R>");
            }
            builder.AddField("Total XP", $"{user.XP:n0}", true)
                .AddField("Current Level", $"{user.Level}", true)
                .AddField("XP To Next Level", $"{user.PartialXP} / {user.CalcTotalXPToNextLevel()}", true)
                .WithColor(0, 255, 128);
            return builder.Build();
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
            UserDBHelper database = UserDBHelper.GetDBForGuild(channel.Guild);
            UserData user = database.GetUser(userId);
            if (user is null || user.XP == 0)
            {
                SendGenericNegativeMessageReply(command.Message, "Unknown User", "User is unknown, or has never received any XP.");
                return;
            }
            SendReply(command.Message, BuildRankEmbedFor(database, user));
        }

        /// <summary>A command for users to show their own current rank, or somebody else's.</summary>
        public static void SlashCommand_Rank(SocketSlashCommand command)
        {
            command.DeferAsync().Wait();
            IGuildChannel channel = (command.Channel as IGuildChannel);
            ulong userId = command.User.Id;
            if (command.Data.Options.Count == 1)
            {
                SocketSlashCommandDataOption option = command.Data.Options.First();
                if (option.Type == ApplicationCommandOptionType.User && option.Value is IUser newUser)
                {
                    userId = newUser.Id;
                }
            }
            UserDBHelper database = UserDBHelper.GetDBForGuild(channel.Guild);
            UserData user = database.GetUser(userId);
            if (user is null || user.XP == 0)
            {
                SendGenericNegativeMessageReply(command, "Unknown User", "User is unknown, or has never received any XP.");
                return;
            }
            SendReply(command, BuildRankEmbedFor(database, user));
        }

        /// <summary>Adds a single user to a leaderboard embed message.</summary>
        public static void AddUserToBoard(StringBuilder output, int index, UserData user)
        {
            string name = NameSimplifier.TrimToMatches(user.LastKnownName);
            output.Append($"**Rank {index}:** <@{user.RawID}> (`{(string.IsNullOrWhiteSpace(name) ? "N/A" : name)}`): Level: **{user.Level}**, Total XP: **{user.XP:n0}**\n");
        }

        /// <summary>Builds a leaderboard embed message.</summary>
        public static Embed BuildBoardEmbed(int start, IGuild guild, UserDBHelper database)
        {
            int rank = 0;
            StringBuilder output = new();
            if (LevelsWeb.WebHelper is not null && LevelsWeb.WebURL is not null)
            {
                output.Append($"[Click To View Full Leaderboard Online]({LevelsWeb.WebURL}leaderboard/{guild.Id})\n\n");
            }
            UserData current = database.GetUser(database.Config.TopID);
            while (current != null)
            {
                rank++;
                if (rank >= start)
                {
                    AddUserToBoard(output, rank, current);
                    if (rank >= start + 9)
                    {
                        break;
                    }
                }
                current = current.LeaderboardPrev == 0 ? null : database.GetUser(current.LeaderboardPrev);
            }
            return new EmbedBuilder().WithTitle($"Leaderboard for `{NameSimplifier.TrimToMatches(guild.Name)}`").WithColor(255, 200, 15).WithDescription(output.ToString())
                .WithAuthor(new EmbedAuthorBuilder().WithName("Discord Levels Bot").WithUrl("https://github.com/mcmonkeyprojects/DiscordLevelsBot")).Build();
        }
        
        /// <summary>A command to show a guild-wide leaderboard.</summary>
        public static void Command_Leaderboard(CommandData command)
        {
            if (command.Message is not SocketUserMessage message || message.Channel is not SocketGuildChannel channel)
            {
                return;
            }
            int start = 1;
            if (command.CleanedArguments.Length == 1 && int.TryParse(command.CleanedArguments[0], out int newStart) && newStart > 0)
            {
                start = newStart;
            }
            UserDBHelper database = UserDBHelper.GetDBForGuild(channel.Guild);
            if (database.Config.TopID == 0)
            {
                SendGenericNegativeMessageReply(command.Message, "Error", "Empty database");
                return;
            }
            SendReply(command.Message, BuildBoardEmbed(start, channel.Guild, database));
        }

        /// <summary>A command to show a guild-wide leaderboard.</summary>
        public static void SlashCommand_Leaderboard(SocketSlashCommand command)
        {
            command.DeferAsync().Wait();
            int start = 1;
            if (command.Data.Options.Count == 1)
            {
                SocketSlashCommandDataOption option = command.Data.Options.First();
                if (option.Type == ApplicationCommandOptionType.Integer && option.Value is int newStart && newStart > 0)
                {
                    start = newStart;
                }
                // Note: docs say Integer value is 'int', but actual value in practice appears to be a long.
                else if (option.Type == ApplicationCommandOptionType.Integer && option.Value is long newStartLong && newStartLong > 0 && newStartLong < int.MaxValue)
                {
                    start = (int)newStartLong;
                }
            }
            UserDBHelper database = UserDBHelper.GetDBForGuild((command.Channel as IGuildChannel).Guild);
            if (database.Config.TopID == 0)
            {
                SendGenericNegativeMessageReply(command, "Error", "Empty database");
                return;
            }
            SendReply(command, BuildBoardEmbed(start, (command.Channel as IGuildChannel).Guild, database));
        }

        /// <summary>A command to configure guild settings.</summary>
        public static void Command_AdminConfigure(CommandData command)
        {
            if (command.Message is not SocketUserMessage message || message.Channel is not SocketGuildChannel channel || message.Author is not SocketGuildUser user)
            {
                return;
            }
            if (!user.Roles.Any(r => r.Permissions.Administrator) && channel.Guild.OwnerId != user.Id)
            {
                SendGenericNegativeMessageReply(command.Message, "Access Denied", "Only the discord owner, or members with the Admin privilege, may use the admin-configure command.");
                return;
            }
            UserDBHelper database = UserDBHelper.GetDBForGuild(user.Guild);
            if (command.CleanedArguments.Length == 2 && command.CleanedArguments[0] == "restrict_channel" && ulong.TryParse(command.CleanedArguments[1], out ulong restrictChanId))
            {
                database.Config.RestrictedChannels.Add(restrictChanId);
                SendGenericPositiveMessageReply(command.Message, "Success", "Added.");
            }
            else if (command.CleanedArguments.Length == 2 && command.CleanedArguments[0] == "unrestrict_channel" && ulong.TryParse(command.CleanedArguments[1], out ulong unrestrictChanId))
            {
                if (database.Config.RestrictedChannels.Remove(unrestrictChanId))
                {
                    SendGenericPositiveMessageReply(command.Message, "Success", "Removed.");
                }
                else
                {
                    SendGenericPositiveMessageReply(command.Message, "Failure", "That ID not listed.");
                    return;
                }
            }
            else if (command.CleanedArguments.Length == 3 && command.CleanedArguments[0] == "add_level_reward" && int.TryParse(command.CleanedArguments[1], out int addLevel) && ulong.TryParse(command.CleanedArguments[2], out ulong addRole))
            {
                database.Config.LevelRewards.Add(new GuildConfig.LevelUpReward() { Level = addLevel, Role = addRole });
                SendGenericPositiveMessageReply(command.Message, "Success", "Added.");
            }
            else if (command.CleanedArguments.Length == 3 && command.CleanedArguments[0] == "remove_level_reward" && int.TryParse(command.CleanedArguments[1], out int remLevel) && ulong.TryParse(command.CleanedArguments[2], out ulong remRole))
            {
                if (database.Config.LevelRewards.RemoveAll(r => r.Level == remLevel && r.Role == remRole) > 0)
                {
                    SendGenericPositiveMessageReply(command.Message, "Success", "Removed.");
                }
                else
                {
                    SendGenericPositiveMessageReply(command.Message, "Failure", "That Level/ID pair not listed.");
                    return;
                }
            }
            else if (command.CleanedArguments.Length == 1 && command.CleanedArguments[0] == "show_restrictions")
            {
                SendGenericPositiveMessageReply(command.Message, "Info", "Channels restricted from gaining XP: " + string.Join(", ", database.Config.RestrictedChannels.Select(id => $"<#{id}>")));
                return;
            }
            else if (command.CleanedArguments.Length == 1 && command.CleanedArguments[0] == "show_rewards")
            {
                SendGenericPositiveMessageReply(command.Message, "Info", "Role rewards configured: " + string.Join(", ", database.Config.LevelRewards.Select(p => $"Level **{p.Level}** gets role <@&{p.Role}>")));
                return;
            }
            else if (command.CleanedArguments.Length == 1 && command.CleanedArguments[0] == "min_xp_per_tick")
            {
                SendGenericPositiveMessageReply(command.Message, "Info", $"Current minimum XP per tick: {database.Config.MinXPPerTick}");
                return;
            }
            else if (command.CleanedArguments.Length == 2 && command.CleanedArguments[0] == "min_xp_per_tick" && int.TryParse(command.CleanedArguments[1], out int newMinXP))
            {
                database.Config.MinXPPerTick = newMinXP;
                SendGenericPositiveMessageReply(command.Message, "Success", "Set.");
            }
            else if (command.CleanedArguments.Length == 1 && command.CleanedArguments[0] == "max_xp_per_tick")
            {
                SendGenericPositiveMessageReply(command.Message, "Info", $"Current maximum XP per tick: {database.Config.MaxXPPerTick}");
                return;
            }
            else if (command.CleanedArguments.Length == 2 && command.CleanedArguments[0] == "max_xp_per_tick" && int.TryParse(command.CleanedArguments[1], out int newMaxXP))
            {
                database.Config.MaxXPPerTick = newMaxXP;
                SendGenericPositiveMessageReply(command.Message, "Success", "Set.");
            }
            else if (command.CleanedArguments.Length == 1 && command.CleanedArguments[0] == "seconds_per_tick")
            {
                SendGenericPositiveMessageReply(command.Message, "Info", $"Current seconds per tick: {database.Config.SecondsBetweenXPTick}");
                return;
            }
            else if (command.CleanedArguments.Length == 2 && command.CleanedArguments[0] == "seconds_per_tick" && int.TryParse(command.CleanedArguments[1], out int newSecPerTick))
            {
                database.Config.SecondsBetweenXPTick = newSecPerTick;
                SendGenericPositiveMessageReply(command.Message, "Success", "Set.");
            }
            else if (command.CleanedArguments.Length == 1 && command.CleanedArguments[0] == "min_notif_level")
            {
                SendGenericPositiveMessageReply(command.Message, "Info", $"Current minimum notification level: {database.Config.MinimumLevelForNotif}");
                return;
            }
            else if (command.CleanedArguments.Length == 2 && command.CleanedArguments[0] == "min_notif_level" && int.TryParse(command.CleanedArguments[1], out int newMinNotifLevel))
            {
                database.Config.MinimumLevelForNotif = newMinNotifLevel;
                SendGenericPositiveMessageReply(command.Message, "Success", "Set.");
            }
            else if (command.CleanedArguments[0] == "sweep")
            {
                channel.Guild.DownloadUsersAsync().Wait();
                foreach (SocketGuildUser otherUser in channel.Guild.Users)
                {
                    UserData udata = database.GetUser(otherUser.Id);
                    if (udata.Level > 0)
                    {
                        Program.CheckRewards(database, udata, otherUser);
                    }
                }
            }
            else
            {
                SendGenericNegativeMessageReply(command.Message, "Invalid Command", "Sub-commands available: `restrict_channel [id]`, `unrestrict_channel [id]`, `add_level_reward [level] [role]`, `remove_level_reward [level] [role]`, `show_restrictions`, `show_rewards`, "
                    + "`min_xp_per_tick (amount)`, `max_xp_per_tick (amount)`, `seconds_per_tick (seconds)`, `min_notif_level (level)`, `sweep`");
                return;
            }
            database.UpdateConfig();
        }
    }
}
