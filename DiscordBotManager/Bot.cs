using McMaster.NETCore.Plugins;
using Plugin;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DiscordBotManager
{
    internal class Bot
    {
        public Thread thread { get; set; }
        public IBot bot { get; set; }
        public PluginLoader loader { get; set; }
        public string path { get; set; }
        public string filename { get; set; }
    }
}
