using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchNotifier.SlashCommands
{
    internal class InviteCommand : ApplicationCommandModule
    {
        private const string inviteLink = "https://goo.su/YI2YK82";


        [SlashCommand("invite", "Display a link to invite Twitch-Notifier to your server.")]
        public static async Task Invite(InteractionContext ctx)
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

            if (!bot.PermissionsIn(ctx.Channel).HasPermission(Permissions.AccessChannels))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "I don't have access to this channel. Please, check the permissions."
                }));
                return;
            }

            if(!bot.PermissionsIn(ctx.Channel).HasPermission(Permissions.SendMessages) || !bot.PermissionsIn(ctx.Channel).HasPermission(Permissions.EmbedLinks))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages or embed links in this channel. Please, check the permissions."
                }));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Color = new DiscordColor("#9246FF"),
                Description = $"You can invite me to a server with this link: **{inviteLink}**\nNeed help? Write to me in private messages: shawtygold"
            }));
        }
    }
}
