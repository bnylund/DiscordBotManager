using McMaster.NETCore.Plugins;
using Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;

namespace DiscordBotManager
{
    class Program
    {
        public static bool run;
        public static Dictionary<string, Bot> bots;

        static void Main(string[] args)
        {
            Log("Waiting for network...");
            bool connected = false;
            while (!connected)
            {
                try
                {
                    if(new Ping().Send("google.com", 2000).Status == IPStatus.Success)
                        connected = true;
                } catch(Exception ex) { }
            }
            Log("Connected to internet!");


            bots = new Dictionary<string, Bot>();
            run = true;
            Log("Current Directory: " + Environment.CurrentDirectory);

            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                Log("Received CTRL+C");
                e.Cancel = true;
                run = false;
            };

            AppDomain.CurrentDomain.ProcessExit += (s, v) =>
            {
                Log("Received ProcessExit");
                run = false;
            };

            Log("Loading plugins...");
            if (!Directory.Exists("plugins"))
                Directory.CreateDirectory("plugins");

            int count = 0;

            foreach (string s in Directory.EnumerateFiles("plugins", "*.dll", SearchOption.TopDirectoryOnly))
            {
                Log("Found file '" + s + "' in plugins, loading");
                if (LoadPlugin(s))
                    count++;
            }

            Log("Loaded " + count + " plugins!");

            while (run)
            {
                // Check for new files ONLY
                foreach (string s in Directory.EnumerateFiles("plugins", "*.dll", SearchOption.TopDirectoryOnly))
                {
                    string path = Path.GetFullPath(s);
                    if (!bots.ContainsKey(path))
                    {
                        Log("Found new file '" + s + "' in plugins, loading");
                        LoadPlugin(s);
                    }
                }

                Thread.Sleep(1000);
            }

            Log("Stopping bots...");
            foreach (Bot bot in bots.Values)
            {
                bot.bot.StopBot();
                bot.thread.Join();
                bot.loader.Dispose();
            }
        }

        public static void Log(string text)
        {
            Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString("h: mm:ss tt") + "] " + text);
            File.AppendAllText("log", "\r\n[" + DateTime.Now.ToLocalTime().ToString("MM/dd/yyyy h:mm:ss tt") + "] " + text);
        }

        public static bool LoadPlugin(string s)
        {
            bool toReturn = false;

            string path = Path.GetFullPath(s);
            var loader = PluginLoader.CreateFromAssemblyFile(assemblyFile: path, sharedTypes: new[] { typeof(IBot) }, isUnloadable: true, configure: config => config.EnableHotReload = true);

            loader.Reloaded += (saaa, v) =>
            {
                bots[path].bot.StopBot();
                bots[path].thread.Join();
                bots.Remove(path);

                foreach (var pluginType in v.Loader.LoadDefaultAssembly().GetTypes())
                {
                    if (typeof(IBot).IsAssignableFrom(pluginType) && !pluginType.IsAbstract)
                    {
                        IBot bot = (IBot)Activator.CreateInstance(pluginType);
                        if (bot != null)
                        {
                            Log("Found bot! " + bot.Name + " by " + bot.Author);

                            bot.Directory = Environment.CurrentDirectory + "/plugins/" + bot.Name + "/";
                            if (!Directory.Exists(bot.Directory))
                                Directory.CreateDirectory(bot.Directory);

                            // Start bot
                            Thread th = new Thread(new ThreadStart(bot.StartBot));
                            th.Start();

                            bots.Add(path, new Bot() { bot = bot, loader = loader, thread = th, path = path, filename = s.Replace("plugins/", "") });
                        }
                    }
                }
            };

            foreach (var pluginType in loader.LoadDefaultAssembly().GetTypes())
            {
                if (typeof(IBot).IsAssignableFrom(pluginType) && !pluginType.IsAbstract)
                {
                    IBot bot = (IBot)Activator.CreateInstance(pluginType);
                    if (bot != null)
                    {
                        Log("Found bot: " + bot.Name + " by " + bot.Author);

                        bot.Directory = Environment.CurrentDirectory + "/plugins/" + bot.Name + "/";
                        if (!Directory.Exists(bot.Directory))
                            Directory.CreateDirectory(bot.Directory);

                        // Start bot
                        Thread th = new Thread(new ThreadStart(bot.StartBot));
                        th.Start();

                        bots.Add(path, new Bot() { bot = bot, loader = loader, thread = th, path = path, filename = s.Replace("plugins/", "") });
                        toReturn = true;
                    }
                }
            }
            return toReturn;
        }
    }
}
