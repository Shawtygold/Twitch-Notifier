using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchNotifier.SlashCommands
{
    internal class AboutCommand : ApplicationCommandModule
    {
        #region [About]

        [SlashCommand("about", "View statistics about bot uptime and usage.")]
        public static async Task About(InteractionContext ctx)
        {

        }     

        #endregion
    }
}
