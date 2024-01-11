using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using DiscordBotBase;
using System.Threading;
using FreneticUtilities.FreneticDataSyntax;
using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;

namespace DiscordLevelsBot
{
    public static class LevelsWeb
    {

        public static MicroWebHelper WebHelper;

        public static string WebURL;

        public static MicroWebHelper.InjectableHtml Header, Footer, Index, Error404;

        public static MicroWebHelper.WebResult IndexResult, Error404Result;

        public static Dictionary<ulong, MicroWebHelper.WebResult> CachedBoards = [];

        /// <summary>Cache rate - default 30 minutes.</summary>
        public static long CacheMillis = 30 * 60 * 1000;

        /// <summary>Max leaderboard entries per page - default 1000.</summary>
        public static int MaxPerPage = 3000;

        public static MicroWebHelper.InjectableHtml GetInjectable(string path)
        {
            return new MicroWebHelper.InjectableHtml(File.ReadAllText($"webpages/{path}.html"));
        }

        public static MicroWebHelper.WebResult BuildFromHtml(MicroWebHelper.InjectableHtml rawPage, int code)
        {
            PageHelper page = new();
            string pageText = page.GetInjectable(rawPage);
            string headerText = page.GetInjectable(Header);
            string footerText = page.GetInjectable(Footer);
            return new MicroWebHelper.WebResult() { Code = code, ContentType = "text/html", Data = StringConversionHelper.UTF8Encoding.GetBytes(headerText + pageText + footerText) };
        }

        public static void Load()
        {
            Header = GetInjectable("header");
            Footer = GetInjectable("footer");
            Index = GetInjectable("index");
            Error404 = GetInjectable("404");
            IndexResult = BuildFromHtml(Index, 200);
            Error404Result = BuildFromHtml(Error404, 404);
        }

        public class PageHelper
        {
            public HttpListenerContext Context;

            public string Title, Description;

            public string ReadInjecter(string inject)
            {
                switch (inject)
                {
                    case "TITLE":
                        return Title;
                    case "DESCRIPTION":
                        return Description;
                }
                (string setter, string value) = inject.BeforeAndAfter('=');
                if (!string.IsNullOrWhiteSpace(value))
                {
                    switch (setter)
                    {
                        case "TITLE":
                            Title = value;
                            return "";
                        case "DESCRIPTION":
                            Description = value;
                            return "";
                    }
                }
                throw new Exception("Invalid injectable: " + inject);
            }

            public string GetInjectable(MicroWebHelper.InjectableHtml injectable)
            {
                return injectable.Get(ReadInjecter);
            }
        }

        public static string[] RankColors = ["bg-warning", "bg-light", "bg-primary"];

        public static void GetBoardEntry(StringBuilder output, int rank, UserData data)
        {
            int width = rank < 5 ? 128 : (rank < 20 ? 64 : (rank < 30 ? 48 : 32));
            string rankColor = rank > 3 ? "bg-dark" : RankColors[rank - 1];
            output.Append("<tr class=\"table-secondary\">")
                .Append($"<th scope=\"row\"><span class=\"badge rounded-pill {rankColor}\" style=\"font-size:{(width / 2)}px;\">{rank}</span></th>")
                .Append(string.IsNullOrWhiteSpace(data.LastKnownAvatar) ? "<td>?</td>" : $"<td><img src=\"{data.LastKnownAvatar}?size={width}\" width=\"{width}\"></td>")
                .Append(string.IsNullOrWhiteSpace(data.LastKnownName) ? $"<td>{data.RawID}</td>" : $"<td><abbr title=\"{data.RawID}\">{MicroWebHelper.HtmlEscape(data.LastKnownName)}</abbr></td>")
                .Append($"<td><b>{data.Level}</b></td><td><b>{data.XP}</b> Total, <b>{data.PartialXP}</b>/<b>{data.CalcTotalXPToNextLevel()}</b> towards next level</td>");
        }
        
        public static MicroWebHelper.WebResult GetPage(string url, HttpListenerContext context)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return IndexResult;
            }
            if (url.StartsWith("leaderboard/") && ulong.TryParse(url["leaderboard/".Length..], out ulong guildId) && UserDBHelper.DatabaseByGuild.TryGetValue(guildId, out UserDBHelper database))
            {
                if (CachedBoards.TryGetValue(guildId, out MicroWebHelper.WebResult cached) && cached.GeneratedTickTime > Environment.TickCount64 - CacheMillis)
                {
                    return cached;
                }
                PageHelper page = new()
                {
                    Title = "Leaderboard for " + MicroWebHelper.HtmlEscape(database.Name),
                    Description = $"Discord Levels Bot leaderbord for Discord Guild '{MicroWebHelper.HtmlEscape(database.Name)}'! Are you active enough?!"
                };
                string header = page.GetInjectable(Header);
                string footer = page.GetInjectable(Footer);
                int rank = 0;
                StringBuilder output = new();
                long start = Environment.TickCount64;
                lock (database.Lock)
                {
                    output.Append($"<br><br><div class=\"leaderboard_wrap\"><h3><img src=\"/levelup.png\" height=\"100\">Discord Activity Leaderboard for <span class=\"text-success\">{MicroWebHelper.HtmlEscape(database.Name)}</span></h3>")
                        .Append($"<br>\n<h4>Tracked a total of {database.Users.Count()} users.\n")
                        .Append("<br>\n<table class=\"table table-hover\">\n")
                        .Append("<thead><tr><th scope=\"col\">Rank</th><th scope=\"col\">Avatar</th><th scope=\"col\">Name</th><th scope=\"col\">Level</th><th scope=\"col\">XP</th></tr></thead>\n<tbody>\n");
                    if (database.Config.TopID != 0)
                    {
                        UserData current = database.GetUser(database.Config.TopID);
                        while (current != null)
                        {
                            rank++;
                            GetBoardEntry(output, rank, current);
                            if (rank >= MaxPerPage)
                            {
                                break;
                            }
                            current = current.LeaderboardPrev == 0 ? null : database.GetUser(current.LeaderboardPrev);
                        }
                    }
                }
                output.Append("\n</tbody></table></div>\n");
                MicroWebHelper.WebResult result = new() { Code = 200, ContentType = "text/html", Data = StringConversionHelper.UTF8Encoding.GetBytes(header + output + footer) };
                CachedBoards[guildId] = result;
                Console.WriteLine($"Generated board for guild {guildId} in {(Environment.TickCount64 - start)}ms");
                return result;
            }
            return Error404Result;
        }
    }
}
