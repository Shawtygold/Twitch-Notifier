using DSharpPlus;
using DSharpPlus.Entities;

namespace TwitchNotifier.Models
{
    internal class PermissionsManager
    {
        public static bool CheckPermissionsIn(DiscordMember member, DiscordChannel channel, List<Permissions> permissions)
        {
            for (int i = 0; i < permissions.Count; i++)
            {
                if (!member.PermissionsIn(channel).HasPermission(permissions[i]))
                    return false;
            }

            return true;
        }
    }
}
