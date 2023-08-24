using DSharpPlus.Entities;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
