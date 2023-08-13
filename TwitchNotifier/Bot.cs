using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchNotifier.Config;

namespace TwitchNotifier
{
    internal class Bot
    {
        internal DiscordClient Client { get; private set; }
        internal SlashCommandsExtension SlashCommands { get; private set; }

        public async Task RunBotAsync()
        {
            var jsonReader = new JSONReader();
            await jsonReader.ReadJSONAsync();

            Client = new DiscordClient(new DiscordConfiguration()
            {
                Token = jsonReader.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
                AutoReconnect = true
            });

            Client.Ready += Client_Ready;

            //SlashCommands = Client.UseSlashCommands();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            Console.WriteLine("Bot is ready!");

            return Task.CompletedTask;
        }
    }
}
