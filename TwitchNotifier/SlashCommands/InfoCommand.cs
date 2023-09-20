using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using TwitchNotifier.Models;

namespace TwitchNotifier.SlashCommands
{
    internal class InfoCommand : ApplicationCommandModule
    {
        [SlashCommand("info", "Shows information about the Twitch-Notifier")]
        public static async Task Info(InteractionContext ctx)
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

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new(){ Permissions.AccessChannels}))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred",
                    Color = DiscordColor.Red,
                    Description = "I don't have access to this channel. Please check the permissions."
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

            int langVersion = 10;
            int totalShard = ctx.Client.ShardCount;
            int guildsCount = ctx.Client.Guilds.Count;
            int gatewayVersion = ctx.Client.GatewayVersion;
            string DSharpVersion = ctx.Client.VersionString;

            DateTimeOffset joinedAt = bot.JoinedAt.LocalDateTime;

            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#9246FF"),
                Title = "Twitch-Notifier Stats",
                Footer = new DiscordEmbedBuilder.EmbedFooter(){ Text = "Twitch-notifier is a copyright 2023–2023 of Twitch-notifier, LLC" }
            };

            embed.AddField("Joined At", $"{joinedAt.LocalDateTime}");
            embed.AddField("Servers", $"**·** Total {guildsCount}\n**·** Total Shards {totalShard}", true);
            embed.AddField("Versions", $"**·** C# {langVersion}\n**·** D#+ Version {DSharpVersion}\n**·** Gateway Version {gatewayVersion}", true);
            embed.AddField("Developer", "[Shawtygold](https://t.me/Shawtygoldq)");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}
