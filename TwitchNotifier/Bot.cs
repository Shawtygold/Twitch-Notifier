using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using TwitchNotifier.Config;
using TwitchNotifier.Models;
using TwitchNotifier.Service;
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
            SlashCommands.RegisterCommands<InfoCommand>();
            SlashCommands.RegisterCommands<InviteCommand>();
            SlashCommands.RegisterCommands<HelpCommand>();

            List<Notification>? notifications = await Database.GetNotificationsAsync();
            if (notifications == null)
                return;

            List<string> channelsToMonitor = await GetChannelsToMonitorAsync(notifications);
            StreamMonitoringService streamMonitoringService = new(channelsToMonitor);

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            Logger.Info("Client is ready!");
            DiscordActivity activity = new()
            {
                Name = "/help",
                ActivityType = ActivityType.Streaming,
                StreamUrl = "https://www.twitch.tv/shawtygoldq"
            };

            Client.UpdateStatusAsync(activity).Wait();

            return Task.CompletedTask;
        }

        private static async Task<List<string>> GetChannelsToMonitorAsync(List<Notification> notifications)
        {
            List<string> channelsToMonitor = new();
            await Task.Run(() => channelsToMonitor = GetChannelsToMonitor(notifications));
            return channelsToMonitor;
        }
        private static List<string> GetChannelsToMonitor(List<Notification> notifications)
        {
            List<string> channelsToMonitor = new();

            for (int i = 0; i < notifications.Count; i++)
            {
                channelsToMonitor.Add(notifications[i].TwitchChannelName);
            }

            return channelsToMonitor;
        }
    }
}
