using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using TwitchNotifier.Config;
using TwitchNotifier.Models;
using TwitchNotifier.SlashCommands;

namespace TwitchNotifier
{
    internal class Bot
    {
        internal static DiscordClient Client { get; private set; }
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

            SlashCommands = Client.UseSlashCommands();
            SlashCommands.RegisterCommands<NotificationCommands>();

            StreamMonitor streamMonitor;
            List<Notification>? notifications = await NotificationsDataWorker.GetNotificationsAsync(); 

            if (notifications == null)
                streamMonitor = new(new List<Notification>());
            else
                streamMonitor = new(notifications);

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            Console.WriteLine("Bot is ready!");
            DiscordActivity activity = new()
            {
                Name = "/help",
                ActivityType = ActivityType.Streaming,
                StreamUrl = "https://www.twitch.tv/shawtygoldq"
            };

            Client.UpdateStatusAsync(activity).Wait();

            return Task.CompletedTask;
        }
    }
}
