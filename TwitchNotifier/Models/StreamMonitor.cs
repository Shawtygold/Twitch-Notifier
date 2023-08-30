using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Services;
using TwitchNotifier.Config;

namespace TwitchNotifier.Models
{
    internal class StreamMonitor
    {
        private static List<string> _channelsToMonitor { get; set; }
        
        private static LiveStreamMonitorService _monitorService;
        private static TwitchAPI _api;
        private static bool _startTracking;

        public StreamMonitor(List<string> channelsToMonitor)
        {
            if (channelsToMonitor == null)
                return;

            var jsonReader = new JSONReader();
            jsonReader.ReadJSONAsync().Wait();

            _api = new TwitchAPI();
            _api.Settings.ClientId = jsonReader.TwitchClientId;
            _api.Settings.AccessToken = jsonReader.TwitchAccessToken;

            _startTracking = false;
            _channelsToMonitor = channelsToMonitor;

            _monitorService = new(_api);
            _monitorService.OnStreamOnline += MonitorService_OnStreamOnline;

            if (_channelsToMonitor.Count > 0)
            {
                StartTrackingAsync().Wait();
                _startTracking = true;
            }
        }

        #region [Events]

        private async void MonitorService_OnStreamOnline(object? sender, TwitchLib.Api.Services.Events.LiveStreamMonitor.OnStreamOnlineArgs e)
        {
            string twitchChannelName = e.Channel;
            var stream = e.Stream;
      
            List<Notification>? allNotifications = await Database.GetNotificationsAsync();

            if (allNotifications == null)
                return;

            //получаю список уведомлений на один и тот же Twitch канал  
            List<Notification> notifications = allNotifications.FindAll(n => n.TwitchChannelName == twitchChannelName);

            for (int i = 0; i < notifications.Count; i++)
            {
                DiscordChannel discordChannel;
                try
                {
                    discordChannel = await Bot.Client.GetChannelAsync(notifications[i].DiscordChannelId);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception: {ex}");
                    return;
                }

                string streamUrl = $"https://www.twitch.tv/{twitchChannelName}";

                User twitchChannelOwner;
                try
                {
                    var userListResponse = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { twitchChannelName });
                    twitchChannelOwner = userListResponse.Users[0];
                }
                catch (Exception ex)
                {
                    try
                    {
                        await discordChannel.SendMessageAsync(new DiscordEmbedBuilder()
                        {
                            Color = DiscordColor.Red,
                            Description = $"Hmm, something went wrong.\n\nPlease contact the developer and include the following debugging information in the message:\n```{ex}\n```"
                        });
                    }
                    catch
                    {
                        Logger.Error($"Exception: {ex}");
                    }                   
                    return;
                }

                var previewUrl = stream.ThumbnailUrl;

                if (previewUrl == null)
                    previewUrl = twitchChannelOwner.ProfileImageUrl;
                else
                {
                    previewUrl = previewUrl.Replace("{height}", "225");
                    previewUrl = previewUrl.Replace("{width}", "400");
                }

                DiscordColor embedColor;

                if (notifications[i].HEXEmbedColor == null)
                    embedColor = new DiscordColor("#9246FF");
                else
                    embedColor = new DiscordColor(notifications[i].HEXEmbedColor);

                DiscordEmbedBuilder embed = new()
                {
                    Author = new() { Url = streamUrl, IconUrl = twitchChannelOwner.ProfileImageUrl, Name = $"{twitchChannelOwner.Login} is now live on Twitch!" },
                    Title = $"{stream.Title}",
                    Url = streamUrl,
                    Color = embedColor,
                    ImageUrl = previewUrl,
                    Footer = new() { Text = $"Id {notifications[i].Id} • twitch-notifier" }
                };

                embed.AddField("Game:", stream.GameName, true);
                embed.AddField("Viewer count", $"{stream.ViewerCount}", true);

                var button = new DiscordLinkButtonComponent(streamUrl, "Watch Stream");

                string notificationMessage = notifications[i].Message ?? "";

                try
                {
                    await discordChannel.SendMessageAsync(new DiscordMessageBuilder().WithContent(notificationMessage).AddEmbed(embed).AddComponents(button));
                }
                catch (UnauthorizedException ex)
                {
                    try
                    {
                        await discordChannel.SendMessageAsync( new DiscordEmbedBuilder()
                        {
                            Color = DiscordColor.Red,
                            Description = $"Hmm, something went wrong. Maybe I'm not allowed to access the channel, send messages, embed links or attach files! Please, check the permissions."
                        });
                    }
                    catch
                    {
                        Logger.Error($"Exception: {ex}");
                    }                    
                    return;
                }
                catch (Exception ex)
                {
                    try
                    {
                        await discordChannel.SendMessageAsync(new DiscordEmbedBuilder()
                        {
                            Color = DiscordColor.Red,
                            Description = $"Hmm, something went wrong when trying to send a notification to the Discord channel.\n\nThis was Discord's response:\n> {ex.Message}\n\nIf you would like to contact the bot owner about this, please include the following debugging information in the message:\n```{ex}\n```"
                        });
                    }
                    catch
                    {
                        Logger.Error($"Exception: {ex}");
                    }                    
                    return;
                }
            }
        }

        #endregion

        #region [Methods]

        public static async Task AddNotificationAsync(string twitchChannelName)
        {
            if (twitchChannelName == null)
                return;

            _channelsToMonitor.Add(twitchChannelName);

            await UpdateChannelsToMonitorAsync(_channelsToMonitor);

            if (!_startTracking)
            {
                await StartTrackingAsync();
                _startTracking = true;
            }
        }

        public static async Task RemoveNotificationAsync(string twitchChannelName)
        {
            if (twitchChannelName == null)
                return;

            _channelsToMonitor.Remove(twitchChannelName);

            if (_channelsToMonitor.Count > 0)
            {
                await UpdateChannelsToMonitorAsync(_channelsToMonitor);
            }
        }

        public static async Task UpdateChannelsToMonitorAsync(List<string> channelsToMonitor)
        {
            try
            {
                await Task.Run(() => _monitorService.SetChannelsByName(channelsToMonitor));
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception: {ex}");
                return;
            }
        }

        private static async Task StartTrackingAsync()
        {
            await Task.Run(() => StartTracking());
        }

        private static void StartTracking()
        {
            try
            {
                _monitorService.SetChannelsByName(_channelsToMonitor);
                _monitorService.Start();
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception: {ex}");
                return;
            }
        }

        #endregion
    }
}
