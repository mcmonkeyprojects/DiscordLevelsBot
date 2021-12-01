using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Loader;
using DiscordBotBase;
using Discord.WebSocket;

namespace DiscordLevelsBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (!Directory.Exists("./saves"))
            {
                Directory.CreateDirectory("./saves");
            }
            DiscordBotConfig config = new()
            {
                CacheSize = 0,
                EnsureCaching = false,
                Initialize = Initialize,
                OnShutdown = UserDBHelper.Shutdown
            };
            AssemblyLoadContext.Default.Unloading += (context) =>
            {
                UserDBHelper.Shutdown();
            };
            AppDomain.CurrentDomain.ProcessExit += (obj, e) =>
            {
                UserDBHelper.Shutdown();
            };
            DiscordBotBaseHelper.StartBotHandler(args, config);
        }

        public static void Initialize(DiscordBot bot)
        {
            bot.Client.MessageReceived += Client_MessageReceived;
        }

        public static async Task Client_MessageReceived(SocketMessage arg)
        {
        }
    }
}
