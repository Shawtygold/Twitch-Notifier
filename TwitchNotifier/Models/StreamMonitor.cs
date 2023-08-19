using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System.Numerics;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Services;
using TwitchNotifier.Config;
using TwitchNotifier.Helpers;

namespace TwitchNotifier.Models
{
    internal class StreamMonitor
    {
        private static List<string> _channelsToMonitor;
        private static List<Notification> _notifications;
        private static LiveStreamMonitorService _monitorService;
        private static TwitchAPI _api;
        private static bool _startTracking;

        public StreamMonitor(List<Notification> notifications)
        {
            if (notifications == null)
                return;

            var jsonReader = new JSONReader();
            jsonReader.ReadJSONAsync().Wait();

            _api = new TwitchAPI();
            _api.Settings.ClientId = jsonReader.TwitchClientId;
            _api.Settings.AccessToken = jsonReader.TwitchAccessToken;

            _startTracking = false;
            _notifications = notifications;
            _channelsToMonitor = new();

            if (_notifications.Count > 0)
            {
                for (int i = 0; i < _notifications.Count; i++)
                {
                    _channelsToMonitor.Add(_notifications[i].TwitchChannelName);
                }
            }

            _monitorService = new(_api);
            _monitorService.OnStreamOnline += MonitorService_OnStreamOnline;

            if (_notifications.Count > 0)
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

            //получаю количество уведомлений на один и тот же канал
            List<Notification> notifications;
            try
            {
                notifications = _notifications.FindAll(n => n.TwitchChannelName == twitchChannelName);
            }
            catch (Exception ex)
            {
                ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong.\nException: {ex}");
                return;
            }

            for (int i = 0; i < notifications.Count; i++)
            {
                DiscordChannel discordChannel;
                try
                {
                    discordChannel = await Bot.Client.GetChannelAsync(notifications[i].DiscordChannelId);
                }
                catch (Exception ex)
                {
                    ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong when trying to get a discord channel to which you need to send a notification about the start of the stream on Twitch. Discord channel not found!\nException: {ex}");
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
                        await ErrorMessageHelper.SendEmbedErrorMessageAsync(discordChannel, $"Hmm, something went wrong.\n\nPlease contact the developer. To do this, go to https://t.me/Shawtygoldq and include the following debugging information in the message:\n```{ex}\n```");
                    }
                    catch
                    {
                        ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong when trying to find out the owner of the channel that started the live stream.\nException: {ex}");
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
                    Footer = new() { Text = $"Id {notifications[i].Id} • Today, {DateTime.Now.ToShortTimeString()}" }
                };

                embed.AddField("Game:", stream.GameName, true);
                embed.AddField("Viewer count", $"{stream.ViewerCount}", true);

                var button = new DiscordLinkButtonComponent(streamUrl, "Watch Stream");

                string? notificationMessage;

                if (notifications[i].Message == null)
                    notificationMessage = "";
                else
                    notificationMessage = notifications[i].Message;

                try
                {
                    await discordChannel.SendMessageAsync(new DiscordMessageBuilder().WithContent(notificationMessage).AddEmbed(embed).AddComponents(button));
                }
                catch (UnauthorizedException)
                {
                    try
                    {
                        await ErrorMessageHelper.SendEmbedErrorMessageAsync(discordChannel, $"Hmm, something went wrong. I may not be allowed to embed links and attach files! Please check the permissions.");
                    }
                    catch
                    {
                        ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong. The bot is not allowed to insert links and attach files! Permissions should be checked.");
                    }                    
                    return;
                }
                catch (Exception ex)
                {
                    try
                    {
                        await ErrorMessageHelper.SendEmbedErrorMessageAsync(discordChannel, $"Hmm, something went wrong when trying to send a notification to the Discord channel.\n\nThis was Discord's response:\n> {ex.Message}\n\nIf you would like to contact the bot owner about this, please go to https://t.me/Shawtygoldq and include the following debugging information in the message:\n```{ex}\n```");
                    }
                    catch
                    {
                        ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong when trying to send a notification to the Discord channel.\nException:{ex}");
                    }                    
                    return;
                }
            }
        }

        #endregion

        #region [Methods]

        public static async Task<bool> AddNotificationAsync(Notification notification)
        {
            //добавление уведомления в бд
            if(!await NotificationsDataWorker.AddNotificationAsync(notification))
            {
                return false;
            }

            _notifications.Add(notification);
            _channelsToMonitor.Add(notification.TwitchChannelName);

            await UpdateChannelsToMonitorAsync(_channelsToMonitor);

            if (!_startTracking)
            {
                await StartTrackingAsync();
                _startTracking = true;
            }

            return true;
        }

        public static async Task<bool> RemoveNotificationAsync(Notification notification)
        {
            if(!await NotificationsDataWorker.RemoveNotificationAsync(notification))
            {
                return false;
            }

            _notifications.Remove(notification);
            _channelsToMonitor.Remove(notification.TwitchChannelName);

            if (_channelsToMonitor.Count > 0)
            {
                await UpdateChannelsToMonitorAsync(_channelsToMonitor);
            }

            return true;
        }

        private static async Task StartTrackingAsync()
        {
            try
            {
                await Task.Run(() => _monitorService.SetChannelsByName(_channelsToMonitor));
                await Task.Run(() => _monitorService.Start());
            }
            catch (Exception ex)
            {
                ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong when trying to start tracking channel activity.\nException: {ex}");
                return;
            }
        }

        private static async Task UpdateChannelsToMonitorAsync(List<string> channelsToMonitor)
        {
            try
            {
                await Task.Run(() => _monitorService.SetChannelsByName(channelsToMonitor));
            }
            catch (Exception ex)
            {
                ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong while trying to set channels to monitor!\nException: {ex}");
            }
        }

        #endregion
    }
}
