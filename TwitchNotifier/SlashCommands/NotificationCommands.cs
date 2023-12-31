﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchNotifier.Config;
using TwitchNotifier.Models;
using TwitchNotifier.Service;

namespace TwitchNotifier.SlashCommands
{
    internal class NotificationCommands : ApplicationCommandModule
    {
        #region [Notification Add]

        [SlashCommand("notification_add", "Adds a Twitch channel notification.")]
        public static async Task AddNotification(InteractionContext ctx,
            [Option("twitch_channel_name", "Twitch channel name.")] string twitchChannelName,
            [Option("channel", "Discord channel.")] DiscordChannel discordChannel,
            [Option("embed_color", "Embed color (HEX).")] string? color = null,
            [Option("message", "Message to be sent with the notification.")] string? message = null)
        {
            if (!PermissionsManager.CheckPermissionsIn(ctx.Member, ctx.Channel, new() { Permissions.Administrator }) && !ctx.Member.IsOwner)
            {
                await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Insufficient permissions. You need **Administrator** permission for this command."
                }, true);
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordMember bot;
            try
            {
                bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Could not find myself on the server. Please try again or contact [support team](https://t.me/Shawtygoldq)."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.AccessChannels }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "I don't have access to this channel! Please check the permissions."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.SendMessages, Permissions.EmbedLinks, Permissions.AttachFiles }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages, embed links or attach files in this channel. Please check the permissions."
                }));
                return;
            }              

            if (color != null)
            {
                if (!color.StartsWith("#") && color.Length != 7)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Title = "An error occurred",
                        Color = DiscordColor.Red,
                        Description = "Wrong color specified! Use HEX code for color transfer, for example: #FFFFFF."
                    }));
                    return;
                }
            }

            var jsonReader = new JSONReader();
            jsonReader.ReadJSONAsync().Wait();

            TwitchAPI api = new();
            api.Settings.ClientId = jsonReader.TwitchClientId;
            api.Settings.AccessToken = jsonReader.TwitchAccessToken;

            User user;
            try
            {
                GetUsersResponse? responce = await api.Helix.Users.GetUsersAsync(logins: new List<string>() { twitchChannelName });
                if(responce.Users == null || responce.Users.ToList().Count == 0)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Title = "An error occurred",
                        Color = DiscordColor.Red,
                        Description = "Couldn't find a channel with that name on Twitch.\nPlease check the channel name."
                    }));
                    return;
                }
                    
                user = responce.Users[0];
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = ex.Message
                }));
                return;
            }

            Notification notification = new()
            {
                DiscordGuildId = discordChannel.Guild.Id,
                DiscordChannelId = discordChannel.Id,
                HEXEmbedColor = color,
                TwitchChannelName = twitchChannelName,
                Message = message
            };

            //добавление уведомления в базу данных
            if (!await Database.AddNotificationAsync(notification))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Hmm, the notification was not added due to a database error.\nPlease try again or contact [support team](https://t.me/Shawtygoldq)"
                }));
                return;
            }

            if (!await StreamMonitoringService.AddChannelToMonitoringAsync(notification.TwitchChannelName))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Hmm, failed to add the channel to the list of monitored channels.\nPlease try again or contact [support team](https://t.me/Shawtygoldq)."
                }));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Green,
                Description = $"Notification to the **{twitchChannelName}** channel has been successfully added!"
            }));
        }

        #endregion

        #region [Notification Remove]

        [SlashCommand("notification_remove", "Removes Twitch channel notification from the server.")]
        public static async Task RemoveNotification(InteractionContext ctx,
            [Option("notification_Id", "Notification Id.")] long notificationId)
        {
            if (!PermissionsManager.CheckPermissionsIn(ctx.Member, ctx.Channel, new() { Permissions.Administrator }) && !ctx.Member.IsOwner)
            {
                await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Insufficient permissions. You need **Administrator** permission for this command."
                }, true);
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordMember bot;
            try
            {
                bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Could not find myself on the server. Please try again or contact [support team](https://t.me/Shawtygoldq)."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.AccessChannels }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "I don't have access to this channel! Please check the permissions."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.SendMessages }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages to this channel. Please check the permissions."
                }));
                return;
            }

            DiscordGuild guild = ctx.Guild;

            Notification? notification = await Database.GetNotificationAsync(notificationId);

            if (notification == null || notification.DiscordGuildId != guild.Id)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Hmm, notification not found on this server! Please check id."
                }));
                return;
            }

            //удаление уведомления из базы данных
            if (!await Database.RemoveNotificationAsync(notification))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Hmm, the notification was not deleted due to a database error.\nPlease try again or contact [support team](https://t.me/Shawtygoldq)."
                }));
                return;
            }

            if(!await StreamMonitoringService.RemoveChannelFromMonitoringAsync(notification.TwitchChannelName))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Hmm, failed to remove the channel from the list of monitored channels.\nPlease try again or contact [support team](https://t.me/Shawtygoldq)."
                }));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Title = "Complete",
                Color = DiscordColor.Green,
                Description = $"The notification was successfully removed!"
            }));
        }

        #endregion

        #region [Notification List]

        [SlashCommand("notification_list", "Lists all available notifications on the server.")]
        public static async Task NotifiactionList(InteractionContext ctx)
        {
            if (!PermissionsManager.CheckPermissionsIn(ctx.Member,ctx.Channel, new() { Permissions.Administrator }) && !ctx.Member.IsOwner)
            {
                await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Insufficient permissions. You need **Administrator** permission for this command."
                }, true);
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordMember bot;
            try
            {
                bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Could not find myself on the server. Please try again or contact [support team](https://t.me/Shawtygoldq)."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.AccessChannels}))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "I don't have access to this channel! Please check the permissions."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.SendMessages, Permissions.EmbedLinks }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages or embed links in this channel. Please check the permissions."
                }));
                return;
            }

            DiscordGuild guild = ctx.Guild;

            List<Notification>? guildNotifications = await Database.GetNotificationsInAsync(guild.Id);

            if (guildNotifications == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Hmm, an error occurred while retrieving notifications from this server from the database. Please try again or contact [support team](https://t.me/Shawtygoldq)."
                }));
                return;
            }

            if (guildNotifications.Count == 0)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "Hmm...",
                    Color = new DiscordColor("#9246FF"),
                    Description = "It looks like there are no notifications on this server yet."
                }));
                return;
            }

            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#9246FF"),
                Title = "Notification list:",
                Footer = new() { Text = "twitch-notifier" }
            };

            for (int i = 0; i < guildNotifications.Count; i++)
            {
                long notificationId = guildNotifications[i].Id;
                string twitchChannelName = guildNotifications[i].TwitchChannelName;
                string twitchChannelLink = $"https://www.twitch.tv/{twitchChannelName}";

                string? color = guildNotifications[i].HEXEmbedColor;
                color ??= "#9246FF";

                string? message = guildNotifications[i].Message;
                message ??= "No message";

                string discordChannelName;
                DiscordChannel discordChannel;
                try
                {
                    discordChannel = guild.GetChannel(guildNotifications[i].DiscordChannelId);
                    if(discordChannel != null)
                    {
                        discordChannelName = discordChannel.Name.Replace(discordChannel.Name[0], char.ToUpper(discordChannel.Name[0]));
                    }
                    else
                    {
                        discordChannelName = "Not found.";
                    }
                }
                catch (ServerErrorException)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Title = "An error occurred",
                        Color = DiscordColor.Red,
                        Description = "Server Error Exception. Please try again or contact [support team](https://t.me/Shawtygoldq)."
                    }));
                    return;
                }

                embed.AddField($"#{twitchChannelName}", $"> **Id:** {notificationId}\n> **Embed color:** {color}\n> **Guild:** {guild.Name}\n> **Channel:** {discordChannelName}\n> **Message:** {message}\n> **Link:** {twitchChannelLink}");
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        #endregion

        #region [Notification Edit]

        [SlashCommand("Notification_edit", "Edits the notification.")]
        public static async Task EditNotification(InteractionContext ctx,
            [Option("notification_Id", "Notification Id.")] long notificationId,
            [Option("message", "Message to be sent with the notification.")] string? message = null,
            [Option("channel", "Discord channel.")] DiscordChannel? discordChannel = null,
            [Option("color", "Embed color (HEX).")] string? embedColor = null)
        {
            if (!PermissionsManager.CheckPermissionsIn(ctx.Member, ctx.Channel, new() { Permissions.Administrator }) && !ctx.Member.IsOwner)
            {
                await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Insufficient permissions. You need **Administrator** permission for this command."
                }, true);
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordMember bot;
            try
            {
                bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Could not find myself on the server. Please try again or contact [support team](https://t.me/Shawtygoldq)."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.AccessChannels }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "I don't have access to this channel! Please check the permissions."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.SendMessages }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages to this channel. Please check the permissions."
                }));
                return;
            }

            if(message == null && discordChannel == null && embedColor == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "Warning",
                    Color = DiscordColor.Yellow,
                    Description = "The notification has not been edited. You did not specify which data should be changed."
                }));
                return;
            }

            Notification? notification = await Database.GetNotificationAsync(notificationId);
            if (notification == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Hmm, notification not found! Please check id."
                }));
                return;
            }

            notification.Message = message ?? notification.Message;

            if (discordChannel != null)
                notification.DiscordChannelId = discordChannel.Id;

            if (embedColor != null)
            {
                if (!embedColor.StartsWith("#") && embedColor.Length != 7)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Title = "An error occurred",
                        Color = DiscordColor.Red,
                        Description = "Wrong color specified! Use HEX code for color transfer, for example: #FFFFFF."
                    }));
                    return;
                }

                notification.HEXEmbedColor = embedColor;
            }

            if(!await Database.EditNotificationAsync(notification))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Hmm, the notification could not be edited due to a database error.\nPlease try again or contact [support team](https://t.me/Shawtygoldq)."
                }));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Title = "Complete",
                Color = DiscordColor.Green,
                Description = $"Notification with id **{notification.Id}** has been successfully edited!"
            }));
        }

        #endregion
    }
}
