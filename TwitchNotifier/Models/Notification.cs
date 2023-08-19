using DSharpPlus.Entities;

namespace TwitchNotifier.Models
{
    internal class Notification
    {
        public long Id { get; set; }
        public string TwitchChannelName { get; set; } = null!;
        public ulong DiscordGuildId { get; set; }
        public ulong DiscordChannelId { get; set; }
        public string? HEXEmbedColor { get; set; }
        public string? Message { get; set; }
    }
}
