using System;

namespace Plugin
{
    public interface IBot
    {
        // Name of the Bot
        string Name { get; }

        // Bot's Author
        string Author { get; }
        
        // Directory to use for storing files (gets pre-filled)
        string Directory { get; set; }

        void StartBot();
        void StopBot();
    }
}
