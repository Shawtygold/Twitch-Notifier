using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Services;
using TwitchNotifier.Config;
using TwitchNotifier.Models;

namespace TwitchNotifier.Service
{
    internal class StreamMonitoringService
    {
        public static List<string> ChannelsToMonitoring { get; set; }
        private static LiveStreamMonitorService MonitorService { get; set; }
        private static TwitchAPI Api { get; set; }

        private static bool _startTracking;

        public StreamMonitoringService(List<string> channelsToMonitoring)
        {
            if (channelsToMonitoring == null)
                return;

            var jsonReader = new JSONReader();
            jsonReader.ReadJSONAsync().Wait();

            Api = new TwitchAPI();
            Api.Settings.ClientId = jsonReader.TwitchClientId;
            Api.Settings.AccessToken = jsonReader.TwitchAccessToken;

            _startTracking = false;
            ChannelsToMonitoring = channelsToMonitoring;

            MonitorService = new(Api);
            MonitorService.OnStreamOnline += MonitorService_OnStreamOnline;

            if (ChannelsToMonitoring.Count > 0)
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
                    Logger.Error($"{ex}");
                    return;
                }

                string streamUrl = $"https://www.twitch.tv/{twitchChannelName}";

                User twitchChannelOwner;
                try
                {
                    GetUsersResponse response = await Api.Helix.Users.GetUsersAsync(logins: new List<string> { twitchChannelName });
                    if (response.Users == null || response.Users.ToList().Count == 0)
                    {
                        try
                        {
                            await discordChannel.SendMessageAsync(new DiscordEmbedBuilder()
                            {
                                Title = "An error occurred",
                                Color = DiscordColor.Red,
                                Description = "The owner of the Twitch channel could not be found.\nPlease contact [support team](https://t.me/Shawtygoldq)."
                            });
                        }
                        catch
                        {
                            Logger.Error($"Could not find the owner of this Twitch channel.");
                        }
                        return;
                    }

                    twitchChannelOwner = response.Users[0];
                }
                catch (Exception ex)
                {
                    try
                    {
                        await discordChannel.SendMessageAsync(new DiscordEmbedBuilder()
                        {
                            Title = "An error occurred",
                            Color = DiscordColor.Red,
                            Description = $"Hmm, something went wrong. This was Discord's response:\n> {ex.Message}\n\nPlease contact [support team](https://t.me/Shawtygoldq)."
                        });
                    }
                    catch
                    {
                        Logger.Error($"{ex}");
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
                        await discordChannel.SendMessageAsync(new DiscordEmbedBuilder()
                        {
                            Title = "An error occurred",
                            Color = DiscordColor.Red,
                            Description = $"Hmm, something went wrong. Maybe I'm not allowed to send messages, embed links or attach files! Please check the permissions."
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
                            Title = "An error occurred",
                            Color = DiscordColor.Red,
                            Description = $"Hmm, something went wrong. This was Discord's response:\n> {ex.Message}\n\nPlease contact [support team](https://t.me/Shawtygoldq)."
                        });
                    }
                    catch
                    {
                        Logger.Error($"{ex}");
                    }
                    return;
                }
            }
        }

        #endregion

        #region [Methods]

        public static async Task<bool> AddChannelToMonitoringAsync(string twitchChannelName)
        {
            if (twitchChannelName == null)
                return false;

            ChannelsToMonitoring.Add(twitchChannelName);

            await UpdateChannelsToMonitoringAsync(ChannelsToMonitoring);

            if (!_startTracking)
            {
                await StartTrackingAsync();
                _startTracking = true;
            }

            return true;
        }

        public static async Task<bool> RemoveChannelFromMonitoringAsync(string twitchChannelName)
        {
            if (twitchChannelName == null)
                return false;

            ChannelsToMonitoring.Remove(twitchChannelName);

            if (ChannelsToMonitoring.Count > 0)
            {
                if(!await UpdateChannelsToMonitoringAsync(ChannelsToMonitoring))
                    return false;
            }

            return true;
        }

        public static async Task<bool> UpdateChannelsToMonitoringAsync(List<string> channelsToMonitor)
        {
            try
            {
                await Task.Run(() => MonitorService.SetChannelsByName(channelsToMonitor));
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex}");
                return false;
            }
        }

        private static async Task<bool> StartTrackingAsync()
        {
            bool result = false;
            await Task.Run(() => { result = StartTracking(); });

            if (!result)
                return false;

            return true;
        }

        private static bool StartTracking()
        {
            try
            {
                MonitorService.SetChannelsByName(ChannelsToMonitoring);
                MonitorService.Start();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex}");
                return false;
            }
        }

        #endregion
    }
}
