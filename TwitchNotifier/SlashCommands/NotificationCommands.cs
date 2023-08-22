using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using TwitchNotifier.Models;

namespace TwitchNotifier.SlashCommands
{
    internal class NotificationCommands : ApplicationCommandModule
    {
        #region [Notification Add]

        [SlashCommand("notification_add", "Add notification of the beginning of the stream account on twitch.")]
        public static async Task AddNotification(InteractionContext ctx,
            [Option("twitch_channel_name", "The name of the Twitch channel.")] string twitchChannelName,
            [Option("channel", "Discord channel.")] DiscordChannel discordChannel,
            [Option("embed_color", "Color embed message.")] string? color = null,
            [Option("message", "The message to be sent along with the notification.")] string? message = null)
        {
            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator) && !ctx.Member.IsOwner)
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
            catch (ServerErrorException)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Server Error Exception. Please, try again or contact the developer."
                }));
                return;
            }

            if (!bot.PermissionsIn(ctx.Channel).HasPermission(Permissions.AccessChannels))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "I don't have access to this channel! Please, check the permissions."
                }));
                return;
            }

            if (!bot.PermissionsIn(ctx.Channel).HasPermission(Permissions.SendMessages) || !bot.PermissionsIn(ctx.Channel).HasPermission(Permissions.EmbedLinks) || !bot.PermissionsIn(ctx.Channel).HasPermission(Permissions.AttachFiles))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages, insert links or attach files in this channel. Please check the permissions."
                }));
                return;
            }              

            if (color != null)
            {
                if (!color.StartsWith("#") && color.Length != 7)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Color = DiscordColor.Red,
                        Description = "Wrong color specified! Use HEX code for color transfer, for example: #FFFFFF."
                    }));
                    return;
                }
            }

            Notification notification = new()
            {
                DiscordGuildId = discordChannel.Guild.Id,
                DiscordChannelId = discordChannel.Id,
                HEXEmbedColor = color,
                TwitchChannelName = twitchChannelName,
                Message = message
            };

            if (!await StreamMonitor.AddNotificationAsync(notification))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Hmm, the notification was not added due to an internal database error.\n\nPlease contact the developer. To do this, go to https://t.me/Shawtygoldq."
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

        [SlashCommand("notification_remove", "Remove notification of the beginning of the stream account on twitch.")]
        public static async Task RemoveNotification(InteractionContext ctx,
            [Option("notification_Id", "The name of the Twitch channel.")] long notificationId)
        {
            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator) && !ctx.Member.IsOwner)
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
            catch (ServerErrorException)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Server Error Exception. Please, try again or contact the developer."
                }));
                return;
            }

            if (!bot.PermissionsIn(ctx.Channel).HasPermission(Permissions.AccessChannels))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "I don't have access to this channel! Please, check the permissions."
                }));
                return;
            }

            if (!bot.PermissionsIn(ctx.Channel).HasPermission(Permissions.SendMessages))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages to this channel. Please, check the permissions."
                }));
                return;
            }

            DiscordGuild guild = ctx.Guild;

            Notification? notification = await NotificationsDataWorker.GetNotificationAsync(notificationId);

            if (notification == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Hmm, notification not found! Please check id."
                }));
                return;
            }

            if (notification.DiscordGuildId != guild.Id)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Hmm, notification not found on this server! Please, check id."
                }));
                return;
            }

            if (!await StreamMonitor.RemoveNotificationAsync(notification))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Hmm, the notification was not removed due to an internal database error.\n\nPlease contact the developer. To do this, go to https://t.me/Shawtygoldq."
                }));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Green,
                Description = $"The notification was successfully removed!"
            }));
        }

        #endregion

        #region [Notification List]

        [SlashCommand("notification_list", "Lists all available notifications on the server.")]
        public static async Task NotifiactionList(InteractionContext ctx)
        {
            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator) && !ctx.Member.IsOwner)
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
            catch (ServerErrorException)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Server Error Exception. Please, try again or contact the developer."
                }));
                return;
            }

            if (!bot.PermissionsIn(ctx.Channel).HasPermission(Permissions.AccessChannels))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "I don't have access to this channel! Please, check the permissions."
                }));
                return;
            }

            if (!bot.PermissionsIn(ctx.Channel).HasPermission(Permissions.SendMessages) || !bot.PermissionsIn(ctx.Channel).HasPermission(Permissions.EmbedLinks))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages or embed links in this channel. Please, check the permissions."
                }));
                return;
            }

            DiscordGuild guild = ctx.Guild;
            DiscordChannel channel = ctx.Channel;

            List<Notification>? guildNotifications = await NotificationsDataWorker.GetNotificationsAsync(guild.Id);

            if (guildNotifications == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Hmm, an error occurred while trying to get notifications on this server from the database. Please, contact the developer. To do this, go to https://t.me/Shawtygoldq."
                }));
                return;
            }

            if (guildNotifications.Count == 0)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
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
                string? color = guildNotifications[i].HEXEmbedColor;
                color ??= "#9246FF";

                string? message = guildNotifications[i].Message;
                message ??= "No message";

                embed.AddField($"#{guildNotifications[i].TwitchChannelName}", $"> **Id:** {guildNotifications[i].Id}\n> **Embed color:** {color}\n> **Guild Id:** {guildNotifications[i].DiscordGuildId}\n> **Channel Id:** {guildNotifications[i].DiscordChannelId}\n> **Message:** {message}\n> **Link:** https://www.twitch.tv/{guildNotifications[i].TwitchChannelName}");
            }

            try
            {
                await ctx.Client.SendMessageAsync(channel, embed);
            }
            catch (UnauthorizedException)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = $"Hmm, something went wrong. Maybe I'm not allowed to send messages, embed links or attach files! Please, check the permissions."
                }));
                return;
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = $"Hmm, something went wrong when trying to send a message to the discord channel!\n\nThis was Discord's response:\n> {ex.Message}\n\nIf you would like to contact the bot owner about this, please include the following debugging information in the message:\n```{ex}\n```"
                }));
                return;
            }        

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Success!"));
        }

        #endregion
    }
}
