using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using TwitchNotifier.Models;

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
                    Description = "I don't have access to this channel. Please check the permissions."
                }));
                return;
            }

            if(!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.SendMessages, Permissions.EmbedLinks }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages or embed links in this channel. Please check the permissions."
                }));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Color = new DiscordColor("#9246FF"),
                Description = $"Link to invite to the server: **{inviteLink}**\nNeed help? Private message me at [Telegram](https://t.me/Shawtygoldq)."
            }));
        }
    }
}
