using DSharpPlus.SlashCommands;
using System;
using DSharpPlus;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using TwitchNotifier.Models;

namespace TwitchNotifier.SlashCommands
{
    internal class HelpCommand : ApplicationCommandModule
    {
        [SlashCommand("help", "View basic help.")]
        public static async Task Help(InteractionContext ctx)
        {
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
                    Description = "Server Error Exception. Please try again or contact the developer."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.AccessChannels }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "I don't have access to this channel. Please, check the permissions."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.SendMessages, Permissions.EmbedLinks, Permissions.AttachFiles }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages, embed links or attach files in this channel. Please check the permissions."
                }));
                return;
            }

            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#9246FF"),
                Title = "Twitch-Notifier help",
                //ImageUrl = bot.AvatarUrl,
                Thumbnail = new() { Url = bot.AvatarUrl},
                
                Footer = new() { IconUrl = bot.AvatarUrl }
            };

            embed.AddField("Info", "To view information about the bot, use the `/info` command.");
            embed.AddField("Add a notification", "To add a notification to the Twitch channel you are interested in, use the `/notification_add` command.");
            embed.AddField("Remove a notification", "To remove a notification added earlier, use the `/notification_remove` command.");
            embed.AddField("Notification list", "To view the list of available notifications on the server, use the `/notification_list` command.");
            embed.AddField("Invite link", "To get a link to invite Twitch-Notifier to your server, use the `/invite command` or go to the bot profile and click on the [add to server](https://discord.com/api/oauth2/authorize?client_id=1140675542123806730&permissions=274878090240&scope=bot) button.");
            embed.AddField("Contacts", "[**Telegram**](https://t.me/Shawtygoldq)\n[**Github**](https://github.com/Shawtygold)\n[**Discord**](https://discordapp.com/users/571713377316110361/)");

            var button = new DiscordLinkButtonComponent("https://t.me/Shawtygoldq", "Contact the developer");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed).AddComponents(button));
        }
    }
}
